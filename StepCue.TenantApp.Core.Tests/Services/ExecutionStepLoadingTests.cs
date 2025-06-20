using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Core.Services;
using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;
using Xunit;

namespace StepCue.TenantApp.Core.Tests.Services
{
    public class ExecutionStepLoadingTests : TestBase
    {
        private readonly ExecutionService _executionService;

        public ExecutionStepLoadingTests()
        {
            _executionService = new ExecutionService(Context);
        }

        [Fact]
        public async Task GetExecutionAsync_ShouldNotHaveNullStepsInCollection()
        {
            // Arrange - Create a plan and execution
            var plan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Step 1", Summary = "First step" },
                    new PlanStep { Name = "Step 2", Summary = "Second step" }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            
            // Clear the context to simulate a fresh load from database
            Context.ChangeTracker.Clear();

            // Act - Load the execution using the service method
            var loadedExecution = await _executionService.GetExecutionAsync(execution.Id);

            // Assert
            Assert.NotNull(loadedExecution);
            Assert.NotNull(loadedExecution.Steps);
            Assert.Equal(2, loadedExecution.Steps.Count);
            
            // This is the key assertion - no step should be null
            Assert.DoesNotContain(loadedExecution.Steps, step => step == null);
            
            // Verify we can safely access properties on all steps
            foreach (var step in loadedExecution.Steps)
            {
                Assert.NotNull(step);
                Assert.NotNull(step.Name);
                Assert.NotNull(step.Summary);
                // This should not throw NullReferenceException
                var isComplete = step.CompleteOn.HasValue;
            }
        }

        [Fact]
        public async Task GetExecutionAsync_LinqOperations_ShouldNotThrowNullReferenceException()
        {
            // Arrange - Create a plan and execution with some completed steps
            var plan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Step 1", Summary = "First step" },
                    new PlanStep { Name = "Step 2", Summary = "Second step" },
                    new PlanStep { Name = "Step 3", Summary = "Third step" }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            
            // Complete the first step
            execution.Steps[0].CompleteOn = DateTime.Now;
            await Context.SaveChangesAsync();
            
            // Clear the context to simulate a fresh load from database
            Context.ChangeTracker.Clear();

            // Act - Load the execution and perform LINQ operations like in ExecutionTracker
            var loadedExecution = await _executionService.GetExecutionAsync(execution.Id);

            // Assert - These operations should not throw NullReferenceException
            Assert.NotNull(loadedExecution);
            
            // This is the operation that was failing in ExecutionTracker.razor line 708
            var incompleteSteps = loadedExecution.Steps.Where(s => !s.CompleteOn.HasValue).ToList();
            Assert.Equal(2, incompleteSteps.Count);
            
            // Other LINQ operations that should work
            var completedCount = loadedExecution.Steps.Count(s => s.CompleteOn.HasValue);
            Assert.Equal(1, completedCount);
            
            var firstIncomplete = loadedExecution.Steps.FirstOrDefault(s => !s.CompleteOn.HasValue);
            Assert.NotNull(firstIncomplete);
            
            // Progress calculation
            var totalSteps = loadedExecution.Steps.Count;
            var progress = (double)completedCount / totalSteps * 100;
            Assert.Equal(33.33, progress, 2);
        }

        [Fact]
        public async Task ExecutionSteps_ShouldHaveCorrectForeignKeys()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Step 1", Summary = "First step" }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);
            
            // Clear the context
            Context.ChangeTracker.Clear();

            // Act - Load execution step directly from context to check foreign keys
            var executionSteps = await Context.ExecutionSteps
                .Where(es => es.Id == execution.Id)
                .ToListAsync();

            // Assert
            Assert.Single(executionSteps);
            var step = executionSteps.First();
            
            Assert.Equal(execution.Id, step.Id);
            Assert.NotNull(step.Name);
            Assert.Equal("Step 1", step.Name);
        }
    }
}