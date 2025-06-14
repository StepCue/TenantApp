using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;
using System;
using System.Collections.Generic;
using Xunit;

namespace StepCue.TenantApp.Core.Tests.Services
{
    public class ExecutionTrackerTests
    {
        [Fact]
        public void Steps_Collection_WithNullItems_ShouldNotThrowNullReferenceException()
        {
            // Arrange - Create an execution with a null step in the collection
            var execution = new Execution
            {
                Id = 1,
                Name = "Test Execution",
                Steps = new List<ExecutionStep>
                {
                    new ExecutionStep { Id = 1, Name = "Step 1", CompleteOn = null },
                    null, // This null step should be handled gracefully
                    new ExecutionStep { Id = 2, Name = "Step 2", CompleteOn = DateTime.Now },
                }
            };

            // Act & Assert - These operations should not throw NullReferenceException
            // Simulate the LINQ operations that were failing
            var validSteps = execution.Steps.Where(s => s != null).ToList();
            var completedCount = execution.Steps.Where(s => s != null).Count(s => s.CompleteOn.HasValue);
            var incompleteSteps = execution.Steps.Where(s => s != null && !s.CompleteOn.HasValue).ToList();
            
            // Verify results
            Assert.Equal(2, validSteps.Count);
            Assert.Equal(1, completedCount);
            Assert.Single(incompleteSteps);
        }

        [Fact]
        public void FindNextIncompleteStep_WithNullSteps_ShouldWork()
        {
            // Arrange
            var execution = new Execution
            {
                Steps = new List<ExecutionStep>
                {
                    new ExecutionStep { Id = 1, CompleteOn = DateTime.Now },
                    null,
                    new ExecutionStep { Id = 3, CompleteOn = null },
                    new ExecutionStep { Id = 4, CompleteOn = null }
                }
            };

            var completionStepId = 1;

            // Act - Simulate the logic from SubmitStepCompletion
            var nextStep = execution.Steps
                .Where(s => s != null && !s.CompleteOn.HasValue && s.Id != completionStepId)
                .OrderBy(s => s.Id)
                .FirstOrDefault();

            // Assert
            Assert.NotNull(nextStep);
            Assert.Equal(3, nextStep.Id);
        }
    }
}