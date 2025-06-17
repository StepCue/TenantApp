using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Core.Services;
using StepCue.TenantApp.Data.Models.Planning;
using Xunit;

namespace StepCue.TenantApp.Core.Tests.Services
{
    public class PlanServiceTests : TestBase
    {
        private readonly PlanService _planService;

        public PlanServiceTests()
        {
            _planService = new PlanService(Context);
        }

        [Fact]
        public async Task CreatePlanAsync_ShouldCreatePlanWithStepsAndMembers()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Step 1", Summary = "First step" },
                    new PlanStep { Name = "Step 2", Summary = "Second step" }
                },
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Member 1", EmailAddress = "member1@test.com" },
                    new PlanMember { Name = "Member 2", EmailAddress = "member2@test.com" }
                }
            };

            // Act
            var result = await _planService.CreatePlanAsync(plan);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("Test Plan", result.Name);
            Assert.Equal(2, result.Steps.Count);
            Assert.Equal(2, result.Members.Count);

            // Verify in database
            var savedPlan = await Context.Plans
                .Include(p => p.Steps)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == result.Id);

            Assert.NotNull(savedPlan);
            Assert.Equal(2, savedPlan.Steps.Count);
            Assert.Equal(2, savedPlan.Members.Count);
        }

        [Fact]
        public async Task CreateNewPlanAsync_ShouldCreatePlanWithDefaultName()
        {
            // Act
            var result = await _planService.CreateNewPlanAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("New Plan", result.Name);
            Assert.Empty(result.Steps);
            Assert.Empty(result.Members);

            // Verify in database
            var savedPlan = await Context.Plans
                .Include(p => p.Steps)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == result.Id);

            Assert.NotNull(savedPlan);
            Assert.Equal("New Plan", savedPlan.Name);
            Assert.Empty(savedPlan.Steps);
            Assert.Empty(savedPlan.Members);
        }

        [Fact]
        public async Task CreateNewPlanAsync_ShouldCreatePlanWithCustomName()
        {
            // Arrange
            var customName = "My Custom Plan";

            // Act
            var result = await _planService.CreateNewPlanAsync(customName);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(customName, result.Name);
            Assert.Empty(result.Steps);
            Assert.Empty(result.Members);

            // Verify in database
            var savedPlan = await Context.Plans
                .Include(p => p.Steps)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == result.Id);

            Assert.NotNull(savedPlan);
            Assert.Equal(customName, savedPlan.Name);
            Assert.Empty(savedPlan.Steps);
            Assert.Empty(savedPlan.Members);
        }

        [Fact]
        public async Task GetPlansAsync_ShouldReturnPlansWithStepsAndMembers()
        {
            // Arrange
            var plan1 = new Plan
            {
                Name = "Plan 1",
                Steps = new List<PlanStep> { new PlanStep { Name = "Step 1" } },
                Members = new List<PlanMember> { new PlanMember { Name = "Member 1" } }
            };
            var plan2 = new Plan
            {
                Name = "Plan 2",
                Steps = new List<PlanStep> { new PlanStep { Name = "Step A" }, new PlanStep { Name = "Step B" } },
                Members = new List<PlanMember> { new PlanMember { Name = "Member A" }, new PlanMember { Name = "Member B" } }
            };

            Context.Plans.AddRange(plan1, plan2);
            await Context.SaveChangesAsync();

            // Act
            var result = await _planService.GetPlansAsync();

            // Assert
            Assert.Equal(2, result.Count);
            
            var resultPlan1 = result.FirstOrDefault(p => p.Name == "Plan 1");
            Assert.NotNull(resultPlan1);
            Assert.Single(resultPlan1.Steps);
            Assert.Single(resultPlan1.Members);
            Assert.Equal("Step 1", resultPlan1.Steps.First().Name);
            Assert.Equal("Member 1", resultPlan1.Members.First().Name);

            var resultPlan2 = result.FirstOrDefault(p => p.Name == "Plan 2");
            Assert.NotNull(resultPlan2);
            Assert.Equal(2, resultPlan2.Steps.Count);
            Assert.Equal(2, resultPlan2.Members.Count);
        }

        [Fact]
        public async Task GetPlanAsync_ShouldReturnPlanWithStepsAndMembers()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Step 1", Summary = "First step" },
                    new PlanStep { Name = "Step 2", Summary = "Second step" }
                },
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Member 1", EmailAddress = "member1@test.com" },
                    new PlanMember { Name = "Member 2", EmailAddress = "member2@test.com" }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act
            var result = await _planService.GetPlanAsync(plan.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Plan", result.Name);
            Assert.Equal(2, result.Steps.Count);
            Assert.Equal(2, result.Members.Count);
            Assert.Equal("Step 1", result.Steps.First().Name);
            Assert.Equal("Member 1", result.Members.First().Name);
        }

        [Fact]
        public async Task GetPlanAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Act
            var result = await _planService.GetPlanAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePlanAsync_ShouldUpdatePlanPropertiesAndRelatedEntities()
        {
            // Arrange - Create initial plan
            var originalPlan = new Plan
            {
                Name = "Original Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Name = "Original Step", Summary = "Original summary" }
                },
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "Original Member", EmailAddress = "original@test.com" }
                }
            };

            Context.Plans.Add(originalPlan);
            await Context.SaveChangesAsync();

            // Arrange - Create updated plan
            var updatedPlan = new Plan
            {
                Id = originalPlan.Id,
                Name = "Updated Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep { Id = originalPlan.Steps.First().Id, Name = "Updated Step", Summary = "Updated summary" },
                    new PlanStep { Name = "New Step", Summary = "New step summary" } // New step with Id = 0
                },
                Members = new List<PlanMember>
                {
                    new PlanMember { Id = originalPlan.Members.First().Id, Name = "Updated Member", EmailAddress = "updated@test.com" },
                    new PlanMember { Name = "New Member", EmailAddress = "new@test.com" } // New member with Id = 0
                }
            };

            // Act
            var result = await _planService.UpdatePlanAsync(updatedPlan);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Plan", result.Name);
            Assert.Equal(2, result.Steps.Count);
            Assert.Equal(2, result.Members.Count);

            // Verify changes in database
            var savedPlan = await Context.Plans
                .Include(p => p.Steps)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == originalPlan.Id);

            Assert.NotNull(savedPlan);
            Assert.Equal("Updated Plan", savedPlan.Name);
            Assert.Equal(2, savedPlan.Steps.Count);
            Assert.Equal(2, savedPlan.Members.Count);

            var updatedStep = savedPlan.Steps.FirstOrDefault(s => s.Name == "Updated Step");
            Assert.NotNull(updatedStep);
            Assert.Equal("Updated summary", updatedStep.Summary);

            var newStep = savedPlan.Steps.FirstOrDefault(s => s.Name == "New Step");
            Assert.NotNull(newStep);
            Assert.Equal("New step summary", newStep.Summary);
        }

        [Fact]
        public async Task UpdatePlanAsync_WithNonExistentPlan_ShouldThrowException()
        {
            // Arrange
            var plan = new Plan { Id = 999, Name = "Non-existent Plan" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _planService.UpdatePlanAsync(plan));
        }

        [Fact]
        public async Task DeletePlanAsync_ShouldRemovePlan()
        {
            // Arrange
            var plan = new Plan
            {
                Name = "Plan to Delete",
                Steps = new List<PlanStep> { new PlanStep { Name = "Step to Delete" } },
                Members = new List<PlanMember> { new PlanMember { Name = "Member to Delete" } }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            var planId = plan.Id;

            // Act
            await _planService.DeletePlanAsync(planId);

            // Assert
            var deletedPlan = await Context.Plans.FindAsync(planId);
            Assert.Null(deletedPlan);
        }

        [Fact]
        public async Task DeletePlanAsync_WithNonExistentId_ShouldNotThrowException()
        {
            // Act & Assert - Should not throw
            await _planService.DeletePlanAsync(999);
        }

        [Fact]
        public async Task PlanStep_AssignedMembers_ShouldBeLoadedCorrectly()
        {
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
                        Name = "Step 1", 
                        Summary = "First step",
                        AssignedMembers = new List<PlanMember> { member1, member2 }
                    }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act - Verify that PlanStep.AssignedMembers relationship works
            var savedPlan = await Context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.AssignedMembers)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == plan.Id);

            // Assert
            Assert.NotNull(savedPlan);
            Assert.Single(savedPlan.Steps);
            var step = savedPlan.Steps.First();
            Assert.Equal(2, step.AssignedMembers.Count);
            Assert.Contains(step.AssignedMembers, am => am.Name == "Member 1");
            Assert.Contains(step.AssignedMembers, am => am.Name == "Member 2");
        }

        [Fact]
        public async Task UpdatePlanAsync_AssignMemberToMultipleSteps_ShouldSaveMemberToAllSteps()
        {
            // Arrange - Create a plan with 2 steps and 1 member
            var member = new PlanMember { Name = "Shared Member", EmailAddress = "shared@test.com" };
            var step1 = new PlanStep { Name = "Step 1", Summary = "First step", Order = 1 };
            var step2 = new PlanStep { Name = "Step 2", Summary = "Second step", Order = 2 };
            
            var originalPlan = new Plan
            {
                Name = "Test Plan",
                Steps = new List<PlanStep> { step1, step2 },
                Members = new List<PlanMember> { member }
            };

            Context.Plans.Add(originalPlan);
            await Context.SaveChangesAsync();

            // Arrange - Create updated plan with the same member assigned to both steps
            var updatedPlan = new Plan
            {
                Id = originalPlan.Id,
                Name = "Test Plan",
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Id = step1.Id, 
                        Name = "Step 1", 
                        Summary = "First step", 
                        Order = 1,
                        AssignedMembers = new List<PlanMember> { new PlanMember { Id = member.Id, Name = member.Name, EmailAddress = member.EmailAddress } }
                    },
                    new PlanStep 
                    { 
                        Id = step2.Id, 
                        Name = "Step 2", 
                        Summary = "Second step", 
                        Order = 2,
                        AssignedMembers = new List<PlanMember> { new PlanMember { Id = member.Id, Name = member.Name, EmailAddress = member.EmailAddress } }
                    }
                },
                Members = new List<PlanMember> { new PlanMember { Id = member.Id, Name = member.Name, EmailAddress = member.EmailAddress } }
            };

            // Act
            var result = await _planService.UpdatePlanAsync(updatedPlan);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Steps.Count);
            
            // Verify the member is assigned to both steps
            var savedStep1 = result.Steps.FirstOrDefault(s => s.Name == "Step 1");
            var savedStep2 = result.Steps.FirstOrDefault(s => s.Name == "Step 2");
            
            Assert.NotNull(savedStep1);
            Assert.NotNull(savedStep2);
            Assert.Single(savedStep1.AssignedMembers);
            Assert.Single(savedStep2.AssignedMembers);
            Assert.Equal("Shared Member", savedStep1.AssignedMembers.First().Name);
            Assert.Equal("Shared Member", savedStep2.AssignedMembers.First().Name);

            // Verify changes in database
            var savedPlan = await Context.Plans
                .Include(p => p.Steps)
                .ThenInclude(s => s.AssignedMembers)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == originalPlan.Id);

            Assert.NotNull(savedPlan);
            var dbStep1 = savedPlan.Steps.FirstOrDefault(s => s.Name == "Step 1");
            var dbStep2 = savedPlan.Steps.FirstOrDefault(s => s.Name == "Step 2");
            
            Assert.NotNull(dbStep1);
            Assert.NotNull(dbStep2);
            Assert.Single(dbStep1.AssignedMembers);
            Assert.Single(dbStep2.AssignedMembers);
            Assert.Equal("Shared Member", dbStep1.AssignedMembers.First().Name);
            Assert.Equal("Shared Member", dbStep2.AssignedMembers.First().Name);
        }
    }
}