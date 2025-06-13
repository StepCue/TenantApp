using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Data.Models
{
    public class PlanMember
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;

        public int PlanId { get; set; }
        public Plan? Plan { get; set; }
    }
}
