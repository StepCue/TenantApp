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
        
        public int ExecutionStepId { get; set; }
        
        [ForeignKey("ExecutionStepId")]
        public ExecutionStep ExecutionStep { get; set; }
        
        public int ExecutionMemberId { get; set; }
        
        [ForeignKey("ExecutionMemberId")]
        public ExecutionMember ExecutionMember { get; set; }
        
        public bool IsApproved { get; set; }
        
        public DateTime? ApprovalDate { get; set; }
        
        public string Comments { get; set; } = string.Empty;
    }
}