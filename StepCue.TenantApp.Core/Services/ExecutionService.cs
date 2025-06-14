using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;
using StepCue.TenantApp.Data.Models.Execution;

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
                .Include(e => e.Steps).ThenInclude(i => i.AssignedMembers);

        }

        public async Task<Execution> GetExecutionAsync(int id)
        {


            return await GetExecutionsQueryable()
                .FirstOrDefaultAsync(e => e.Id == id);
        }


        public async Task<Execution> CreateExecutionFromPlanAsync(int planId)
        {
            var plan = await _context.Plans
                .Include(p => p.Steps).ThenInclude(s => s.AssignedMembers)
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

            // Copy steps
            foreach (var step in plan.Steps)
            {
                var executionStep = new ExecutionStep
                {
                    Name = step.Name,
                    Summary = step.Summary,
                    Screenshot = step.Screenshot
                };

                // Copy assigned members from plan step to execution step
                foreach (var assignedMember in step.AssignedMembers)
                {
                    executionStep.AssignedMembers.Add(assignedMember);
                }

                execution.Steps.Add(executionStep);
            }

            _context.Executions.Add(execution);
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
    }
}