using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepCue.TenantApp.Data.Models.Planning
{
    public class PlanMember
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
    }
}
