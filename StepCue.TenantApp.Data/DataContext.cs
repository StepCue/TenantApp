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
        public DbSet<ExecutionStepApproval> ExecutionStepApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Execution -> Steps relationship
            modelBuilder.Entity<Execution>()
                .HasMany(e => e.Steps)
                .WithOne(s => s.Execution)
                .HasForeignKey(s => s.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Execution -> Members relationship  
            modelBuilder.Entity<Execution>()
                .HasMany(e => e.Members)
                .WithOne(m => m.Execution)
                .HasForeignKey(m => m.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExecutionStep -> Messages relationship
            modelBuilder.Entity<ExecutionStep>()
                .HasMany(es => es.Messages)
                .WithOne(m => m.ExecutionStep)
                .HasForeignKey(m => m.ExecutionStepId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExecutionStep -> Approvals relationship
            modelBuilder.Entity<ExecutionStep>()
                .HasMany(es => es.Approvals)
                .WithOne(a => a.ExecutionStep)
                .HasForeignKey(a => a.ExecutionStepId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ExecutionStepApproval -> ExecutionMember relationship
            modelBuilder.Entity<ExecutionStepApproval>()
                .HasOne(a => a.ExecutionMember)
                .WithMany()
                .HasForeignKey(a => a.ExecutionMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}