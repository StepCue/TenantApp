using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;
using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Web.Services
{
    public class ExecutionService
    {
        private readonly DataContext _context;
        private readonly FileService _fileService;

        public ExecutionService(DataContext context, FileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<List<Execution>> GetExecutionsAsync()
        {
            return await _context.Executions
                .Include(e => e.Plan)
                .Include(e => e.Steps)
                .ToListAsync();
        }

        public async Task<Execution> GetExecutionAsync(int id)
        {
            return await _context.Executions
                .Include(e => e.Plan)
                .Include(e => e.Steps)
                .Include(e => e.Members)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ExecutionStep> GetExecutionStepAsync(int id)
        {
            return await _context.ExecutionSteps
                .Include(s => s.Messages)
                    .ThenInclude(m => m.Author)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Execution> CreateExecutionFromPlanAsync(int planId)
        {
            var plan = await _context.Plans
                .Include(p => p.Steps)
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
                execution.Steps.Add(new ExecutionStep
                {
                    Name = step.Name,
                    Summary = step.Summary,
                    Screenshot = step.Screenshot
                });
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