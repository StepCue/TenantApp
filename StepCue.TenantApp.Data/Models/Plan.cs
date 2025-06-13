using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Data.Models
{
    internal class Plan
    {
        public string Name { get; set; }
        public List<PlanStep> Steps { get; set; }   

        public List<PlanMember> Members { get; set; }
    }
}
