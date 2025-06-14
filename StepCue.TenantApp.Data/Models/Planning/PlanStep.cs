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
        public string Name { get; set; }
        public string Summary { get; set; }
        public byte[]? Screenshot { get; set; }
        public List<PlanMember> AssignedMembers { get; set; } = new();
    }
}
