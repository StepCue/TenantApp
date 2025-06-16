using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Core.Services;
using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;
using Xunit;

namespace StepCue.TenantApp.Core.Tests.Services
{
    public class FallbackWorkflowTests : TestBase
    {
        private readonly ExecutionService _executionService;

        public FallbackWorkflowTests()
        {
            _executionService = new ExecutionService(Context);
        }

        [Fact]
        public async Task CreateFallbackApprovalStepAsync_ShouldCreateApprovalStep()
        {
            // Arrange - Create a plan with steps and fallback steps
            var plan = new Plan
            {
                Name = "Test Plan with Fallback",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "John Doe", EmailAddress = "john@example.com" },
                    new PlanMember { Name = "Jane Smith", EmailAddress = "jane@example.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Main Step", 
                        Summary = "Main execution step",
                        Order = 1,
                        StepType = StepType.Execution,
                        AssignedMembers = new List<PlanMember>()
                    },
                    new PlanStep 
                    { 
                        Name = "Fallback Step", 
                        Summary = "Fallback step to execute",
                        Order = 2,
                        StepType = StepType.Fallback,
                        AssignedMembers = new List<PlanMember>()
                    }
                }
            };

            // Add members to steps
            plan.Steps[0].AssignedMembers.Add(plan.Members[0]);
            plan.Steps[1].AssignedMembers.Add(plan.Members[1]);
            
            // Add fallback relationship
            plan.Steps[0].FallbackSteps.Add(plan.Steps[1]);

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Create execution from plan
            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            var mainStep = execution.Steps.First(s => s.Name == "Main Step");
            var fallbackStep = execution.Steps.First(s => s.Name == "Fallback Step");

            // Act - Create fallback approval step
            var approvalStep = await _executionService.CreateFallbackApprovalStepAsync(
                execution.Id, 
                mainStep.Id, 
                "Testing fallback workflow", 
                new List<int> { fallbackStep.Id });

            // Assert
            Assert.NotNull(approvalStep);
            Assert.Equal(StepType.Fallback, approvalStep.StepType);
            Assert.Equal("Testing fallback workflow", approvalStep.FallbackReason);
            Assert.Equal(mainStep.Id, approvalStep.FallbackOriginStepId);
            Assert.Contains("Fallback Approval for Main Step", approvalStep.Name);
            
            // Verify approval records were created
            var reloadedExecution = await _executionService.GetExecutionAsync(execution.Id);
            var reloadedApprovalStep = reloadedExecution.Steps.FirstOrDefault(s => s.Id == approvalStep.Id);
            
            Assert.NotNull(reloadedApprovalStep);
            Assert.Equal(1, reloadedApprovalStep.Approvals.Count); // Only Jane is assigned to fallback step
        }

        [Fact]
        public async Task ExecuteFallbackAsync_ShouldExecuteFallbackWorkflow()
        {
            // Arrange - Create execution with fallback approval step
            var plan = new Plan
            {
                Name = "Test Plan for Fallback Execution",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Team Lead", EmailAddress = "lead@test.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Original Step", 
                        Order = 1,
                        StepType = StepType.Execution,
                        AssignedMembers = new List<PlanMember>()
                    },
                    new PlanStep 
                    { 
                        Name = "Next Step", 
                        Order = 2,
                        StepType = StepType.Execution,
                        AssignedMembers = new List<PlanMember>()
                    },
                    new PlanStep 
                    { 
                        Name = "Recovery Step", 
                        Order = 3,
                        StepType = StepType.Fallback,
                        AssignedMembers = new List<PlanMember>()
                    }
                }
            };

            // Add member to all steps
            foreach (var step in plan.Steps)
            {
                step.AssignedMembers.Add(plan.Members[0]);
            }
            
            // Add fallback relationship
            plan.Steps[0].FallbackSteps.Add(plan.Steps[2]);

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            var originalStep = execution.Steps.First(s => s.Name == "Original Step");
            var nextStep = execution.Steps.First(s => s.Name == "Next Step");
            var recoveryStep = execution.Steps.First(s => s.Name == "Recovery Step");

            // Create and approve fallback approval step
            var approvalStep = await _executionService.CreateFallbackApprovalStepAsync(
                execution.Id, 
                originalStep.Id, 
                "Emergency fallback", 
                new List<int> { recoveryStep.Id });
            
            // Approve the fallback
            var approval = approvalStep.Approvals.First();
            approval.IsApproved = true;
            approval.ApprovalDate = DateTime.Now;
            await _executionService.UpdateStepApprovalAsync(approval);

            // Act - Execute the fallback
            var newSteps = await _executionService.ExecuteFallbackAsync(execution.Id, approvalStep.Id);

            // Assert
            Assert.Single(newSteps);
            Assert.Equal("Recovery Step", newSteps[0].Name);
            Assert.Equal(originalStep.Id, newSteps[0].FallbackOriginStepId);

            // Verify remaining steps were cancelled
            var updatedExecution = await _executionService.GetExecutionAsync(execution.Id);
            var updatedNextStep = updatedExecution.Steps.FirstOrDefault(s => s.Id == nextStep.Id);
            
            Assert.NotNull(updatedNextStep);
            Assert.True(updatedNextStep.IsCancelled);
        }

        [Fact]
        public async Task IsStepComplete_FallbackStep_ShouldRequireAllApprovals()
        {
            // Arrange - Create fallback approval step with multiple approvers
            var plan = new Plan
            {
                Name = "Test Multi-Approval Fallback",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Approver 1", EmailAddress = "approver1@test.com" },
                    new PlanMember { Name = "Approver 2", EmailAddress = "approver2@test.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Main Step", 
                        Order = 1,
                        StepType = StepType.Execution,
                        AssignedMembers = new List<PlanMember>()
                    },
                    new PlanStep 
                    { 
                        Name = "Fallback Step", 
                        Order = 2,
                        StepType = StepType.Fallback,
                        AssignedMembers = new List<PlanMember>()
                    }
                }
            };

            // Assign both members to fallback step
            plan.Steps[1].AssignedMembers.AddRange(plan.Members);
            plan.Steps[0].FallbackSteps.Add(plan.Steps[1]);

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            var mainStep = execution.Steps.First(s => s.Name == "Main Step");
            var fallbackStep = execution.Steps.First(s => s.Name == "Fallback Step");

            var approvalStep = await _executionService.CreateFallbackApprovalStepAsync(
                execution.Id, 
                mainStep.Id, 
                "Multi-approval test", 
                new List<int> { fallbackStep.Id });

            // Act & Assert - Should not be complete with no approvals
            Assert.False(_executionService.IsStepComplete(approvalStep));

            // Approve by first member only
            var firstApproval = approvalStep.Approvals.First();
            firstApproval.IsApproved = true;
            firstApproval.ApprovalDate = DateTime.Now;
            await _executionService.UpdateStepApprovalAsync(firstApproval);

            // Reload and check - should still not be complete
            var reloadedExecution = await _executionService.GetExecutionAsync(execution.Id);
            var reloadedApprovalStep = reloadedExecution.Steps.FirstOrDefault(s => s.Id == approvalStep.Id);
            Assert.False(_executionService.IsStepComplete(reloadedApprovalStep));

            // Approve by second member
            var secondApproval = reloadedApprovalStep.Approvals.Last();
            secondApproval.IsApproved = true;
            secondApproval.ApprovalDate = DateTime.Now;
            await _executionService.UpdateStepApprovalAsync(secondApproval);

            // Reload and check - should now be complete
            reloadedExecution = await _executionService.GetExecutionAsync(execution.Id);
            reloadedApprovalStep = reloadedExecution.Steps.FirstOrDefault(s => s.Id == approvalStep.Id);
            Assert.True(_executionService.IsStepComplete(reloadedApprovalStep));
        }

        [Fact]
        public async Task IsStepComplete_CancelledStep_ShouldReturnTrue()
        {
            // Arrange - Create a regular execution step
            var plan = new Plan
            {
                Name = "Test Cancelled Step",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Test User", EmailAddress = "test@test.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Test Step", 
                        Order = 1,
                        StepType = StepType.Execution,
                        AssignedMembers = new List<PlanMember>()
                    }
                }
            };

            plan.Steps[0].AssignedMembers.Add(plan.Members[0]);
            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            var step = execution.Steps.First();

            // Act - Mark step as cancelled
            step.IsCancelled = true;
            await _executionService.UpdateExecutionStepAsync(step);

            // Assert - Cancelled step should be considered complete
            Assert.True(_executionService.IsStepComplete(step));
        }
    }
}