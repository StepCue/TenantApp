using StepCue.TenantApp.Data.Models.Execution;
using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Web.Components.Dialogs.Models;

public class StepApprovalDialogModel
{
    public ExecutionStep Step { get; set; } = new();
    public PlanMember? SelectedApprover { get; set; }
    public string Comments { get; set; } = string.Empty;
}