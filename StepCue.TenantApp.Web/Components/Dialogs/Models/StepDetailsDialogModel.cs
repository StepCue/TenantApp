using StepCue.TenantApp.Data.Models.Execution;

namespace StepCue.TenantApp.Web.Components.Dialogs.Models;

public class StepDetailsDialogModel
{
    public ExecutionStep Step { get; set; } = new();
    public bool CanStart { get; set; }
    public bool IsCompleted { get; set; }
}