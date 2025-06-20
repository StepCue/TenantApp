using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Data.Models.Execution
{
    public class ExecutionStepMessage
    {
        [Key]
        public int Id { get; set; }
        
        public int ExecutionStepId { get; set; }
        
        [ForeignKey("ExecutionStepId")]
        public ExecutionStep ExecutionStep { get; set; }
        
        [ForeignKey("AuthorId")]
        public ExecutionMember Author { get; set; }
        
        public int AuthorId { get; set; }
        
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
