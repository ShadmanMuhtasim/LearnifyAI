# ============================================
# LearnPlatform - Solution Scaffolding Script
# Milestone 1.1: Clean Architecture Setup
# ============================================

$ErrorActionPreference = "Stop"
$SolutionName = "LearnPlatform"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LearnPlatform Solution Scaffolding" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Create the solution
Write-Host "[1/6] Creating solution: $SolutionName..." -ForegroundColor Yellow
dotnet new sln -n $SolutionName
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create solution file."
    exit 1
}
Write-Host "  [OK] Solution '$SolutionName.sln' created." -ForegroundColor Green
Write-Host ""

# Step 2: Create Core Project (Domain Layer - POCOs)
Write-Host "[2/6] Creating Core project (Domain Layer)..." -ForegroundColor Yellow
dotnet new classlib -n LearnPlatform.Core -f net8.0
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Core project."
    exit 1
}
Write-Host "  [OK] LearnPlatform.Core created." -ForegroundColor Green
Write-Host ""

# Step 3: Create Application Project (Use Cases / CQRS)
Write-Host "[3/6] Creating Application project (Application Layer)..." -ForegroundColor Yellow
dotnet new classlib -n LearnPlatform.Application -f net8.0
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Application project."
    exit 1
}
Write-Host "  [OK] LearnPlatform.Application created." -ForegroundColor Green
Write-Host ""

# Step 4: Create Infrastructure Project (Persistence / External Services)
Write-Host "[4/6] Creating Infrastructure project (Infrastructure Layer)..." -ForegroundColor Yellow
dotnet new classlib -n LearnPlatform.Infrastructure -f net8.0
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Infrastructure project."
    exit 1
}
Write-Host "  [OK] LearnPlatform.Infrastructure created." -ForegroundColor Green
Write-Host ""

# Step 5: Create Web Project (Presentation Layer - ASP.NET Core MVC)
Write-Host "[5/6] Creating Web project (Presentation Layer - MVC)..." -ForegroundColor Yellow
dotnet new mvc -n LearnPlatform.Web -f net8.0
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Web project."
    exit 1
}
Write-Host "  [OK] LearnPlatform.Web created." -ForegroundColor Green
Write-Host ""

# Step 6: Add projects to solution
Write-Host "[6/6] Adding projects to solution..." -ForegroundColor Yellow
dotnet sln add LearnPlatform.Core/LearnPlatform.Core.csproj
dotnet sln add LearnPlatform.Application/LearnPlatform.Application.csproj
dotnet sln add LearnPlatform.Infrastructure/LearnPlatform.Infrastructure.csproj
dotnet sln add LearnPlatform.Web/LearnPlatform.Web.csproj
Write-Host "  [OK] All projects added to solution." -ForegroundColor Green
Write-Host ""

# Step 7: Add project references (Clean Architecture dependencies)
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Setting Up Project References" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Application references Core
Write-Host "[1/5] Adding: Application -> Core..." -ForegroundColor Yellow
dotnet add LearnPlatform.Application/LearnPlatform.Application.csproj reference LearnPlatform.Core/LearnPlatform.Core.csproj
Write-Host "  [OK] Application references Core." -ForegroundColor Green

# Infrastructure references Core
Write-Host "[2/5] Adding: Infrastructure -> Core..." -ForegroundColor Yellow
dotnet add LearnPlatform.Infrastructure/LearnPlatform.Infrastructure.csproj reference LearnPlatform.Core/LearnPlatform.Core.csproj
Write-Host "  [OK] Infrastructure references Core." -ForegroundColor Green

# Infrastructure references Application
Write-Host "[3/5] Adding: Infrastructure -> Application..." -ForegroundColor Yellow
dotnet add LearnPlatform.Infrastructure/LearnPlatform.Infrastructure.csproj reference LearnPlatform.Application/LearnPlatform.Application.csproj
Write-Host "  [OK] Infrastructure references Application." -ForegroundColor Green

# Web references Application
Write-Host "[4/5] Adding: Web -> Application..." -ForegroundColor Yellow
dotnet add LearnPlatform.Web/LearnPlatform.Web.csproj reference LearnPlatform.Application/LearnPlatform.Application.csproj
Write-Host "  [OK] Web references Application." -ForegroundColor Green

# Web references Infrastructure
Write-Host "[5/5] Adding: Web -> Infrastructure..." -ForegroundColor Yellow
dotnet add LearnPlatform.Web/LearnPlatform.Web.csproj reference LearnPlatform.Infrastructure/LearnPlatform.Infrastructure.csproj
Write-Host "  [OK] Web references Infrastructure." -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  [OK] Solution Scaffolding Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Project Reference Flow:" -ForegroundColor Cyan
Write-Host "  LearnPlatform.Web" -ForegroundColor White
Write-Host "    +-- LearnPlatform.Application" -ForegroundColor White
Write-Host "    |     +-- LearnPlatform.Core" -ForegroundColor White
Write-Host "    +-- LearnPlatform.Infrastructure" -ForegroundColor White
Write-Host "          +-- LearnPlatform.Application" -ForegroundColor White
Write-Host "          |     +-- LearnPlatform.Core" -ForegroundColor White
Write-Host "          +-- LearnPlatform.Core" -ForegroundColor White
Write-Host ""