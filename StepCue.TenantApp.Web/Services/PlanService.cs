using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;
using StepCue.TenantApp.Data.Models.Planning;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Web.Services
{
    public class PlanService
    {
        private readonly DataContext _context;
        private readonly FileService _fileService;

        public PlanService(DataContext context, FileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<List<Plan>> GetPlansAsync()
        {
            return await _context.Plans
                .Include(p => p.Steps)
                .Include(p => p.Members)
                .ToListAsync();
        }

        public async Task<Plan> GetPlanAsync(int id)
        {
            return await _context.Plans
                .Include(p => p.Steps)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Plan> CreatePlanAsync(Plan plan)
        {
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<Plan> UpdatePlanAsync(Plan plan)
        {
            // Load the existing plan from database to get proper tracking
            var existingPlan = await _context.Plans
                .Include(p => p.Steps)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == plan.Id);

            if (existingPlan == null)
            {
                throw new InvalidOperationException($"Plan with ID {plan.Id} not found");
            }

            // Update plan properties
            existingPlan.Name = plan.Name;

            // Handle steps - collect new ones and update existing ones
            var newSteps = plan.Steps.Where(s => s.Id == 0).ToList();
            var existingSteps = plan.Steps.Where(s => s.Id > 0).ToList();

            // Add new steps
            foreach (var step in newSteps)
            {
                existingPlan.Steps.Add(step);
            }

            // Update existing steps
            foreach (var step in existingSteps)
            {
                var existingStep = existingPlan.Steps.FirstOrDefault(s => s.Id == step.Id);
                if (existingStep != null)
                {
                    existingStep.Name = step.Name;
                    existingStep.Summary = step.Summary;
                    existingStep.Screenshot = step.Screenshot;
                    existingStep.AssignedMembers = step.AssignedMembers;
                }
            }

            // Handle members - collect new ones and update existing ones
            var newMembers = plan.Members.Where(m => m.Id == 0).ToList();
            var existingMembers = plan.Members.Where(m => m.Id > 0).ToList();

            // Add new members
            foreach (var member in newMembers)
            {
                existingPlan.Members.Add(member);
            }

            // Update existing members
            foreach (var member in existingMembers)
            {
                var existingMember = existingPlan.Members.FirstOrDefault(m => m.Id == member.Id);
                if (existingMember != null)
                {
                    existingMember.Name = member.Name;
                    existingMember.EmailAddress = member.EmailAddress;
                }
            }

            await _context.SaveChangesAsync();
            return existingPlan;
        }

        public async Task DeletePlanAsync(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan != null)
            {
                _context.Plans.Remove(plan);
                await _context.SaveChangesAsync();
            }
        }
    }
}