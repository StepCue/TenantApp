# Use the .NET 10.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution file and project files
COPY *.sln ./
COPY StepCue.TenantApp.Web/*.csproj StepCue.TenantApp.Web/
COPY StepCue.TenantApp.Data/*.csproj StepCue.TenantApp.Data/
COPY StepCue.TenantApp.Core/*.csproj StepCue.TenantApp.Core/
COPY StepCue.TenantApp.Core.Tests/*.csproj StepCue.TenantApp.Core.Tests/

# Restore NuGet packages
RUN dotnet restore

# Copy the rest of the application files
COPY . .

# Build the application
RUN dotnet build --configuration Release --no-restore

# Publish the application
RUN dotnet publish StepCue.TenantApp.Web/StepCue.TenantApp.Web.csproj --configuration Release --no-build --output /app/publish

# Use the .NET 10.0 runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create a non-root user for security
RUN useradd -m appuser && chown -R appuser /app
USER appuser

# Expose port 8080 (ASP.NET Core default in containers)
EXPOSE 8080

# Set environment variable for ASP.NET Core to listen on all interfaces
ENV ASPNETCORE_URLS=http://+:8080

# Set the entry point
ENTRYPOINT ["dotnet", "StepCue.TenantApp.Web.dll"]


