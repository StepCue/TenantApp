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
                .Include(e => e.Steps).ThenInclude(s => s.FallbackSteps)
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
                .Include(p => p.Steps)
                .ThenInclude(s => s.FallbackSteps)
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

            // Copy steps in order
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

            // Copy fallback step relationships after steps have IDs
            foreach (var planStep in plan.Steps)
            {
                var executionStep = execution.Steps.FirstOrDefault(es => es.Order == planStep.Order);
                if (executionStep != null && planStep.FallbackSteps.Any())
                {
                    foreach (var fallbackPlanStep in planStep.FallbackSteps)
                    {
                        var fallbackExecutionStep = execution.Steps.FirstOrDefault(es => es.Order == fallbackPlanStep.Order);
                        if (fallbackExecutionStep != null)
                        {
                            executionStep.FallbackSteps.Add(fallbackExecutionStep);
                        }
                    }
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

            // For go/nogo and fallback approval steps, check if all assigned members have approved
            if (step.StepType == Data.Models.Planning.StepType.GoNoGo || step.StepType == Data.Models.Planning.StepType.Fallback)
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
        public async Task<ExecutionStep> CreateFallbackApprovalStepAsync(int executionId, int originalStepId, string reason, List<int> fallbackStepIds)
        {
            var execution = await GetExecutionAsync(executionId);
            if (execution == null)
                throw new ArgumentException("Execution not found");

            var originalStep = execution.Steps.FirstOrDefault(s => s.Id == originalStepId);
            if (originalStep == null)
                throw new ArgumentException("Original step not found");

            // Get all affected members from the fallback steps
            var fallbackSteps = execution.Steps.Where(s => fallbackStepIds.Contains(s.Id)).ToList();
            var affectedMembers = fallbackSteps.SelectMany(s => s.AssignedMembers).Distinct().ToList();

            // Create fallback approval step
            var approvalStep = new ExecutionStep
            {
                Name = $"Fallback Approval for {originalStep.Name}",
                Summary = $"Approval required for falling back from step '{originalStep.Name}'. Reason: {reason}",
                StepType = StepType.Fallback,
                Order = GetNextOrderValue(execution),
                ExecutionId = executionId,
                FallbackOriginStepId = originalStepId,
                FallbackReason = reason
            };

            // Add affected members to the approval step
            foreach (var member in affectedMembers)
            {
                approvalStep.AssignedMembers.Add(member);
            }

            execution.Steps.Add(approvalStep);
            _context.ExecutionSteps.Add(approvalStep);
            await _context.SaveChangesAsync();

            // Create approval records for all affected members
            foreach (var member in affectedMembers)
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
            if (approvalStep == null || approvalStep.StepType != StepType.Fallback)
                throw new ArgumentException("Approval step not found or invalid");

            if (!IsStepComplete(approvalStep))
                throw new InvalidOperationException("Approval step is not complete");

            var originalStepId = approvalStep.FallbackOriginStepId;
            if (!originalStepId.HasValue)
                throw new InvalidOperationException("Original step ID not found");

            var originalStep = execution.Steps.FirstOrDefault(s => s.Id == originalStepId.Value);
            if (originalStep == null)
                throw new ArgumentException("Original step not found");

            // Get the fallback steps to execute
            var fallbackSteps = originalStep.FallbackSteps.ToList();
            
            // Cancel all remaining steps after the original step
            var stepsToCancel = execution.Steps.Where(s => s.Order > originalStep.Order && !s.IsCancelled).ToList();
            foreach (var step in stepsToCancel)
            {
                step.IsCancelled = true;
            }

            // Create new execution steps for the fallback steps
            var newSteps = new List<ExecutionStep>();
            var currentOrder = GetNextOrderValue(execution);

            foreach (var fallbackStep in fallbackSteps.OrderBy(s => s.Order))
            {
                var newStep = new ExecutionStep
                {
                    Name = fallbackStep.Name,
                    Summary = fallbackStep.Summary,
                    Screenshot = fallbackStep.Screenshot,
                    StepType = fallbackStep.StepType,
                    Order = currentOrder++,
                    ExecutionId = executionId,
                    FallbackOriginStepId = originalStepId.Value
                };

                // Copy assigned members
                foreach (var member in fallbackStep.AssignedMembers)
                {
                    newStep.AssignedMembers.Add(member);
                }

                execution.Steps.Add(newStep);
                newSteps.Add(newStep);
                _context.ExecutionSteps.Add(newStep);
            }

            await _context.SaveChangesAsync();

            // Create approval records for any new go/nogo steps
            foreach (var step in newSteps.Where(s => s.StepType == StepType.GoNoGo))
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
            return newSteps;
        }

        private int GetNextOrderValue(Execution execution)
        {
            return execution.Steps.Any() ? execution.Steps.Max(s => s.Order) + 1 : 1;
        }
    }
}