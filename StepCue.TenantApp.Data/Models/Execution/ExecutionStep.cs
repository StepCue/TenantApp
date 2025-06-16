using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionStep : PlanStep
    {
        public int ExecutionId { get; set; }
        public Execution Execution { get; set; }
        public DateTime? StartedOn { get; set; }
        public DateTime? CompleteOn { get; set; }
        public string ResultSummary { get; set; } = string.Empty;
        public byte[]? ResultScreenshot { get; set; }
        public string WhatWentWell { get; set; } = string.Empty;
        public string WhatCouldBeBetter { get; set; } = string.Empty;
        public List<ExecutionStepMessage> Messages { get; set; } = new();
        public List<ExecutionStepApproval> Approvals { get; set; } = new();
        
        // Fallback-related properties
        public bool IsCancelled { get; set; }
        public int? FallbackOriginStepId { get; set; }
        public string FallbackReason { get; set; } = string.Empty;
    }
}
