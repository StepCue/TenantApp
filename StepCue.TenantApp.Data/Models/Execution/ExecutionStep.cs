using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionStep : PlanStep
    {
        public DateTime? StartedOn { get; set; }
        public DateTime? CompleteOn { get; set; }
        public string ResultSummary { get; set; }

    }
}
