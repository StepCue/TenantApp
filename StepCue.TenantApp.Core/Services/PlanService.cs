using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Core.Services
{
    public class PlanService
    {
        private readonly DataContext _context;
  

        public PlanService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Plan>> GetPlansAsync()
        {
            return await _context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.AssignedMembers)
                .Include(p => p.Members)
                .ToListAsync();
        }

        public async Task<Plan> GetPlanAsync(int id)
        {
            return await _context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.AssignedMembers)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Plan> CreatePlanAsync(Plan plan)
        {
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<Plan> CreateNewPlanAsync(string name = "New Plan")
        {
            var plan = new Plan
            {
                Name = name
            };
            
            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<Plan> UpdatePlanAsync(Plan plan)
        {
            // Load the existing plan from database to get proper tracking
            var existingPlan = await _context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.AssignedMembers)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == plan.Id);

            if (existingPlan == null)
            {
                throw new InvalidOperationException($"Plan with ID {plan.Id} not found");
            }

            // Update plan properties
            existingPlan.Name = plan.Name;

            // Handle steps - use a cleaner approach to avoid duplicates
            
            // First, remove steps that are no longer in the updated plan
            var incomingStepIds = plan.Steps.Where(s => s.Id > 0).Select(s => s.Id).ToHashSet();
            var stepsToRemove = existingPlan.Steps.Where(s => !incomingStepIds.Contains(s.Id)).ToList();
            foreach (var stepToRemove in stepsToRemove)
            {
                existingPlan.Steps.Remove(stepToRemove);
            }

            // Process each step from the incoming plan
            foreach (var incomingStep in plan.Steps)
            {
                if (incomingStep.Id == 0)
                {
                    // New step - add it
                    existingPlan.Steps.Add(incomingStep);
                }
                else
                {
                    // Existing step - update it
                    var existingStep = existingPlan.Steps.FirstOrDefault(s => s.Id == incomingStep.Id);
                    if (existingStep != null)
                    {
                        existingStep.Name = incomingStep.Name;
                        existingStep.Order = incomingStep.Order;
                        existingStep.Summary = incomingStep.Summary;
                        existingStep.Screenshot = incomingStep.Screenshot;
                        existingStep.StepType = incomingStep.StepType;
                        
                        // Handle AssignedMembers relationship properly
                        existingStep.AssignedMembers.Clear();
                        foreach (var assignedMember in incomingStep.AssignedMembers)
                        {
                            // Find the existing member in the plan's members to ensure proper tracking
                            var existingMember = existingPlan.Members.FirstOrDefault(m => m.Id == assignedMember.Id);
                            if (existingMember != null)
                            {
                                existingStep.AssignedMembers.Add(existingMember);
                            }
                        }
                    }
                }
            }

            // Handle members - use similar approach
            var incomingMemberIds = plan.Members.Where(m => m.Id > 0).Select(m => m.Id).ToHashSet();
            var membersToRemove = existingPlan.Members.Where(m => !incomingMemberIds.Contains(m.Id)).ToList();
            foreach (var memberToRemove in membersToRemove)
            {
                existingPlan.Members.Remove(memberToRemove);
            }

            // Process each member from the incoming plan
            foreach (var incomingMember in plan.Members)
            {
                if (incomingMember.Id == 0)
                {
                    // New member - add it
                    existingPlan.Members.Add(incomingMember);
                }
                else
                {
                    // Existing member - update it
                    var existingMember = existingPlan.Members.FirstOrDefault(m => m.Id == incomingMember.Id);
                    if (existingMember != null)
                    {
                        existingMember.Name = incomingMember.Name;
                        existingMember.EmailAddress = incomingMember.EmailAddress;
                    }
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