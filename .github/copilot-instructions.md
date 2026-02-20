# GitHub Copilot Instructions for StepCue TenantApp

This file provides AI-specific guidelines and coding patterns for the StepCue TenantApp project.

## 🏗️ Project Architecture

### Solution Structure
- **StepCue.TenantApp.Web**: Blazor Server UI (MudBlazor 8.x)
- **StepCue.TenantApp.Core**: Business logic and services
- **StepCue.TenantApp.Data**: EF Core models and DbContext
- **StepCue.TenantApp.Core.Tests**: xUnit tests with in-memory database

### Technology Stack
- .NET 10
- Blazor Server (Interactive Server Components)
- Entity Framework Core 8.0.11 (In-Memory Database)
- MudBlazor 8.x
- xUnit + coverlet

## 🎯 Coding Conventions

### General Rules
- **Nullable Reference Types**: Enabled - always handle null properly
- **Implicit Usings**: Enabled - don't add redundant using statements
- **Async/Await**: Use consistently in service layer and Blazor components
- **No Comments**: Unless they match existing style or explain complex logic
- **Existing Libraries**: Use existing packages; avoid adding new dependencies

### Naming Conventions
- Services: `{Entity}Service` (e.g., `PlanService`, `ExecutionService`)
- Blazor Pages: `{Feature}List.razor`, `{Feature}Editor.razor`, `{Feature}Tracker.razor`
- Shared Components: Descriptive names (e.g., `MemberManager.razor`, `StepDiscussion.razor`)
- Test Classes: `{Service}Tests.cs`, `{Feature}WorkflowTests.cs`

## 📐 Common Patterns

### Service Pattern

```csharp
public class EntityService
{
    private readonly DataContext _context;

    public EntityService(DataContext context)
    {
        _context = context;
    }

    public async Task<List<Entity>> GetEntitiesAsync()
    {
        return await _context.Entities
            .Include(e => e.RelatedData)
            .ThenInclude(r => r.NestedData)
            .Include(e => e.OtherRelatedData)
            .ToListAsync();
    }

    public async Task<Entity> GetEntityAsync(int id)
    {
        return await _context.Entities
            .Include(e => e.RelatedData)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Entity> CreateEntityAsync(Entity entity)
    {
        _context.Entities.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}
```

### Update Pattern with Proper EF Core Tracking

**CRITICAL**: Always load existing entity with tracking before updating:

```csharp
public async Task<Entity> UpdateEntityAsync(Entity entity)
{
    // Load existing entity with all relationships
    var existing = await _context.Entities
        .Include(e => e.Children)
        .ThenInclude(c => c.GrandChildren)
        .Include(e => e.OtherRelation)
        .FirstOrDefaultAsync(e => e.Id == entity.Id);

    if (existing == null)
    {
        throw new InvalidOperationException($"Entity with ID {entity.Id} not found");
    }

    // Update scalar properties
    existing.Name = entity.Name;
    existing.Description = entity.Description;

    // Handle new vs existing children
    var newChildren = entity.Children.Where(c => c.Id == 0).ToList();
    var existingChildren = entity.Children.Where(c => c.Id > 0).ToList();

    // Add new children
    foreach (var child in newChildren)
    {
        existing.Children.Add(child);
    }

    // Update existing children
    foreach (var child in existingChildren)
    {
        var existingChild = existing.Children.FirstOrDefault(c => c.Id == child.Id);
        if (existingChild != null)
        {
            existingChild.Property = child.Property;
            
            // Clear and repopulate many-to-many relationships
            existingChild.RelatedItems.Clear();
            foreach (var item in child.RelatedItems)
            {
                var trackedItem = existing.OtherRelation.FirstOrDefault(i => i.Id == item.Id);
                if (trackedItem != null)
                {
                    existingChild.RelatedItems.Add(trackedItem);
                }
            }
        }
    }

    // Remove deleted children (if needed)
    var childrenToRemove = existing.Children
        .Where(ec => !entity.Children.Any(c => c.Id == ec.Id))
        .ToList();
    
    foreach (var child in childrenToRemove)
    {
        existing.Children.Remove(child);
    }

    await _context.SaveChangesAsync();
    return existing;
}
```

### Blazor Component Pattern

```csharp
@page "/feature/{id:int}"
@inject EntityService EntityService
@inject NavigationManager Navigation
@inject ISnackbar Snackbar

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    @if (entity == null)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudPaper Class="pa-4">
            <!-- MudBlazor components here -->
        </MudPaper>
    }
</MudContainer>

@code {
    [Parameter] public int Id { get; set; }
    
    private Entity? entity;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        entity = await EntityService.GetEntityAsync(Id);
    }
    
    private async Task SaveAsync()
    {
        try
        {
            await EntityService.UpdateEntityAsync(entity);
            Snackbar.Add("Saved successfully", Severity.Success);
            Navigation.NavigateTo("/feature-list");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }
}
```

### MudBlazor Dialog Pattern

```csharp
// In parent component
private async Task OpenDialogAsync()
### MudBlazor Dialog Pattern

MudBlazor 8.x uses a specific pattern for dialogs that must be followed exactly.

#### Opening a Dialog

```csharp
// In parent component
private async Task OpenDialogAsync()
{
    var parameters = new DialogParameters
    {
        ["Entity"] = entity
    };

    var options = new DialogOptions 
    { 
        MaxWidth = MaxWidth.Medium, 
        FullWidth = true,
        CloseOnEscapeKey = true,
        BackdropClick = false  // Prevent closing by clicking backdrop
    };

    var dialog = await DialogService.ShowAsync<EntityDialog>("Title", parameters, options);
    var result = await dialog.Result;

    if (!result.Canceled)
    {
        // Handle the returned data
        var returnedData = result.Data;
        await LoadDataAsync();
    }
}
```

#### Dialog Component Structure

**CRITICAL**: Use `IMudDialogInstance` (with the `I` prefix) as the cascading parameter type.

```csharp
// In dialog component (e.g., AddMemberDialog.razor)
@using MudBlazor

<MudDialog>
    <DialogContent>
        <MudTextField @bind-Value="InputValue"
                     Label="Input"
                     Variant="Variant.Outlined"
                     OnKeyDown="HandleKeyDown"
                     AutoFocus="true"
                     FullWidth="true" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" 
                   Variant="Variant.Filled"
                   OnClick="Submit" 
                   Disabled="@string.IsNullOrWhiteSpace(InputValue)">
            OK
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string InputValue { get; set; } = string.Empty;

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        if (!string.IsNullOrWhiteSpace(InputValue))
        {
            MudDialog.Close(DialogResult.Ok(InputValue));
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(InputValue))
        {
            Submit();
        }
        else if (e.Key == "Escape")
        {
            Cancel();
        }
    }
}
```

#### Dialog Best Practices

**✅ DO:**
- Use `IMudDialogInstance` (with `I` prefix) for the cascading parameter
- Use `MudDialog.Cancel()` for cancel action (no DialogResult needed)
- Use `MudDialog.Close(DialogResult.Ok(data))` for submit with data
- Use `MudDialog.Close(DialogResult.Cancel())` if you need to explicitly cancel with Close
- Add keyboard handlers (Enter to submit, Escape to cancel) for accessibility
- Set `AutoFocus="true"` on the primary input field
- Use `BackdropClick = false` if you want to prevent accidental closes

**❌ DON'T:**
- Don't use `MudDialogInstance` without the `I` prefix (doesn't exist in MudBlazor 8.x)
- Don't use `IDialogReference` as cascading parameter (this is for the return value)
- Don't use `dynamic` type (causes runtime errors)
- Don't use `MudDialogContainer` (internal type, not for public use)
- Don't forget to add `@using MudBlazor` directive

#### Common Dialog Issues and Solutions

**Issue**: Buttons don't respond to clicks
- **Solution**: Ensure cascading parameter is `IMudDialogInstance` (with `I`)

**Issue**: Runtime error about `MudDialogContainer` not having `Close` method
- **Solution**: Check the cascading parameter type - should be `IMudDialogInstance`, not `dynamic`

**Issue**: Dialog doesn't pass data back to parent
- **Solution**: Use `DialogResult.Ok(data)` when closing, and check `result.Data` in parent

**Issue**: Can't click inside dialog (clicks go through to background)
- **Solution**: Set `BackdropClick = false` in DialogOptions
```

## 🗄️ Entity Framework Core Guidelines

### Always Use Include for Related Data

```csharp
// ✅ CORRECT - Explicit includes
var execution = await _context.Executions
    .Include(e => e.Plan)
    .Include(e => e.Members)
    .Include(e => e.Steps).ThenInclude(s => s.AssignedMembers)
    .Include(e => e.Steps).ThenInclude(s => s.Approvals).ThenInclude(a => a.ExecutionMember)
    .Include(e => e.Steps).ThenInclude(s => s.Messages)
    .FirstOrDefaultAsync(e => e.Id == id);

// ❌ WRONG - Lazy loading not enabled
var execution = await _context.Executions.FirstOrDefaultAsync(e => e.Id == id);
// execution.Steps will be null!
```

### Use AsNoTracking for Read-Only Queries

```csharp
// For display-only lists
var plans = await _context.Plans
    .AsNoTracking()
    .Include(p => p.Steps)
    .Include(p => p.Members)
    .ToListAsync();
```

### Queryable Pattern for Reusability

```csharp
public IQueryable<Execution> GetExecutionsQueryable()
{
    return _context.Executions
        .Include(e => e.Plan)
        .Include(e => e.Members)
        .Include(e => e.Steps).ThenInclude(s => s.AssignedMembers)
        .Include(e => e.Steps).ThenInclude(s => s.Approvals);
}

public async Task<Execution> GetExecutionAsync(int id)
{
    return await GetExecutionsQueryable()
        .FirstOrDefaultAsync(e => e.Id == id);
}

public async Task<List<Execution>> GetRecentExecutionsAsync()
{
    return await GetExecutionsQueryable()
        .OrderByDescending(e => e.CreatedOn)
        .Take(10)
        .ToListAsync();
}
```

## 🧪 Testing Patterns

### Test Setup with In-Memory Database

```csharp
public class ServiceTests
{
    private DataContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new DataContext(options);
    }

    [Fact]
    public async Task TestMethod_Should_DoSomething()
    {
        // Arrange
        using var context = CreateContext();
        var service = new EntityService(context);
        
        var entity = new Entity { Name = "Test" };
        await context.Entities.AddAsync(entity);
        await context.SaveChangesAsync();
        
        // Act
        var result = await service.GetEntityAsync(entity.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }
}
```

### Test with Complex Relationships

```csharp
[Fact]
public async Task CreateExecutionFromPlan_Should_CopyAllData()
{
    // Arrange
    using var context = CreateContext();
    var service = new ExecutionService(context);
    
    var plan = new Plan
    {
        Name = "Test Plan",
        Members = new List<PlanMember>
        {
            new PlanMember { Name = "User 1", EmailAddress = "user1@test.com" }
        },
        Steps = new List<PlanStep>
        {
            new PlanStep 
            { 
                Name = "Step 1", 
                Order = 1, 
                StepType = StepType.Activity,
                AssignedMembers = new List<PlanMember>()
            }
        }
    };
    
    plan.Steps[0].AssignedMembers.Add(plan.Members[0]);
    
    await context.Plans.AddAsync(plan);
    await context.SaveChangesAsync();
    
    // Act
    var execution = await service.CreateExecutionFromPlanAsync(plan.Id);
    
    // Assert
    Assert.NotNull(execution);
    Assert.Single(execution.Members);
    Assert.Single(execution.Steps);
    Assert.Equal("Step 1", execution.Steps[0].Name);
}
```

## 🚨 Common Pitfalls to Avoid

### ❌ Don't Attach Untracked Entities

```csharp
// ❌ WRONG - Will cause tracking conflicts
public async Task UpdateEntityAsync(Entity entity)
{
    _context.Entities.Update(entity); // Don't do this!
    await _context.SaveChangesAsync();
}

// ✅ CORRECT - Load and update
public async Task UpdateEntityAsync(Entity entity)
{
    var existing = await _context.Entities.FindAsync(entity.Id);
    existing.Name = entity.Name;
    await _context.SaveChangesAsync();
}
```

### ❌ Don't Forget to Include Related Data

```csharp
// ❌ WRONG - Related data not loaded
var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id);
// plan.Steps will be null or empty!

// ✅ CORRECT
var plan = await _context.Plans
    .Include(p => p.Steps)
    .Include(p => p.Members)
    .FirstOrDefaultAsync(p => p.Id == id);
```

### ❌ Don't Use StateHasChanged() Excessively

```csharp
// ❌ WRONG - Unnecessary in most cases
private async Task LoadDataAsync()
{
    data = await Service.GetDataAsync();
    StateHasChanged(); // Usually not needed!
}

// ✅ CORRECT - Blazor calls StateHasChanged automatically after async operations
private async Task LoadDataAsync()
{
    data = await Service.GetDataAsync();
}
```

## 📝 Feature Development Workflow

### Adding a New Feature

1. **Data Layer** (`StepCue.TenantApp.Data`):
   - Add model class(es) in appropriate folder (`Models/Planning/` or `Models/Execution/`)
   - Add `DbSet<Entity>` to `DataContext.cs`
   - Configure relationships in `OnModelCreating` if needed

2. **Service Layer** (`StepCue.TenantApp.Core`):
   - Create service class (e.g., `NewFeatureService.cs`)
   - Implement CRUD methods following existing patterns
   - Use proper Include statements for related data
   - Handle entity tracking correctly in update methods

3. **UI Layer** (`StepCue.TenantApp.Web`):
   - Add pages in `/Components/Pages/FeatureName/`
   - Create shared components in `/Components/Shared/` if reusable
   - Register service in `Program.cs`: `builder.Services.AddScoped<NewFeatureService>();`
   - Add navigation link in `NavMenu.razor` if needed

4. **Tests** (`StepCue.TenantApp.Core.Tests`):
   - Create test class `NewFeatureServiceTests.cs`
   - Test CRUD operations
   - Test business logic and edge cases
   - Use in-memory database for isolation

## 🎨 MudBlazor Best Practices

### Use MudBlazor Components Consistently

```csharp
// Forms
<MudTextField @bind-Value="entity.Name" Label="Name" Required="true" />
<MudSelect @bind-Value="entity.Type" Label="Type">
    <MudSelectItem Value="@(StepType.Activity)">Activity</MudSelectItem>
    <MudSelectItem Value="@(StepType.GoNoGo)">Go/No-Go</MudSelectItem>
</MudSelect>

// Tables
<MudTable Items="@entities" Hover="true">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Name">@context.Name</MudTd>
        <MudTd DataLabel="Actions">
            <MudIconButton Icon="@Icons.Material.Filled.Edit" OnClick="@(() => Edit(context))" />
        </MudTd>
    </RowTemplate>
</MudTable>

// Buttons
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SaveAsync">
    Save
</MudButton>

// Notifications
Snackbar.Add("Operation successful", Severity.Success);
Snackbar.Add("Error occurred", Severity.Error);
```

## 🔍 Domain-Specific Rules

### Plan vs Execution
- **Plan**: Template/blueprint (reusable)
- **Execution**: Instance of a plan being carried out (one-time)
- Always create new ExecutionMember/ExecutionStep copies when creating executions
- Maintain reference to original PlanStep via `PlanStepId`

### Step Types
- **Activity**: Regular task - marked complete when done
- **GoNoGo**: Decision point - requires approvals from assigned members

### Fallback Workflow
- Steps can be cancelled (`IsCancelled = true`)
- Cancelled steps are considered "complete" for workflow purposes
- Track fallback origin with `FallbackOriginStepId`
- Store reason in `FallbackReason`

### Member Assignment
- Plan members can be assigned to steps
- When creating execution, copy assigned members as ExecutionMembers
- For Go/No-Go steps, create ExecutionStepApproval records for each assigned member

## 📚 Reference Links

- [README.md](../README.md) - Project overview and getting started
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [MudBlazor Documentation](https://mudblazor.com/)
- [EF Core Best Practices](https://learn.microsoft.com/en-us/ef/core/)

## ⚠️ Important Reminders

- **Never** skip loading existing entities before updating
- **Always** use Include/ThenInclude for related data
- **Don't** add new NuGet packages without consulting team
- **Follow** existing naming conventions and patterns
- **Write** tests for new functionality
- **Keep** services focused on business logic (no UI concerns)
- **Keep** components focused on UI (delegate to services)

---

**This file is automatically loaded by GitHub Copilot for AI-assisted development.**
