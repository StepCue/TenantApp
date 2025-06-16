using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;
using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Core.Services
{
    public class ExecutionService
    {
        private readonly DataContext _context;


        public ExecutionService(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Execution> GetExecutionsQueryable()
        {
            return _context.Executions
                .Include(e => e.Plan)
                .Include(e => e.Members)
                .Include(e => e.Steps).ThenInclude(s => s.AssignedMembers)
                .Include(e => e.Steps).ThenInclude(s => s.Approvals).ThenInclude(a => a.ExecutionMember);
        }

        public async Task<Execution> GetExecutionAsync(int id)
        {


            return await GetExecutionsQueryable()
                .FirstOrDefaultAsync(e => e.Id == id);
        }


        public async Task<Execution> CreateExecutionFromPlanAsync(int planId)
        {
            var plan = await _context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.AssignedMembers)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null)
                return null;

            var execution = new Execution
            {
                Name = $"Execution of {plan.Name}",
                PlanId = plan.Id,
                Plan = plan,
                CreatedOn = DateTime.Now
            };

            // Copy members
            foreach (var member in plan.Members)
            {
                execution.Members.Add(new ExecutionMember
                {
                    Name = member.Name,
                    EmailAddress = member.EmailAddress
                });
            }

            // Copy steps in order (excluding fallback steps as they are now separate entities)
            foreach (var step in plan.Steps.OrderBy(s => s.Order))
            {
                var executionStep = new ExecutionStep
                {
                    Name = step.Name,
                    Summary = step.Summary,
                    Screenshot = step.Screenshot,
                    Order = step.Order,
                    StepType = step.StepType
                };

                // Copy assigned members as ExecutionMembers
                foreach (var assignedMember in step.AssignedMembers)
                {
                    executionStep.AssignedMembers.Add(new ExecutionMember
                    {
                        Name = assignedMember.Name,
                        EmailAddress = assignedMember.EmailAddress
                    });
                }

                execution.Steps.Add(executionStep);
            }

            _context.Executions.Add(execution);
            await _context.SaveChangesAsync();

            // After saving, create approval records for go/nogo steps
            foreach (var step in execution.Steps.Where(s => s.StepType == StepType.GoNoGo))
            {
                foreach (var member in step.AssignedMembers)
                {
                    var approval = new ExecutionStepApproval
                    {
                        ExecutionStepId = step.Id,
                        ExecutionMemberId = member.Id,
                        IsApproved = false
                    };
                    _context.ExecutionStepApprovals.Add(approval);
                }
            }

            await _context.SaveChangesAsync();
            return execution;
        }

        public async Task<ExecutionStep> UpdateExecutionStepAsync(ExecutionStep step)
        {
            _context.Entry(step).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return step;
        }

        public async Task<ExecutionStepMessage> AddMessageToStepAsync(ExecutionStepMessage message)
        {
            _context.ExecutionStepMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<ExecutionStepApproval> AddApprovalToStepAsync(ExecutionStepApproval approval)
        {
            _context.ExecutionStepApprovals.Add(approval);
            await _context.SaveChangesAsync();
            return approval;
        }

        public async Task<ExecutionStepApproval> UpdateStepApprovalAsync(ExecutionStepApproval approval)
        {
            _context.Entry(approval).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return approval;
        }

        public bool IsStepComplete(ExecutionStep step)
        {
            // Cancelled steps are considered "complete" for workflow purposes
            if (step.IsCancelled)
            {
                return true;
            }

            // For regular execution steps, check if CompleteOn is set
            if (step.StepType == Data.Models.Planning.StepType.Execution)
            {
                return step.CompleteOn.HasValue;
            }

            // For go/nogo steps, check if all assigned members have approved
            if (step.StepType == Data.Models.Planning.StepType.GoNoGo)
            {
                if (!step.AssignedMembers.Any())
                    return false; // Can't complete an approval step with no assigned members

                // All assigned members must have an approval record that is approved
                return step.AssignedMembers.All(member =>
                    step.Approvals.Any(approval =>
                        approval.ExecutionMemberId == member.Id && approval.IsApproved));
            }

            return false;
        }

        // Fallback functionality
        public async Task<ExecutionStep> CreateFallbackApprovalStepAsync(int executionId, int originalStepId, string reason)
        {
            var execution = await GetExecutionAsync(executionId);
            if (execution == null)
                throw new ArgumentException("Execution not found");

            var originalStep = execution.Steps.FirstOrDefault(s => s.Id == originalStepId);
            if (originalStep == null)
                throw new ArgumentException("Original step not found");

            // Get the plan with its steps and fallback definitions
            var plan = await _context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.FallbackSteps)
                .ThenInclude(f => f.AssignedMembers)
                .FirstOrDefaultAsync(p => p.Id == execution.PlanId);

            if (plan == null)
                throw new ArgumentException("Plan not found");

            // Find the corresponding plan step
            var planStep = plan.Steps.FirstOrDefault(ps => ps.Order == originalStep.Order);

            if (planStep == null || !planStep.FallbackSteps.Any())
                throw new ArgumentException("No fallback steps defined for this step");

            // Get all affected members from the fallback definitions
            var affectedMembers = planStep.FallbackSteps.SelectMany(f => f.AssignedMembers).Distinct().ToList();

            // Create fallback approval step (using GoNoGo type since Fallback type no longer exists)
            var approvalStep = new ExecutionStep
            {
                Name = $"Fallback Approval for {originalStep.Name}",
                Summary = $"Approval required for falling back from step '{originalStep.Name}'. Reason: {reason}",
                StepType = StepType.GoNoGo, // Changed from Fallback to GoNoGo
                Order = GetNextOrderValue(execution),
                ExecutionId = executionId,
                FallbackOriginStepId = originalStepId,
                FallbackReason = reason
            };

            // Map plan members to execution members
            foreach (var planMember in affectedMembers)
            {
                var executionMember = execution.Members.FirstOrDefault(em => 
                    em.Name == planMember.Name && em.EmailAddress == planMember.EmailAddress);
                if (executionMember != null)
                {
                    approvalStep.AssignedMembers.Add(executionMember);
                }
            }

            execution.Steps.Add(approvalStep);
            _context.ExecutionSteps.Add(approvalStep);
            await _context.SaveChangesAsync();

            // Create approval records for all affected members
            foreach (var member in approvalStep.AssignedMembers)
            {
                var approval = new ExecutionStepApproval
                {
                    ExecutionStepId = approvalStep.Id,
                    ExecutionMemberId = member.Id,
                    IsApproved = false
                };
                _context.ExecutionStepApprovals.Add(approval);
            }

            await _context.SaveChangesAsync();
            return approvalStep;
        }

        public async Task<List<ExecutionStep>> ExecuteFallbackAsync(int executionId, int approvalStepId)
        {
            var execution = await GetExecutionAsync(executionId);
            if (execution == null)
                throw new ArgumentException("Execution not found");

            var approvalStep = execution.Steps.FirstOrDefault(s => s.Id == approvalStepId);
            if (approvalStep == null || !approvalStep.FallbackOriginStepId.HasValue)
                throw new ArgumentException("Approval step not found or invalid");

            if (!IsStepComplete(approvalStep))
                throw new InvalidOperationException("Approval step is not complete");

            var originalStepId = approvalStep.FallbackOriginStepId.Value;
            var originalStep = execution.Steps.FirstOrDefault(s => s.Id == originalStepId);
            if (originalStep == null)
                throw new ArgumentException("Original step not found");

            // Get the plan with its steps and fallback definitions
            var plan = await _context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.FallbackSteps)
                .ThenInclude(f => f.AssignedMembers)
                .FirstOrDefaultAsync(p => p.Id == execution.PlanId);

            if (plan == null)
                throw new ArgumentException("Plan not found");

            // Find the corresponding plan step
            var planStep = plan.Steps.FirstOrDefault(ps => ps.Order == originalStep.Order);

            if (planStep == null || !planStep.FallbackSteps.Any())
                throw new ArgumentException("No fallback definitions found for this step");

            // Cancel all remaining steps after the original step
            var stepsToCancel = execution.Steps.Where(s => s.Order > originalStep.Order && !s.IsCancelled).ToList();
            foreach (var step in stepsToCancel)
            {
                step.IsCancelled = true;
            }

            // Create new execution steps for the fallback definitions (in reverse order as requested)
            var newSteps = new List<ExecutionStep>();
            var currentOrder = GetNextOrderValue(execution);

            foreach (var fallback in planStep.FallbackSteps.OrderByDescending(f => f.Order))
            {
                var newStep = new ExecutionStep
                {
                    Name = fallback.Name,
                    Summary = fallback.Summary,
                    Screenshot = fallback.Screenshot,
                    StepType = StepType.Execution, // Fallback definitions become execution steps
                    Order = currentOrder++,
                    ExecutionId = executionId,
                    FallbackOriginStepId = originalStepId
                };

                // Map plan members to execution members
                foreach (var planMember in fallback.AssignedMembers)
                {
                    var executionMember = execution.Members.FirstOrDefault(em => 
                        em.Name == planMember.Name && em.EmailAddress == planMember.EmailAddress);
                    if (executionMember != null)
                    {
                        newStep.AssignedMembers.Add(executionMember);
                    }
                }

                execution.Steps.Add(newStep);
                newSteps.Add(newStep);
                _context.ExecutionSteps.Add(newStep);
            }

            await _context.SaveChangesAsync();
            return newSteps;
        }

        private int GetNextOrderValue(Execution execution)
        {
            return execution.Steps.Any() ? execution.Steps.Max(s => s.Order) + 1 : 1;
        }

        // Helper methods for fallback definitions
        public async Task<bool> HasFallbackStepsAsync(int executionId, int stepOrder)
        {
            var execution = await _context.Executions
                .Include(e => e.Plan)
                .ThenInclude(p => p.Steps)
                .ThenInclude(s => s.FallbackSteps)
                .FirstOrDefaultAsync(e => e.Id == executionId);

            if (execution?.Plan == null)
                return false;

            var planStep = execution.Plan.Steps.FirstOrDefault(ps => ps.Order == stepOrder);
            return planStep?.FallbackSteps.Any() ?? false;
        }

        public async Task<List<Fallback>> GetFallbackStepsAsync(int executionId, int stepOrder)
        {
            var execution = await _context.Executions
                .Include(e => e.Plan)
                .ThenInclude(p => p.Steps)
                .ThenInclude(s => s.FallbackSteps)
                .ThenInclude(f => f.AssignedMembers)
                .FirstOrDefaultAsync(e => e.Id == executionId);

            if (execution?.Plan == null)
                return new List<Fallback>();

            var planStep = execution.Plan.Steps.FirstOrDefault(ps => ps.Order == stepOrder);
            return planStep?.FallbackSteps ?? new List<Fallback>();
        }
    }
}