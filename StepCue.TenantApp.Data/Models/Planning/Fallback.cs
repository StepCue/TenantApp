using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StepCue.TenantApp.Data.Models.Planning
{
    public class Fallback
    {
        [Key]
        public int Id { get; set; }
        
        public int Order { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string Summary { get; set; } = string.Empty;
        
        public byte[]? Screenshot { get; set; }
        
        public List<PlanMember> AssignedMembers { get; set; } = new();
        
    }
}