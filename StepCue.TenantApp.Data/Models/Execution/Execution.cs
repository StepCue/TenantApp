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
        public Plan Plan { get; set; }
        public List<ExecutionMember> Members { get; set; }
        public List<ExecutionStep> Step { get; set; }

    }
}
