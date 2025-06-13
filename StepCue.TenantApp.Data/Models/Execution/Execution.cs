using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class Execution
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public Plan Plan { get; set; }
        public int PlanId { get; set; }
        public List<ExecutionMember> Members { get; set; } = new();
        public List<ExecutionStep> Steps { get; set; } = new(); // Fixed property name from Step to Steps
    }
}
