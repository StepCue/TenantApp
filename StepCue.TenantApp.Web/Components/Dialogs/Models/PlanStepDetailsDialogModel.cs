using StepCue.TenantApp.Data.Models.Planning;

namespace StepCue.TenantApp.Web.Components.Dialogs.Models;

public class PlanStepDetailsDialogModel
{
    public PlanStep Step { get; set; } = new();
    public List<PlanMember> PlanMembers { get; set; } = new();
    public List<PlanStep> PlanSteps { get; set; } = new();
}