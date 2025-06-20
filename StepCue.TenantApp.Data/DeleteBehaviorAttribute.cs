using Microsoft.EntityFrameworkCore;
using System;

namespace StepCue.TenantApp.Data
{
    /// <summary>
    /// Attribute for specifying delete behavior in data annotations.
    /// This will be used in OnModelCreating to set the delete behavior for relationships.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DeleteBehaviorAttribute : Attribute
    {
        public DeleteBehavior DeleteBehavior { get; }

        public DeleteBehaviorAttribute(DeleteBehavior deleteBehavior)
        {
            DeleteBehavior = deleteBehavior;
        }
    }
}