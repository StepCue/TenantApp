# StepCue TenantApp

A comprehensive Blazor Server application for managing step-by-step plans and tracking their execution progress.

## Overview

StepCue TenantApp is a collaborative planning and execution management system that allows teams to:

- **Create Plans**: Design multi-step workflows with detailed instructions, summaries, and optional screenshots
- **Manage Team Members**: Assign team members to plans with email addresses for coordination
- **Execute Plans**: Transform plans into trackable executions where progress can be monitored
- **Track Progress**: Monitor the completion status of each step during execution
- **Collaborate**: Team members can add messages and updates to execution steps

The application is built with modern web technologies and provides a responsive, user-friendly interface for managing complex workflows and ensuring nothing falls through the cracks.

## Technologies Used

- .NET 8.0
- ASP.NET Core Blazor Server
- Entity Framework Core (In-Memory Database)
- MudBlazor

## Running the Application

### Using Docker (Recommended)

The easiest way to run StepCue TenantApp is using Docker, which provides a consistent environment with all dependencies included.

#### Quick Start with Docker Compose
```bash
# Clone the repository (if not already done)
git clone https://github.com/StepCue/TenantApp.git
cd TenantApp

# Build and start the application
docker compose up --build
```

#### Manual Docker Setup
```bash
# Build the Docker image
docker build -t thestamp/stepcue:latest .

# Run the container
docker run -p 8080:8080 thestamp/stepcue:latest
```

#### Accessing the Application
Once running, the application will be available at:
- **URL**: `http://localhost:8080`
- **Features**: Create plans, manage team members, execute workflows, and track progress
- **Data**: Uses an in-memory database, so data will reset when the container stops

#### Docker Environment Details
- **Port**: 8080 (mapped from container to host)
- **Environment**: Development mode with detailed error pages
- **Database**: In-memory Entity Framework Core database
- **Security**: Runs as non-root user inside container

### Using .NET CLI (For Development)

If you want to run the application locally for development:

```bash
# Restore packages
dotnet restore

# Run the application
dotnet run --project StepCue.TenantApp.Web
```

**Note**: Requires .NET 9.0 SDK or later installed on your system.

## Docker Hub

The application is automatically built and pushed to Docker Hub when changes are made to the main or develop branches.

Images are tagged with:
- `latest` - Latest build for the main branch
- `v{build-number}` - Specific build version for main branch
- `{branch-name}-latest` - Latest build for other branches (e.g., develop)
- `{branch-name}-v{build-number}` - Specific build version for other branches

## Development

### Prerequisites
- .NET 8.0 SDK
- Docker (for containerization)

### Project Structure
- `StepCue.TenantApp.Web` - Main Blazor Server application containing:
  - Blazor components and pages for the user interface
  - Services for business logic (PlanService, ExecutionService, FileService)
  - Program.cs with application configuration
- `StepCue.TenantApp.Data` - Data layer containing:
  - Entity Framework Core models for Plans, Executions, Steps, and Members
  - DataContext for database operations
  - Support for in-memory database for development and testing

### Key Features
- **Plan Management**: Create, edit, and organize multi-step plans
- **Team Collaboration**: Assign members to plans and executions
- **Execution Tracking**: Convert plans to trackable executions
- **Progress Monitoring**: Track completion status of individual steps
- **File Attachments**: Support for screenshots and file uploads
- **Responsive UI**: Built with MudBlazor for modern, accessible design