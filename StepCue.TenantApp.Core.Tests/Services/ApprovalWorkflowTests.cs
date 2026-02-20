using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Core.Services;
using StepCue.TenantApp.Data;
using StepCue.TenantApp.Data.Models;
using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Core.Tests.Services
{
    public class ApprovalWorkflowTests : TestBase
    {
        private readonly ExecutionService _executionService;

        public ApprovalWorkflowTests()
        {
            _executionService = new ExecutionService(Context);
        }

        [Fact]
        public async Task ApprovalWorkflow_SingleMemberApproval_CompletesStep()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Test Plan",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Team Lead", EmailAddress = "lead@test.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Go/NoGo Decision", 
                        StepType = StepType.GoNoGo,
                        Order = 1,
                        AssignedMembers = new List<PlanMember>()
                    }
                }
            };
            
            // Add the member to the step
            plan.Steps[0].AssignedMembers.Add(plan.Members[0]);
            
            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act - Create execution from plan
            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            
            // Assert - Step should not be complete initially
            var goNoGoStep = execution.Steps.First(s => s.StepType == StepType.GoNoGo);
            Assert.False(_executionService.IsStepComplete(goNoGoStep));
            
            // Act - Approve the step
            var approval = goNoGoStep.Approvals.First();
            approval.IsApproved = true;
            approval.ApprovalDate = DateTime.Now;
            await _executionService.UpdateStepApprovalAsync(approval);
            
            // Reload step with approvals
            var updatedStep = await Context.ExecutionSteps
                .Include(s => s.Approvals)
                .Include(s => s.AssignedMembers)
                .FirstAsync(s => s.Id == goNoGoStep.Id);
            
            // Assert - Step should now be complete
            Assert.True(_executionService.IsStepComplete(updatedStep));
        }

        [Fact]
        public async Task ApprovalWorkflow_MultipleMembers_RequiresAllApprovals()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Test Plan",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Team Lead", EmailAddress = "lead@test.com" },
                    new PlanMember { Name = "Architect", EmailAddress = "architect@test.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Production Readiness", 
                        StepType = StepType.GoNoGo,
                        Order = 1,
                        AssignedMembers = new List<PlanMember>()
                    }
                }
            };
            
            // Add both members to the step
            plan.Steps[0].AssignedMembers.AddRange(plan.Members);
            
            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act - Create execution from plan
            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            
            // Assert - Step should not be complete initially
            var goNoGoStep = execution.Steps.First(s => s.StepType == StepType.GoNoGo);
            Assert.False(_executionService.IsStepComplete(goNoGoStep));
            Assert.Equal(2, goNoGoStep.Approvals.Count);
            
            // Act - Approve by first member only
            var firstApproval = goNoGoStep.Approvals.First();
            firstApproval.IsApproved = true;
            firstApproval.ApprovalDate = DateTime.Now;
            await _executionService.UpdateStepApprovalAsync(firstApproval);
            
            // Reload step
            var partiallyApprovedStep = await Context.ExecutionSteps
                .Include(s => s.Approvals)
                .Include(s => s.AssignedMembers)
                .FirstAsync(s => s.Id == goNoGoStep.Id);
            
            // Assert - Step should still not be complete with partial approval
            Assert.False(_executionService.IsStepComplete(partiallyApprovedStep));
            
            // Act - Approve by second member
            var secondApproval = goNoGoStep.Approvals.Last();
            secondApproval.IsApproved = true;
            secondApproval.ApprovalDate = DateTime.Now;
            await _executionService.UpdateStepApprovalAsync(secondApproval);
            
            // Reload step
            var fullyApprovedStep = await Context.ExecutionSteps
                .Include(s => s.Approvals)
                .Include(s => s.AssignedMembers)
                .FirstAsync(s => s.Id == goNoGoStep.Id);
            
            // Assert - Step should now be complete with all approvals
            Assert.True(_executionService.IsStepComplete(fullyApprovedStep));
        }

        [Fact]
        public async Task ApprovalWorkflow_MixedStepTypes_BehavesCorrectly()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Mixed Plan",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Developer", EmailAddress = "dev@test.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Deploy Code", 
                        StepType = StepType.Activity,
                        Order = 1,
                        AssignedMembers = new List<PlanMember>()
                    },
                    new PlanStep 
                    { 
                        Name = "Go/NoGo Check", 
                        StepType = StepType.GoNoGo,
                        Order = 2,
                        AssignedMembers = new List<PlanMember>()
                    }
                }
            };
            
            // Add member to both steps
            plan.Steps[0].AssignedMembers.Add(plan.Members[0]);
            plan.Steps[1].AssignedMembers.Add(plan.Members[0]);
            
            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act - Create execution from plan
            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            
            var executionStep = execution.Steps.First(s => s.StepType == StepType.Activity);
            var goNoGoStep = execution.Steps.First(s => s.StepType == StepType.GoNoGo);
            
            // Assert - Neither step is complete initially
            Assert.False(_executionService.IsStepComplete(executionStep));
            Assert.False(_executionService.IsStepComplete(goNoGoStep));
            
            // Act - Complete execution step
            executionStep.CompleteOn = DateTime.Now;
            await _executionService.UpdateExecutionStepAsync(executionStep);
            
            // Assert - Execution step is complete, go/nogo is not
            Assert.True(_executionService.IsStepComplete(executionStep));
            Assert.False(_executionService.IsStepComplete(goNoGoStep));
            
            // Act - Approve go/nogo step
            var approval = goNoGoStep.Approvals.First();
            approval.IsApproved = true;
            approval.ApprovalDate = DateTime.Now;
            await _executionService.UpdateStepApprovalAsync(approval);
            
            // Reload step
            var updatedGoNoGoStep = await Context.ExecutionSteps
                .Include(s => s.Approvals)
                .Include(s => s.AssignedMembers)
                .FirstAsync(s => s.Id == goNoGoStep.Id);
            
            // Assert - Both steps are now complete
            Assert.True(_executionService.IsStepComplete(executionStep));
            Assert.True(_executionService.IsStepComplete(updatedGoNoGoStep));
        }
    }
}