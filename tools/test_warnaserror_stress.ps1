# tools/test_warnaserror_stress.ps1
# Adversarial Stress Testing of /warnaserror Enforcement and Release Test Execution
$ErrorActionPreference = "Continue"

$RootDir = Split-Path $PSScriptRoot -Parent
$Runner = Join-Path $PSScriptRoot "run_tests.ps1"
$Scanner = Join-Path $PSScriptRoot "check_clean_room.ps1"
$DotnetCmd = if (Test-Path "C:\Users\omarp\.dotnet\dotnet.exe") { "C:\Users\omarp\.dotnet\dotnet.exe" } else { "dotnet" }
$SlnPath = Join-Path $RootDir "RadianceStudio.sln"

Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host " STARTING ADVERSARIAL STRESS TEST: /warnaserror Enforcement" -ForegroundColor Magenta
Write-Host "==========================================================" -ForegroundColor Magenta

$global:AllPassed = $true
$global:TestCount = 0
$global:PassCount = 0

function Assert-Result([bool]$condition, [string]$description) {
    $global:TestCount++
    if ($condition) {
        Write-Host "  [PASS] $description" -ForegroundColor Green
        $global:PassCount++
    } else {
        Write-Host "  [FAIL] $description" -ForegroundColor Red
        $global:AllPassed = $false
    }
}

# Ensure clean build-server state
& $DotnetCmd build-server shutdown > $null 2>&1

# 1. Clean Baseline Verification
Write-Host "`n--- 1. Baseline Verification in Release Configuration ---" -ForegroundColor Cyan
$baseOut = & powershell -ExecutionPolicy Bypass -File $Runner 2>&1
$baseCode = $LASTEXITCODE
$baseStr = $baseOut | Out-String
Assert-Result ($baseCode -eq 0) "Baseline tools/run_tests.ps1 exits with code 0"
Assert-Result ($baseStr -match "Clean-Room Integrity Verified") "Baseline passes clean-room check"
Assert-Result ($baseStr -match "Passed!\s+-\s+Failed:\s+0") "Baseline passes all unit tests with 0 failures"
Assert-Result (-not ($baseStr -match "warning CS")) "Baseline compiles with 0 compiler warnings"

# 2. Inject CS0219 (Unused variable assignment) into src/RadianceStudio.Core
Write-Host "`n--- 2. Testing Warning Escalation (CS0219) in src/RadianceStudio.Core ---" -ForegroundColor Cyan
$srcProbe = Join-Path $RootDir "src\RadianceStudio.Core\WarningProbeCS0219.cs"
$codeSnippet1 = @"
namespace RadianceStudio.Core;
public static class WarningProbeCS0219
{
    public static void TriggerWarning()
    {
        int unusedVariable1 = 42;
    }
}
"@
Set-Content -Path $srcProbe -Value $codeSnippet1 -Force

# 2a. Run with tools/run_tests.ps1 (uses /warnaserror)
$warnOut = & powershell -ExecutionPolicy Bypass -File $Runner 2>&1
$warnCode = $LASTEXITCODE
$warnStr = $warnOut | Out-String
Assert-Result ($warnCode -ne 0) "tools/run_tests.ps1 exits with non-zero ($warnCode) when CS0219 is present"
Assert-Result ($warnStr -match "error CS0219") "CS0219 was escalated to 'error CS0219' by /warnaserror"

# 2b. Control test: Run dotnet build WITHOUT /warnaserror to confirm CS0219 is normally just a warning
$codeSnippet2 = @"
namespace RadianceStudio.Core;
public static class WarningProbeCS0219
{
    public static void TriggerWarning()
    {
        int unusedVariable2 = 99;
    }
}
"@
Set-Content -Path $srcProbe -Value $codeSnippet2 -Force
$controlOut = & $DotnetCmd build $SlnPath --configuration Release --verbosity minimal 2>&1
$controlCode = $LASTEXITCODE
$controlStr = $controlOut | Out-String
Assert-Result ($controlStr -match "warning CS0219") "Without /warnaserror, CS0219 produces 'warning CS0219'"
Assert-Result ($controlCode -eq 0) "Without /warnaserror, build succeeds (exit code 0) despite warning"

if (Test-Path $srcProbe) { Remove-Item $srcProbe -Force }

# 3. Inject CS8600 (Nullable conversion warning) into src/RadianceStudio.Core
Write-Host "`n--- 3. Testing Nullable Warning Escalation (CS8600) in src/RadianceStudio.Core ---" -ForegroundColor Cyan
$nullableProbe = Join-Path $RootDir "src\RadianceStudio.Core\NullableWarningProbe.cs"
$nullableSnippet = @"
namespace RadianceStudio.Core;
public static class NullableWarningProbe
{
    public static void TriggerNullableWarning()
    {
        string? maybeNull = null;
        string notNull = maybeNull;
        _ = notNull;
    }
}
"@
Set-Content -Path $nullableProbe -Value $nullableSnippet -Force

$nullOut = & powershell -ExecutionPolicy Bypass -File $Runner 2>&1
$nullCode = $LASTEXITCODE
$nullStr = $nullOut | Out-String
Assert-Result ($nullCode -ne 0) "tools/run_tests.ps1 exits with non-zero ($nullCode) on nullable warning CS8600"
Assert-Result ($nullStr -match "error CS8600") "CS8600 was escalated to 'error CS8600' by /warnaserror"

if (Test-Path $nullableProbe) { Remove-Item $nullableProbe -Force }

# 4. Inject CS0219 into tests/RadianceStudio.Core.Tests
Write-Host "`n--- 4. Testing Warning Escalation in tests/RadianceStudio.Core.Tests ---" -ForegroundColor Cyan
$testProbe = Join-Path $RootDir "tests\RadianceStudio.Core.Tests\WarningProbeTests.cs"
$testSnippet = @"
using Xunit;
namespace RadianceStudio.Core.Tests;
public class WarningProbeTests
{
    [Fact]
    public void TestWarning()
    {
        int unusedInTest = 999;
        Assert.True(true);
    }
}
"@
Set-Content -Path $testProbe -Value $testSnippet -Force

$testWarnOut = & powershell -ExecutionPolicy Bypass -File $Runner 2>&1
$testWarnCode = $LASTEXITCODE
$testWarnStr = $testWarnOut | Out-String
Assert-Result ($testWarnCode -ne 0) "tools/run_tests.ps1 exits with non-zero ($testWarnCode) when test project has CS0219"
Assert-Result ($testWarnStr -match "error CS0219") "CS0219 in test project was escalated to 'error CS0219'"

if (Test-Path $testProbe) { Remove-Item $testProbe -Force }

# 5. Testing Debug configuration parameter
Write-Host "`n--- 5. Testing Debug Configuration Parameter ---" -ForegroundColor Cyan
Set-Content -Path $srcProbe -Value $codeSnippet1 -Force
$debugOut = & powershell -ExecutionPolicy Bypass -File $Runner -Configuration Debug 2>&1
$debugCode = $LASTEXITCODE
$debugStr = $debugOut | Out-String
Assert-Result ($debugCode -ne 0) "tools/run_tests.ps1 -Configuration Debug fails on warning"
Assert-Result ($debugStr -match "error CS0219") "Debug configuration also escalates CS0219 to error"
if (Test-Path $srcProbe) { Remove-Item $srcProbe -Force }

# 6. Post-test cleanup and final clean verification
Write-Host "`n--- 6. Final Clean Verification ---" -ForegroundColor Cyan
& $DotnetCmd build-server shutdown > $null 2>&1
$finalOut = & powershell -ExecutionPolicy Bypass -File $Runner 2>&1
$finalCode = $LASTEXITCODE
$finalStr = $finalOut | Out-String
Assert-Result ($finalCode -eq 0) "Post-cleanup tools/run_tests.ps1 exits with 0"
Assert-Result ($finalStr -match "\[SUCCESS\]") "tools/run_tests.ps1 outputs [SUCCESS]"

Write-Host "`n==========================================================" -ForegroundColor Magenta
Write-Host " RESULTS: $global:PassCount / $global:TestCount tests passed" -ForegroundColor Magenta
Write-Host "==========================================================" -ForegroundColor Magenta

if ($global:AllPassed) {
    exit 0
} else {
    exit 1
}
