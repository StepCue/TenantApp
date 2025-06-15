using StepCue.TenantApp.Core.Services;
using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Core.Tests.Services
{
    public class GoNoGoStepTests : TestBase
    {
        private readonly ExecutionService _executionService;

        public GoNoGoStepTests()
        {
            _executionService = new ExecutionService(Context);
        }

        [Fact]
        public async Task CreateExecutionFromPlan_WithGoNoGoStep_ShouldCreateApprovalRecords()
        {
            // Arrange - Create a plan with a go/nogo step
            var plan = new Plan
            {
                Name = "Test Plan with Go/NoGo",
                Members = new List<PlanMember>
                {
                    new PlanMember { Name = "John Doe", EmailAddress = "john@example.com" },
                    new PlanMember { Name = "Jane Smith", EmailAddress = "jane@example.com" }
                },
                Steps = new List<PlanStep>
                {
                    new PlanStep 
                    { 
                        Name = "Regular Execution Step", 
                        Summary = "Regular step",
                        Order = 1,
                        StepType = StepType.Execution
                    },
                    new PlanStep 
                    { 
                        Name = "Go/NoGo Decision", 
                        Summary = "Go/NoGo checkpoint",
                        Order = 2,
                        StepType = StepType.GoNoGo,
                        AssignedMembers = new List<PlanMember>
                        {
                            new PlanMember { Name = "John Doe", EmailAddress = "john@example.com" },
                            new PlanMember { Name = "Jane Smith", EmailAddress = "jane@example.com" }
                        }
                    }
                }
            };

            Context.Plans.Add(plan);
            await Context.SaveChangesAsync();

            // Act - Create execution from plan
            var execution = await _executionService.CreateExecutionFromPlanAsync(plan.Id);

            // Assert
            Assert.NotNull(execution);
            Assert.Equal(2, execution.Steps.Count);

            var goNoGoStep = execution.Steps.FirstOrDefault(s => s.StepType == StepType.GoNoGo);
            Assert.NotNull(goNoGoStep);
            Assert.Equal("Go/NoGo Decision", goNoGoStep.Name);
            Assert.Equal(2, goNoGoStep.AssignedMembers.Count);

            // Reload execution to get approvals
            var reloadedExecution = await _executionService.GetExecutionAsync(execution.Id);
            var reloadedGoNoGoStep = reloadedExecution.Steps.FirstOrDefault(s => s.StepType == StepType.GoNoGo);
            
            Assert.NotNull(reloadedGoNoGoStep);
            Assert.Equal(2, reloadedGoNoGoStep.Approvals.Count);
            Assert.All(reloadedGoNoGoStep.Approvals, approval => Assert.False(approval.IsApproved));
        }

        [Fact]
        public void IsStepComplete_ExecutionStep_ShouldReturnTrueWhenCompleteOnSet()
        {
            // Arrange
            var executionStep = new ExecutionStep
            {
                StepType = StepType.Execution,
                CompleteOn = DateTime.Now
            };

            // Act & Assert
            Assert.True(_executionService.IsStepComplete(executionStep));
        }

        [Fact]
        public void IsStepComplete_ExecutionStep_ShouldReturnFalseWhenCompleteOnNotSet()
        {
            // Arrange
            var executionStep = new ExecutionStep
            {
                StepType = StepType.Execution,
                CompleteOn = null
            };

            // Act & Assert
            Assert.False(_executionService.IsStepComplete(executionStep));
        }

        [Fact]
        public void IsStepComplete_GoNoGoStep_ShouldReturnFalseWhenNoAssignedMembers()
        {
            // Arrange
            var goNoGoStep = new ExecutionStep
            {
                StepType = StepType.GoNoGo,
                AssignedMembers = new List<PlanMember>(),
                Approvals = new List<ExecutionStepApproval>()
            };

            // Act & Assert
            Assert.False(_executionService.IsStepComplete(goNoGoStep));
        }

        [Fact]
        public void IsStepComplete_GoNoGoStep_ShouldReturnFalseWhenNotAllMembersApproved()
        {
            // Arrange
            var member1 = new PlanMember { Id = 1, Name = "John", EmailAddress = "john@example.com" };
            var member2 = new PlanMember { Id = 2, Name = "Jane", EmailAddress = "jane@example.com" };

            var goNoGoStep = new ExecutionStep
            {
                StepType = StepType.GoNoGo,
                AssignedMembers = new List<PlanMember> { member1, member2 },
                Approvals = new List<ExecutionStepApproval>
                {
                    new ExecutionStepApproval { ExecutionMemberId = 1, IsApproved = true },
                    new ExecutionStepApproval { ExecutionMemberId = 2, IsApproved = false }
                }
            };

            // Act & Assert
            Assert.False(_executionService.IsStepComplete(goNoGoStep));
        }

        [Fact]
        public void IsStepComplete_GoNoGoStep_ShouldReturnTrueWhenAllMembersApproved()
        {
            // Arrange
            var member1 = new PlanMember { Id = 1, Name = "John", EmailAddress = "john@example.com" };
            var member2 = new PlanMember { Id = 2, Name = "Jane", EmailAddress = "jane@example.com" };

            var goNoGoStep = new ExecutionStep
            {
                StepType = StepType.GoNoGo,
                AssignedMembers = new List<PlanMember> { member1, member2 },
                Approvals = new List<ExecutionStepApproval>
                {
                    new ExecutionStepApproval { ExecutionMemberId = 1, IsApproved = true },
                    new ExecutionStepApproval { ExecutionMemberId = 2, IsApproved = true }
                }
            };

            // Act & Assert
            Assert.True(_executionService.IsStepComplete(goNoGoStep));
        }
    }
}