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
            _context.Entry(plan).State = EntityState.Modified;
            foreach (var step in plan.Steps)
            {
                _context.Entry(step).State = step.Id == 0 
                    ? EntityState.Added 
                    : EntityState.Modified;
            }
            foreach (var member in plan.Members)
            {
                _context.Entry(member).State = member.Id == 0 
                    ? EntityState.Added 
                    : EntityState.Modified;
            }
            await _context.SaveChangesAsync();
            return plan;
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