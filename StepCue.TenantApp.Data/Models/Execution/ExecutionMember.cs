using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionMember : PlanMember
    {
        public int ExecutionId { get; set; }
        
        [ForeignKey("ExecutionId")]
        public Execution Execution { get; set; }
    }
}
