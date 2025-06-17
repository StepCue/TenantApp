using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Web.Components.Dialogs.Models;

public class FallbackDialogModel
{
    public ExecutionStep Step { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public List<Fallback> CurrentFallbackSteps { get; set; } = new();
    public Dictionary<int, List<Fallback>> AvailableFallbackOptions { get; set; } = new();
    public int? SelectedFallbackStepOrder { get; set; }
}