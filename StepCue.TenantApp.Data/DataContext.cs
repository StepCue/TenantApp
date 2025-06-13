using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StepCue.TenantApp.Data.Models;

namespace StepCue.TenantApp.Data
{
    internal class DataContext : DbContext
    {
        public DbSet<Plan> Plans { get; set; }
    }
}
