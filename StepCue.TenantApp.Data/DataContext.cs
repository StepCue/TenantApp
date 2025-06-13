using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data.Models.Planning;
using StepCue.TenantApp.Data.Models.Execution;

namespace StepCue.TenantApp.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<Plan> Plans { get; set; }
        public DbSet<PlanStep> PlanSteps { get; set; }
        public DbSet<PlanMember> PlanMembers { get; set; }
        public DbSet<Execution> Executions { get; set; }
        public DbSet<ExecutionStep> ExecutionSteps { get; set; }
        public DbSet<ExecutionMember> ExecutionMembers { get; set; }
        public DbSet<ExecutionStepMessage> ExecutionStepMessages { get; set; }
    }
}
