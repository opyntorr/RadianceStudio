# ==============================================================================
# Headless MCP E2E Verification Agent for RadianceStudio
# ==============================================================================
# Deploys an automated evaluator agent that boots the RadianceStudio MCP engine
# in headless mode, executes the complete 9-stage parametric jewelry pipeline,
# and verifies mathematical invariants, BOM calculations, and watertight STL generation.
# ==============================================================================

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent
$DotnetCmd = if (Test-Path "C:\Users\omarp\.dotnet\dotnet.exe") { "C:\Users\omarp\.dotnet\dotnet.exe" } else { "dotnet" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Headless MCP E2E Agent: Executing Jewelry Pipeline" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$TestProject = Join-Path $RootDir "tests\RadianceStudio.Core.Tests\RadianceStudio.Core.Tests.csproj"
$Filter = "FullyQualifiedName~HeadlessMcpE2EPipelineAgentTests"

$AgentStart = [System.Diagnostics.Stopwatch]::StartNew()

$Output = & $DotnetCmd test $TestProject `
    --configuration $Configuration `
    --no-build `
    --filter $Filter `
    --verbosity normal 2>&1

$ExitCode = $LASTEXITCODE
$AgentStart.Stop()

Write-Host ($Output | Out-String)

if ($ExitCode -eq 0) {
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "  [PASS] Headless MCP E2E Agent Verified Successfully!" -ForegroundColor Green
    Write-Host "  Execution Time: $($AgentStart.Elapsed.TotalSeconds.ToString('F2')) seconds" -ForegroundColor Gray
    Write-Host "  Pipeline Stages Verified:" -ForegroundColor Gray
    Write-Host "    [1/9] Parametric Ring Rail (US 7.0 Standard)" -ForegroundColor Gray
    Write-Host "    [2/9] Swept Shank Profile (Comfort-Fit)" -ForegroundColor Gray
    Write-Host "    [3/9] Gem Curve Distribution (Round Brilliant)" -ForegroundColor Gray
    Write-Host "    [4/9] Prong Head Setting (4-Prong Clasp)" -ForegroundColor Gray
    Write-Host "    [5/9] Parametric Gem Cutters (Seat & Light Hole)" -ForegroundColor Gray
    Write-Host "    [6/9] Adaptive Multi-Pass Boolean Operations" -ForegroundColor Gray
    Write-Host "    [7/9] BOM & Precious Metal Density Estimations" -ForegroundColor Gray
    Write-Host "    [8/9] Inverted-Normal Wall Thickness Inspection" -ForegroundColor Gray
    Write-Host "    [9/9] Watertight 2-Manifold Binary STL Export" -ForegroundColor Gray
    Write-Host "==========================================================" -ForegroundColor Green
    exit 0
} else {
    Write-Host "==========================================================" -ForegroundColor Red
    Write-Host "  [FAIL] Headless MCP E2E Agent Verification Failed!" -ForegroundColor Red
    Write-Host "==========================================================" -ForegroundColor Red
    exit 1
}
