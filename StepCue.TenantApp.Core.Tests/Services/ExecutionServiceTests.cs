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
                        AssignedMembers = new List<PlanMember>
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
                        AssignedMembers = new List<PlanMember>
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
        public async Task CreateExecutionFromPlanAsync_AssignedMembers_ShouldBeCopied()
        {
            // This test verifies that AssignedMembers from PlanStep 
            // are copied to ExecutionStep during execution creation
            
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
            
            // FIXED: AssignedMembers should now be copied from PlanStep to ExecutionStep
            var executionStep = result.Steps.First();
            Assert.Equal(2, executionStep.AssignedMembers.Count);
            Assert.Contains(executionStep.AssignedMembers, m => m.Name == "Member 1" && m.EmailAddress == "member1@test.com");
            Assert.Contains(executionStep.AssignedMembers, m => m.Name == "Member 2" && m.EmailAddress == "member2@test.com");
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

            // FIXED: Execution.Members should now be included in GetExecutionsQueryable
            Assert.Equal(2, result.Members.Count);
            Assert.Contains(result.Members, m => m.Name == "Member 1" && m.EmailAddress == "member1@test.com");
            Assert.Contains(result.Members, m => m.Name == "Member 2" && m.EmailAddress == "member2@test.com");
        }
    }
}