using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionStepApproval
    {
        public int Id { get; set; }
        public int ExecutionStepId { get; set; }
        public ExecutionStep ExecutionStep { get; set; }
        public int ExecutionMemberId { get; set; }
        public ExecutionMember ExecutionMember { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string Comments { get; set; } = string.Empty;
    }
}