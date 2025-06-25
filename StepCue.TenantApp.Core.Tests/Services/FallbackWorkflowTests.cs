using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Core.Services;
using StepCue.TenantApp.Data.Models;
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

        [Fact(Skip = "Fallback functionality has been disabled")]
        public async Task CreateFallbackApprovalStepAsync_ShouldCreateApprovalStep()
        {
            // This test has been disabled because fallback functionality was removed
            await Task.CompletedTask;
        }

        [Fact(Skip = "Fallback functionality has been disabled")]
        public async Task ExecuteFallbackAsync_ShouldExecuteFallbackWorkflow()
        {
            // This test has been disabled because fallback functionality was removed
            await Task.CompletedTask;
        }

        [Fact(Skip = "Fallback functionality has been disabled")]
        public async Task IsStepComplete_FallbackStep_ShouldRequireAllApprovals()
        {
            // This test has been disabled because fallback functionality was removed
            await Task.CompletedTask;
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