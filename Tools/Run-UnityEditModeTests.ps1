
param(
    [string]$UnityExecutable = ""
)

$ErrorActionPreference = "Stop"

$projectPath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")
)

$versionFile = Join-Path $projectPath `
    "ProjectSettings\ProjectVersion.txt"

if (-not (Test-Path $versionFile)) {
    throw "Unity project version file not found."
}

$versionText = Get-Content $versionFile |
    Where-Object { $_ -match "^m_EditorVersion:" } |
    Select-Object -First 1

if ($versionText -notmatch "^m_EditorVersion:\s*(\S+)") {
    throw "Cannot determine Unity Editor version."
}

$unityVersion = $Matches[1]

if ([string]::IsNullOrWhiteSpace($UnityExecutable)) {
    $UnityExecutable = Join-Path $env:ProgramFiles `
        "Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
}

if (-not (Test-Path $UnityExecutable)) {
    throw "Unity Editor not found: $UnityExecutable"
}

$resultsDirectory = Join-Path $projectPath "Logs\UnityTests"

New-Item -ItemType Directory `
    -Path $resultsDirectory `
    -Force | Out-Null

$resultFile = Join-Path $resultsDirectory "EditMode.xml"
$logFile = Join-Path $resultsDirectory "EditMode.log"

Remove-Item $resultFile -ErrorAction SilentlyContinue

Write-Host "Unity version: $unityVersion"
Write-Host "Project: $projectPath"
Write-Host "Running EditMode tests..."

$unityArguments = @(
    "-batchmode"
    "-nographics"
    "-projectPath `"$projectPath`""
    "-runTests"
    "-testPlatform EditMode"
    "-testResults `"$resultFile`""
    "-logFile `"$logFile`""
) -join " "

$unityProcess = Start-Process `
    -FilePath $UnityExecutable `
    -ArgumentList $unityArguments `
    -WorkingDirectory $projectPath `
    -PassThru `
    -Wait

$exitCode = $unityProcess.ExitCode

if (-not (Test-Path $resultFile)) {
    throw "Unity did not produce test results. Exit code: $exitCode. See $logFile"
}

[xml]$report = Get-Content $resultFile -Raw

$testRun = $report.SelectSingleNode("/test-run")

if ($null -eq $testRun) {
    throw "Invalid test report format: $resultFile"
}

$passed = [int]$testRun.GetAttribute("passed")
$failed = [int]$testRun.GetAttribute("failed")
$skipped = [int]$testRun.GetAttribute("skipped")
$result = $testRun.GetAttribute("result")

Write-Host ""
Write-Host "EditMode results:"
Write-Host "Passed:  $passed"
Write-Host "Failed:  $failed"
Write-Host "Skipped: $skipped"
Write-Host "Result:  $result"

if ($exitCode -ne 0 -or $failed -ne 0 -or
    $passed -eq 0 -or $result -ne "Passed") {

    Write-Host "Report: $resultFile"
    Write-Host "Log:    $logFile"
    exit 1
}

Write-Host "All EditMode tests passed."
exit 0
