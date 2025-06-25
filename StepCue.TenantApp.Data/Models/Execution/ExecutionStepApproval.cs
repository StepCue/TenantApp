using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionStepApproval
    {
        [Key]
        public int Id { get; set; }

        public ExecutionStep ExecutionStep { get; set; }
        
        public ExecutionMember ExecutionMember { get; set; }
        
        public bool IsApproved { get; set; }
        
        public DateTime? ApprovalDate { get; set; }
        
        public string Comments { get; set; } = string.Empty;
    }
}