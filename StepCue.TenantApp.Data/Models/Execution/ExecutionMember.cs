using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionMember : PlanMember
    {
        public int ExecutionId { get; set; }
        public Execution Execution { get; set; }
    }
}
