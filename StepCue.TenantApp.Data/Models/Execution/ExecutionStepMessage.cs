using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionStepMessage
    {
        public int Id { get; set; }
        public ExecutionMember Author { get; set; }
        public string Message { get; set; }
    }
}
