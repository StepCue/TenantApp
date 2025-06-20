using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StepCue.TenantApp.Data.Models.Planning
{
    public class PlanMember
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string EmailAddress { get; set; } = string.Empty;
        
    }
}
