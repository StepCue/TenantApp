using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    //represents an instance of a plan being executed, with assigned members, steps, and results
    public class Execution
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public Plan Plan { get; set; }

        public List<ExecutionMember> Members { get; set; } = new();
        
        public List<ExecutionStep> Steps { get; set; } = new();
    }
}
