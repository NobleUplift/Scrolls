<#
.SYNOPSIS
    Builds Scrolls, compiles the render harness against it, runs it, and prints the result.

.DESCRIPTION
    The harness cannot print to stdout - it allocates its own console - so it writes a
    report file instead and this script echoes it back.

    Everything is built into a scratch directory so the committed binaries under
    Scrolls\bin\ are never touched.

.PARAMETER WorkDir
    Where to put build output and the report. Defaults to a temp directory.

.PARAMETER Full
    Echo the rendered frames as well as the PASS/FAIL lines.

.EXAMPLE
    .\tools\run-harness.ps1
    .\tools\run-harness.ps1 -Full
#>
[CmdletBinding()]
param(
    [string] $WorkDir = (Join-Path $env:TEMP 'scrolls-harness'),
    [switch] $Full
)

$ErrorActionPreference = 'Stop'

$repo    = Split-Path -Parent $PSScriptRoot
$msbuild = 'C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
$csc     = 'C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\csc.exe'

foreach ($tool in @($msbuild, $csc)) {
    if (-not (Test-Path $tool)) { throw "Not found: $tool" }
}

$build  = Join-Path $WorkDir 'build'
$obj    = Join-Path $WorkDir 'obj'
$report = Join-Path $WorkDir 'render-harness-report.txt'

New-Item -ItemType Directory -Force -Path $build | Out-Null

Write-Host 'Building Scrolls...' -ForegroundColor Cyan
& $msbuild (Join-Path $repo 'Scrolls.sln') `
    /p:Configuration=Debug /p:Platform=x86 `
    "/p:OutputPath=$build\" "/p:BaseIntermediateOutputPath=$obj\" `
    /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }

Write-Host 'Compiling harness...' -ForegroundColor Cyan
& $csc /nologo /target:exe /platform:x86 `
    "/out:$build\RenderHarness.exe" `
    "/r:$build\Scrolls.exe" `
    (Join-Path $PSScriptRoot 'RenderHarness.cs')
if ($LASTEXITCODE -ne 0) { throw "Harness compilation failed with exit code $LASTEXITCODE." }

Write-Host 'Running harness...' -ForegroundColor Cyan
# The harness opens its own console window; -WindowStyle Hidden keeps it out of the way.
$run = Start-Process -FilePath "$build\RenderHarness.exe" -ArgumentList $report `
                     -PassThru -Wait -WindowStyle Hidden

if (-not (Test-Path $report)) { throw 'Harness produced no report.' }

Write-Host ''
if ($Full) {
    Get-Content $report
} else {
    Get-Content $report | Where-Object { $_ -match '^\s*(PASS|FAIL|RESULT|=)' -or $_ -match '^\S.*[^|]$' }
    Write-Host ''
    Write-Host "Full report with rendered frames: $report" -ForegroundColor DarkGray
}

if ($run.ExitCode -ne 0) {
    Write-Host ''
    Write-Host 'Harness reported failures.' -ForegroundColor Red
}
exit $run.ExitCode
