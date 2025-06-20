using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Core.Services;
using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;
using Xunit;

namespace StepCue.TenantApp.Core.Tests.Services
{
    public class ExecutionServiceTests : TestBase
    {
        private readonly ExecutionService _executionService;

        public ExecutionServiceTests()
        {
            _executionService = new ExecutionService(Context);
        }

        [Fact]
        public async Task GetExecutionsQueryable_ShouldIncludeAllRelatedData()
        {
            // Arrange
            var plan = new Plan 
            { 
                Name = "Test Plan",
                Steps = new List<PlanStep> { new PlanStep { Name = "Plan Step" } },
                Members = new List<PlanMember> { new PlanMember { Name = "Plan Member" } }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = new Execution
            {
                Name = "Test Execution",
                PlanId = plan.Id,
                Plan = plan,
                Steps = new List<ExecutionStep>
                {
                    new ExecutionStep
                    {
                        Name = "Execution Step",
                        Summary = "Step summary",
                        AssignedMembers = new List<ExecutionMember>
                        {
                            new ExecutionMember { Name = "Assigned Member", EmailAddress = "assigned@test.com" }
                        }
                    }
                },
                Members = new List<ExecutionMember>
                {
                    new ExecutionMember { Name = "Execution Member", EmailAddress = "member@test.com" }
                }
            };

            Context.Executions.Add(execution);
            await Context.SaveChangesAsync();

            // Act
            var queryable = _executionService.GetExecutionsQueryable();
            var result = await queryable.ToListAsync();

            // Assert
            Assert.Single(result);
            var executionResult = result.First();
            
            // Verify Plan is loaded
            Assert.NotNull(executionResult.Plan);
            Assert.Equal("Test Plan", executionResult.Plan.Name);
            
            // Verify Steps are loaded
            Assert.Single(executionResult.Steps);
            Assert.Equal("Execution Step", executionResult.Steps.First().Name);
            
            // Verify Steps.AssignedMembers are loaded (ThenInclude)
            Assert.Single(executionResult.Steps.First().AssignedMembers);
            Assert.Equal("Assigned Member", executionResult.Steps.First().AssignedMembers.First().Name);
        }

        [Fact]
        public async Task GetExecutionAsync_ShouldReturnExecutionWithAllRelatedData()
        {
            // Arrange
            var plan = new Plan { Name = "Test Plan" };
            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = new Execution
            {
                Name = "Test Execution",
                PlanId = plan.Id,
                Plan = plan,
                Steps = new List<ExecutionStep>
                {
                    new ExecutionStep
                    {
                        Name = "Step 1",
                        AssignedMembers = new List<ExecutionMember>
                        {
                            new ExecutionMember { Name = "Member 1" }
                        }
                    }
                }
            };

            Context.Executions.Add(execution);
            await Context.SaveChangesAsync();

            // Act
            var result = await _executionService.GetExecutionAsync(execution.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Execution", result.Name);
            Assert.NotNull(result.Plan);
            Assert.Equal("Test Plan", result.Plan.Name);
            Assert.Single(result.Steps);
            Assert.Equal("Step 1", result.Steps.First().Name);
            Assert.Single(result.Steps.First().AssignedMembers);
            Assert.Equal("Member 1", result.Steps.First().AssignedMembers.First().Name);
        }

        [Fact]
        public async Task GetExecutionAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Act
            var result = await _executionService.GetExecutionAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateExecutionFromPlanAsync_ShouldCreateExecutionWithAllPlanData()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Source Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Plan Step 1", Summary = "Step 1 summary", Screenshot = new byte[] { 1, 2, 3 } },
                    new PlanStep { Name = "Plan Step 2", Summary = "Step 2 summary", Screenshot = new byte[] { 4, 5, 6 } }
                },
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Plan Member 1", EmailAddress = "member1@test.com" },
                    new PlanMember { Name = "Plan Member 2", EmailAddress = "member2@test.com" }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act
            var result = await _executionService.CreateExecutionFromPlanAsync(plan.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal($"Execution of {plan.Name}", result.Name);
            Assert.Equal(plan.Id, result.PlanId);
            Assert.NotNull(result.Plan);
            Assert.Equal(plan.Name, result.Plan.Name);

            // Verify members were copied correctly
            Assert.Equal(2, result.Members.Count);
            Assert.Contains(result.Members, m => m.Name == "Plan Member 1" && m.EmailAddress == "member1@test.com");
            Assert.Contains(result.Members, m => m.Name == "Plan Member 2" && m.EmailAddress == "member2@test.com");

            // Verify steps were copied correctly
            Assert.Equal(2, result.Steps.Count);
            Assert.Contains(result.Steps, s => s.Name == "Plan Step 1" && s.Summary == "Step 1 summary");
            Assert.Contains(result.Steps, s => s.Name == "Plan Step 2" && s.Summary == "Step 2 summary");

            // Verify screenshots were copied
            var step1 = result.Steps.FirstOrDefault(s => s.Name == "Plan Step 1");
            Assert.NotNull(step1);
            Assert.NotNull(step1.Screenshot);
            Assert.Equal(new byte[] { 1, 2, 3 }, step1.Screenshot);

            // Verify execution was saved to database
            var savedExecution = await Context.Executions
                .Include(e => e.Steps)
                .Include(e => e.Members)
                .Include(e => e.Plan)
                .FirstOrDefaultAsync(e => e.Id == result.Id);

            Assert.NotNull(savedExecution);
            Assert.Equal(2, savedExecution.Steps.Count);
            Assert.Equal(2, savedExecution.Members.Count);
        }

        [Fact]
        public async Task CreateExecutionFromPlanAsync_WithNonExistentPlan_ShouldReturnNull()
        {
            // Act
            var result = await _executionService.CreateExecutionFromPlanAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateExecutionFromPlanAsync_AssignedMembers_ShouldCopyFromPlanStep()
        {
            // This test verifies that AssignedMembers from PlanStep 
            // are correctly copied to ExecutionStep during execution creation
            
            // Arrange
            var member1 = new PlanMember { Name = "Member 1", EmailAddress = "member1@test.com" };
            var member2 = new PlanMember { Name = "Member 2", EmailAddress = "member2@test.com" };

            var plan = new Plan
            {
                Name = "Test Plan",
                Members = new List<PlanMember> { member1, member2 },
                Steps = new List<PlanStep>
                {
                    new PlanStep
                    {
                        Name = "Plan Step",
                        AssignedMembers = new List<PlanMember> { member1, member2 }
                    }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act
            var result = await _executionService.CreateExecutionFromPlanAsync(plan.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Steps);
            
            // Verify AssignedMembers are copied from PlanStep to ExecutionStep
            var executionStep = result.Steps.First();
            Assert.Equal(2, executionStep.AssignedMembers.Count);
            Assert.Contains(executionStep.AssignedMembers, am => am.Name == "Member 1" && am.EmailAddress == "member1@test.com");
            Assert.Contains(executionStep.AssignedMembers, am => am.Name == "Member 2" && am.EmailAddress == "member2@test.com");
        }

        [Fact]
        public async Task GetExecutionsQueryable_Members_ShouldBeIncluded()
        {
            // This test verifies that Execution.Members are included 
            // in the GetExecutionsQueryable method

            // Arrange
            var plan = new Plan { Name = "Test Plan" };
            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var execution = new Execution
            {
                Name = "Test Execution",
                PlanId = plan.Id,
                Plan = plan,
                Members = new List<ExecutionMember>
                {
                    new ExecutionMember { Name = "Member 1", EmailAddress = "member1@test.com" },
                    new ExecutionMember { Name = "Member 2", EmailAddress = "member2@test.com" }
                }
            };

            Context.Executions.Add(execution);
            await Context.SaveChangesAsync();

            // Clear context to force fresh load
            Context.ChangeTracker.Clear();

            // Act - Use GetExecutionsQueryable
            var queryable = _executionService.GetExecutionsQueryable();
            var results = await queryable.Where(e => e.Id == execution.Id).ToListAsync();

            // Assert
            Assert.Single(results);
            var result = results.First();

            // FIXED: Execution.Members are now included in GetExecutionsQueryable
            Assert.Equal(2, result.Members.Count);
            Assert.Contains(result.Members, m => m.Name == "Member 1" && m.EmailAddress == "member1@test.com");
            Assert.Contains(result.Members, m => m.Name == "Member 2" && m.EmailAddress == "member2@test.com");
        }

        [Fact]
        public async Task CreateExecutionFromPlanAsync_ShouldPreserveStepOrder()
        {
            // This test demonstrates that step order should be preserved when creating execution from plan
            
            // Arrange
            var plan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Third Step", Summary = "Should be third", Order = 3 },
                    new PlanStep { Name = "First Step", Summary = "Should be first", Order = 1 },
                    new PlanStep { Name = "Second Step", Summary = "Should be second", Order = 2 }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act
            var result = await _executionService.CreateExecutionFromPlanAsync(plan.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Steps.Count);
            
            // Steps should be ordered by Order property, and Order should be copied
            var orderedSteps = result.Steps.OrderBy(s => s.Order).ToList();
            
            Assert.Equal(1, orderedSteps[0].Order);
            Assert.Equal("First Step", orderedSteps[0].Name);
            
            Assert.Equal(2, orderedSteps[1].Order);
            Assert.Equal("Second Step", orderedSteps[1].Name);
            
            Assert.Equal(3, orderedSteps[2].Order);
            Assert.Equal("Third Step", orderedSteps[2].Name);
        }

        [Fact]
        public async Task CreateExecutionFromPlanAsync_ShouldHandleStepsWithZeroOrder()
        {
            // Test edge case where some steps have Order = 0
            
            // Arrange
            var plan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Unordered Step", Summary = "Should be first", Order = 0 },
                    new PlanStep { Name = "Second Step", Summary = "Should be second", Order = 2 },
                    new PlanStep { Name = "First Step", Summary = "Should be after unordered", Order = 1 }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act
            var result = await _executionService.CreateExecutionFromPlanAsync(plan.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Steps.Count);
            
            // Steps should be in order: Order 0, Order 1, Order 2
            var steps = result.Steps.ToList();
            
            Assert.Equal(0, steps[0].Order);
            Assert.Equal("Unordered Step", steps[0].Name);
            
            Assert.Equal(1, steps[1].Order);
            Assert.Equal("First Step", steps[1].Name);
            
            Assert.Equal(2, steps[2].Order);
            Assert.Equal("Second Step", steps[2].Name);
        }
    }
}