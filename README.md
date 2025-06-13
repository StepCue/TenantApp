# StepCue TenantApp

A Blazor Server application for managing plans and executions.

## Technologies Used

- .NET 9.0
- ASP.NET Core Blazor Server
- Entity Framework Core (In-Memory Database)
- MudBlazor

## Running the Application

### Using Docker

#### Build and run with docker-compose (recommended)
```bash
docker-compose up --build
```

#### Build and run manually
```bash
# Build the Docker image
docker build -t stepcue/tenantapp:latest .

# Run the container
docker run -p 8080:8080 stepcue/tenantapp:latest
```

The application will be available at `http://localhost:8080`

### Using .NET CLI

```bash
# Restore packages
dotnet restore

# Run the application
dotnet run --project StepCue.TenantApp.Web
```

## Docker Hub

The application is automatically built and pushed to Docker Hub when changes are made to the main or develop branches.

Images are tagged with:
- `{branch-name}-latest` - Latest build for the branch
- `{branch-name}-v{build-number}` - Specific build version

## Development

### Prerequisites
- .NET 9.0 SDK
- Docker (for containerization)

### Project Structure
- `StepCue.TenantApp.Web` - Main Blazor Server application
- `StepCue.TenantApp.Data` - Data layer with Entity Framework models and context