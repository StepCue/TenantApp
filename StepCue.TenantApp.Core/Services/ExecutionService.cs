using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;
using StepCue.TenantApp.Data.Models;
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
                    StepType = step.StepType,
                    PlanStepOrder = step.Order // Track original plan step order
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
                        ExecutionMember = member,
                        IsApproved = false
                    };

                    step.Approvals.Add(approval);
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
            if (step.StepType == StepType.Activity)
            {
                return step.CompleteOn.HasValue;
            }

            // For go/nogo steps, check if all assigned members have approved
            if (step.StepType == StepType.GoNoGo)
            {
                if (!step.AssignedMembers.Any())
                    return false; // Can't complete an approval step with no assigned members

                //todo  All assigned members must have an approval record that is approved

                throw new NotImplementedException();
            }

            return false;
        }

        // Fallback functionality
     

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
                .ThenInclude(s => s.FallbackActivities)
                .FirstOrDefaultAsync(e => e.Id == executionId);

            if (execution?.Plan == null)
                return false;

            var planStep = execution.Plan.Steps.FirstOrDefault(ps => ps.Order == stepOrder);
            return planStep?.FallbackActivities.Any() ?? false;
        }

        public async Task<List<FallbackActivity>> GetFallbackStepsAsync(int executionId, int stepOrder)
        {
            var execution = await _context.Executions
                .Include(e => e.Plan)
                .ThenInclude(p => p.Steps)
                .ThenInclude(s => s.FallbackActivities)
                .ThenInclude(f => f.AssignedMembers)
                .FirstOrDefaultAsync(e => e.Id == executionId);

            if (execution?.Plan == null)
                return new List<FallbackActivity>();

            var planStep = execution.Plan.Steps.FirstOrDefault(ps => ps.Order == stepOrder);
            return planStep?.FallbackActivities ?? new List<FallbackActivity>();
        }

        private bool CanStartStep(ExecutionStep step, Execution execution)
        {
            // Check if all steps with lower order numbers are completed
            var stepsBeforeThisOne = execution.Steps
                .Where(s => s.Order < step.Order)
                .ToList();
                
            // If there are no steps before this one, it can always be started
            if (!stepsBeforeThisOne.Any()) return true;
            
            // Check if all previous steps are completed
            return stepsBeforeThisOne.All(s => IsStepComplete(s));
        }
    }
}