using StepCue.TenantApp.Data.Models.Execution;

namespace StepCue.TenantApp.Web.Components.Dialogs.Models;

public class StepCompletionDialogModel
{
    public ExecutionStep Step { get; set; } = new();
    public string ResultSummary { get; set; } = string.Empty;
    public string WentWell { get; set; } = string.Empty;
    public string CouldBeBetter { get; set; } = string.Empty;
    public byte[]? Screenshot { get; set; }
}