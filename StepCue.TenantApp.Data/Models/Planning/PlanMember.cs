using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StepCue.TenantApp.Data.Models.Planning
{
    //represents a person involved in the planning and execution of a plan, can be assigned to steps and activities
    public class PlanMember
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string EmailAddress { get; set; } = string.Empty;
        public List<PlanStep> Steps { get; set; }

        
    }
}
