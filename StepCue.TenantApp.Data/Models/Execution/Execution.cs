using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class Execution
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        
        [ForeignKey("PlanId")]
        public Plan Plan { get; set; }
        
        public int PlanId { get; set; }
        
        [InverseProperty(nameof(ExecutionMember.Execution))]
        public List<ExecutionMember> Members { get; set; } = new();
        
        [InverseProperty(nameof(ExecutionStep.Execution))]
        public List<ExecutionStep> Steps { get; set; } = new();
    }
}
