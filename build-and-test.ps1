# Build + browsers + testes. Corre com: build-and-test.cmd (duplo clique) ou
#   powershell -NoProfile -ExecutionPolicy Bypass -File build-and-test.ps1
$ErrorActionPreference = 'Continue'
Set-Location -Path $PSScriptRoot
$log = Join-Path $PSScriptRoot 'build-and-test.log'

function Write-Log([string]$text) { $text | Out-File -FilePath $log -Append -Encoding utf8 }
function Invoke-Logged([string]$title, [scriptblock]$command) {
    Write-Log "=== $title ==="
    & $command 2>&1 | ForEach-Object { "$_" } | Out-File -FilePath $log -Append -Encoding utf8
    return $LASTEXITCODE
}

"=== $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File -FilePath $log -Encoding utf8

Invoke-Logged 'dotnet --list-sdks' { dotnet --list-sdks } | Out-Null

$build = Invoke-Logged 'dotnet build' { dotnet build -nologo -v minimal }
Write-Log "BUILD_EXIT=$build"
if ($build -ne 0) { Write-Log '=== FIM ==='; exit $build }

$playwrightScript = 'SauceDemo.Tests\bin\Debug\net10.0\playwright.ps1'
$install = Invoke-Logged 'playwright install chromium' { & $playwrightScript install chromium }
Write-Log "PLAYWRIGHT_INSTALL_EXIT=$install"

$gate = Invoke-Logged 'dotnet test (gate: sem KnownBug)' {
    dotnet test --no-build -nologo --filter 'Category!=KnownBug' `
        --logger 'console;verbosity=normal' --logger 'trx;LogFileName=results.trx' --results-directory TestResults
}
Write-Log "TEST_EXIT=$gate"

$known = Invoke-Logged 'dotnet test (KnownBug: devem falhar)' {
    dotnet test --no-build -nologo --filter 'Category=KnownBug' `
        --logger 'console;verbosity=normal' --logger 'trx;LogFileName=known-bugs.trx' --results-directory TestResults
}
Write-Log "KNOWNBUG_EXIT=$known"
Write-Log '=== FIM ==='
exit $gate
