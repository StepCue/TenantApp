using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionStep
    {
        public int Id { get; set; }
        
        [ForeignKey("ExecutionId")]
        public Execution Execution { get; set; }
        
        public DateTime? StartedOn { get; set; }
        
        public DateTime? CompleteOn { get; set; }
        
        public string ResultSummary { get; set; } = string.Empty;
        
        public byte[]? ResultScreenshot { get; set; }

        //post-mortem properties
        public string WhatWentWell { get; set; } = string.Empty;
        
        public string WhatCouldBeBetter { get; set; } = string.Empty;

        // communication properties
        public List<ExecutionStepMessage> Messages { get; set; } = new();

        //approval properties

        public List<ExecutionStepApproval> Approvals { get; set; } = new();
        
        // Fallback-related properties
        public bool IsCancelled { get; set; }

        
        
        public int? FallbackOriginStepId { get; set; }
        
        public string FallbackReason { get; set; } = string.Empty;
        
        public int? PlanStepOrder { get; set; } // Tracks the original plan step order for fallback lookups

        //from planning

        public List<ExecutionMember> AssignedMembers { get; set; } = new();
        public string Name { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;
        public StepType StepType { get; set; } = StepType.Activity;

        public int Order { get; set; }
        public byte[]? Screenshot { get; set; }


    }
}
