using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    //represents a person involved in the execution of a plan, can be assigned to steps and activities, may or may not be part of the original planning
    public class ExecutionMember
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }
}
