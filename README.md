# StepCue TenantApp

A comprehensive .NET 10 Blazor application for managing multi-step plans and tracking their execution with team collaboration features.

## 🎯 Project Overview

StepCue TenantApp is a plan execution and tracking system that allows teams to:
- Create detailed multi-step plans with assigned team members
- Execute plans with real-time progress tracking
- Support activity steps and Go/No-Go decision points
- Enable team collaboration through discussions and approvals
- Implement fallback workflows for handling step failures
- Capture screenshots and documentation at each step

## 🏗️ Architecture

### Solution Structure

```
StepCue.TenantApp/
├── StepCue.TenantApp.Web/              # Blazor Server UI Layer
├── StepCue.TenantApp.Core/             # Business Logic Layer
├── StepCue.TenantApp.Data/             # Data Access Layer
└── StepCue.TenantApp.Core.Tests/       # Unit Tests
```

### Technology Stack

- **Framework**: .NET 10
- **UI Framework**: Blazor Server (Interactive Server Components)
- **Component Library**: MudBlazor 8.x
- **Database**: Entity Framework Core 8.0.11 with In-Memory Database
- **Testing**: xUnit with coverlet for code coverage
- **Containerization**: Docker with multi-stage builds
- **CI/CD**: GitHub Actions

## 📦 Project Details

### StepCue.TenantApp.Web
**Type**: Blazor Server Application  
**Entry Point**: `Program.cs`

**Key Features**:
- Interactive server-side rendering
- MudBlazor-based UI components
- File upload service for screenshots
- Circuit options with detailed errors enabled

**Main Components**:
- `/Components/Pages/Plans/` - Plan management pages
  - `PlansList.razor` - List all plans
  - `PlanEditor.razor` - Create/edit plans
- `/Components/Pages/Executions/` - Execution tracking pages
  - `ExecutionsList.razor` - List all executions
  - `ExecutionTracker.razor` - Real-time execution tracking
- `/Components/Shared/` - Reusable components
  - `MemberManager.razor` - Team member management
  - `StepDiscussion.razor` - Step-level messaging
  - `StepEditorPopup.razor` - Step editing interface
  - `ScreenshotUploader.razor` - Image upload component
- `/Components/Layout/` - Layout components
  - `MainLayout.razor` - Application shell
  - `NavMenu.razor` - Navigation menu

**Dependencies**:
- Microsoft.EntityFrameworkCore 8.0.11
- Microsoft.EntityFrameworkCore.InMemory 8.0.11
- MudBlazor 8.x

### StepCue.TenantApp.Core
**Type**: Class Library  
**Purpose**: Business logic and service layer

**Services**:
- `PlanService.cs` - CRUD operations for plans
  - Create, read, update plans
  - Manage plan steps and member assignments
  - Handle complex entity relationship updates
- `ExecutionService.cs` - Execution lifecycle management
  - Create executions from plans
  - Track step progress and completion
  - Manage approvals and messages
  - Handle fallback workflows

**Key Responsibilities**:
- Business logic encapsulation
- Data validation and processing
- Complex query construction with EF Core
- Transaction management

### StepCue.TenantApp.Data
**Type**: Class Library  
**Purpose**: Data models and Entity Framework context

**Database Context**:
- `DataContext.cs` - EF Core DbContext
  - Configured for In-Memory database
  - Entity relationship mappings

**Domain Models**:

**Planning Models** (`Models/Planning/`):
- `Plan.cs` - Overall plan definition
  - Contains steps and team members
- `PlanStep.cs` - Individual plan step
  - Order, name, summary, screenshot
  - Step type (Activity or Go/No-Go)
  - Assigned members
  - Fallback activities
- `PlanMember.cs` - Team member in planning phase
  - Name and email address
- `FallbackActivity.cs` - Alternative actions if step fails

**Execution Models** (`Models/Execution/`):
- `Execution.cs` - Instance of a plan being executed
  - References original plan
  - Contains execution-specific steps and members
  - Creation timestamp
- `ExecutionStep.cs` - Step execution state
  - Start/complete timestamps
  - Result summary and screenshots
  - Post-mortem fields (what went well, what could be better)
  - Messages and approvals
  - Cancellation and fallback tracking
  - Reference to original PlanStep
- `ExecutionMember.cs` - Participant in execution
- `ExecutionStepMessage.cs` - Discussion messages on steps
- `ExecutionStepApproval.cs` - Approval records for Go/No-Go steps

**Shared Models**:
- `StepType.cs` - Enum for step types
  - `Activity (0)` - Regular task step
  - `GoNoGo (1)` - Decision/approval checkpoint

### StepCue.TenantApp.Core.Tests
**Type**: xUnit Test Project  
**Purpose**: Unit and integration tests

**Test Coverage**:
- `PlanServiceTests.cs` - Plan CRUD operations
- `ExecutionServiceTests.cs` - Execution creation and management
- `ExecutionStepLoadingTests.cs` - Step loading and querying
- `GoNoGoStepTests.cs` - Approval workflow logic
- `ApprovalWorkflowTests.cs` - Multi-approver scenarios
- `FallbackWorkflowTests.cs` - Fallback activity handling

**Test Infrastructure**:
- In-memory database for isolated tests
- xUnit test framework
- Visual Studio test runner support
- Code coverage with coverlet

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- Docker (optional, for containerized deployment)
- Visual Studio 2022 or VS Code with C# extension

### Local Development

1. **Clone the repository**:
   ```bash
   git clone https://github.com/StepCue/TenantApp.git
   cd TenantApp
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the application**:
   ```bash
   dotnet run --project StepCue.TenantApp.Web
   ```

4. **Access the application**:
   - Navigate to `https://localhost:5001` or `http://localhost:5000`

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbosity
dotnet test --verbosity normal

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🐳 Docker Deployment

### Building the Docker Image

```bash
docker build -t stepcue-tenantapp .
```

### Running the Container

```bash
docker run -p 8080:8080 stepcue-tenantapp
```

Access the application at `http://localhost:8080`

### Docker Image Details

- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:10.0`
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:10.0`
- **Multi-stage Build**: Optimized for size and security
- **Non-root User**: Runs as `appuser` for enhanced security
- **Port**: 8080 (configurable via `ASPNETCORE_URLS`)

## 🔄 CI/CD Pipeline

### GitHub Actions Workflow

**Workflow File**: `.github/workflows/docker-build-push.yml`

**Triggers**:
- Push to any branch
- Pull requests

**Pipeline Steps**:
1. Checkout code
2. Extract and sanitize branch name
3. Setup .NET 10 SDK
4. Run unit tests
5. Build Docker image
6. Login to Docker Hub (for non-PR branches)
7. Tag and push images

**Image Tags**:
- **Main branch**:
  - `latest`
  - `v{run_number}` (e.g., v123)
- **Feature branches**:
  - `{branch-name}-latest`
  - `{branch-name}-v{run_number}`

**Required Secrets**:
- `DOCKER_USER` - Docker Hub username
- `DOCKER_PASSWORD` - Docker Hub password

**Docker Repository**: `stepcue` on Docker Hub

## 📊 Domain Model

### Core Concepts

1. **Plan**: A template defining a series of steps to accomplish a goal
   - Contains ordered steps
   - Has team members that can be assigned to steps
   - Can be reused to create multiple executions

2. **Execution**: A specific instance of a plan being carried out
   - Created from a plan template
   - Tracks actual progress and results
   - Maintains independent member list and step states
   - Records timestamps and outcomes

3. **Step Types**:
   - **Activity**: Regular task requiring completion
   - **Go/No-Go**: Decision checkpoint requiring approvals

4. **Fallback Workflow**:
   - Steps can be cancelled if they encounter issues
   - Alternative fallback activities can be defined
   - Execution can continue with modified approach

5. **Collaboration Features**:
   - Step-level messaging for team communication
   - Screenshot capture for documentation
   - Approval tracking for Go/No-Go decisions
   - Post-mortem analysis (what went well/could be better)

## 🔑 Key Features

### Plan Management
- Create and edit reusable plan templates
- Define sequential steps with descriptions
- Assign team members to specific steps
- Upload reference screenshots
- Configure step types (Activity vs Go/No-Go)
- Define fallback activities for risk mitigation

### Execution Tracking
- Launch executions from plan templates
- Real-time progress monitoring
- Step-by-step workflow guidance
- Mark steps as started/completed
- Capture result screenshots and summaries
- Cancel steps and trigger fallback workflows

### Collaboration
- Team member management
- Step-level discussion threads
- Approval workflows for Go/No-Go steps
- Multi-approver support
- Email integration for notifications

### Post-Mortem Analysis
- Capture lessons learned per step
- Document what went well
- Identify areas for improvement
- Support continuous process refinement

## 🗄️ Data Persistence

### Current Implementation
- **In-Memory Database**: Used for development and testing
- **Entity Framework Core**: ORM for data access
- **Eager Loading**: Comprehensive include strategies for related entities

### Production Considerations
To migrate to a persistent database (SQL Server, PostgreSQL, etc.):

1. Update `Program.cs` in StepCue.TenantApp.Web:
   ```csharp
   // Replace
   builder.Services.AddDbContext<DataContext>(options =>
       options.UseInMemoryDatabase("StepCueInMemoryDb"));

   // With (example for SQL Server)
   builder.Services.AddDbContext<DataContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

2. Add connection string to `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=...;Database=StepCueDb;..."
     }
   }
   ```

3. Install appropriate NuGet package:
   ```bash
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   ```

4. Create and apply migrations:
   ```bash
   dotnet ef migrations add InitialCreate --project StepCue.TenantApp.Data
   dotnet ef database update --project StepCue.TenantApp.Web
   ```

## 🧪 Testing Strategy

### Test Categories

1. **Service Tests**: Validate business logic in Core services
2. **Workflow Tests**: End-to-end scenario testing
3. **Step Tests**: Specific step behavior validation
4. **Approval Tests**: Multi-user approval scenarios
5. **Fallback Tests**: Error handling and alternative workflows

### Test Database
All tests use in-memory database for:
- Fast execution
- Isolation between tests
- No external dependencies

## 📝 Code Conventions

- **Nullable Reference Types**: Enabled across all projects
- **Implicit Usings**: Enabled for cleaner code
- **Async/Await**: Consistent async patterns in service layer
- **EF Core Best Practices**:
  - Explicit Include statements for related data
  - Proper entity tracking management
  - Transaction management in service layer

## 🛠️ Development Guidelines for AI Assistance

### When Modifying Data Models
1. Update the model class in `StepCue.TenantApp.Data/Models/`
2. Update `DataContext.cs` if adding new DbSet
3. Update related services in `StepCue.TenantApp.Core/Services/`
4. Update Blazor components that use the model
5. Add/update corresponding tests
6. Consider migration impact if moving to persistent database

### When Adding New Features
1. Start with data model changes in Data project
2. Implement service logic in Core project
3. Create/update Blazor components in Web project
4. Add comprehensive tests in Tests project
5. Update this README with new functionality

### When Working with Entity Framework
- Always use `Include()` and `ThenInclude()` for related entities
- Be cautious with change tracking when updating entities
- Use `AsNoTracking()` for read-only queries
- Handle concurrency conflicts appropriately

### When Creating Blazor Components
- Follow MudBlazor component patterns
- Use interactive server render mode for dynamic features
- Leverage existing shared components when possible
- Maintain consistent styling with MudBlazor theme

### Common Patterns in Codebase

**Service Pattern**:
```csharp
public class SomeService
{
    private readonly DataContext _context;

    public SomeService(DataContext context)
    {
        _context = context;
    }

    public async Task<Entity> GetEntityAsync(int id)
    {
        return await _context.Entities
            .Include(e => e.RelatedData)
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}
```

**Update Pattern with Tracking**:
```csharp
// Load existing entity with tracking
var existing = await _context.Entities
    .Include(e => e.Children)
    .FirstOrDefaultAsync(e => e.Id == entity.Id);

// Update properties
existing.Property = newValue;

// Handle collections
existing.Children.Clear();
foreach (var child in newChildren)
{
    existing.Children.Add(child);
}

await _context.SaveChangesAsync();
```

**Blazor Component Pattern**:
```csharp
@page "/route"
@inject ServiceName Service

// Markup with MudBlazor components

@code {
    [Parameter] public int Id { get; set; }

    private Entity? entity;

    protected override async Task OnInitializedAsync()
    {
        entity = await Service.GetEntityAsync(Id);
    }
}
```

## 📚 Additional Resources

- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [MudBlazor Documentation](https://mudblazor.com/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [xUnit Documentation](https://xunit.net/)

## 🤝 Contributing

This README serves as a comprehensive guide for AI-assisted development. When making changes:

1. Ensure all tests pass
2. Follow existing code patterns
3. Update relevant documentation
4. Maintain backward compatibility when possible
5. Add tests for new functionality

## 📄 License

Repository: https://github.com/StepCue/TenantApp

---

**Last Updated**: 2024
**Version**: .NET 10 / EF Core 8.0.11
**Status**: Active Development