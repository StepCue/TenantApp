using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StepCue.TenantApp.Data.Models.Planning
{
    //represents a single step within a plan, which can be an activity or a decision point, and can have assigned members and fallback activities
    public class PlanStep
    {
        [Key]
        public int Id { get; set; }
        
        public int Order { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string Summary { get; set; } = string.Empty;
        
        public byte[]? Screenshot { get; set; }
        
        public StepType StepType { get; set; } = StepType.Activity;
        
        public List<PlanMember> AssignedMembers { get; set; } = new();
        public List<FallbackActivity> FallbackActivities { get; set; } = new();

    }
}
