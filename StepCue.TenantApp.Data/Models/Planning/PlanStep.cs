using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Data.Models.Planning
{
    public class PlanStep
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public byte[]? Screenshot { get; set; }
        public StepType StepType { get; set; } = StepType.Execution;
        public List<PlanMember> AssignedMembers { get; set; } = new();
        public List<PlanStep> FallbackSteps { get; set; } = new();
    }
}
