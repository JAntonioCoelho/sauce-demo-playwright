<#
  Escreve o resumo do run no GITHUB_STEP_SUMMARY a partir dos .trx:
  totais do gate, falhas do gate e o estado de cada bug conhecido.
  Um KnownBug que passa gera um aviso: o bug pode ter sido corrigido e o teste deve sair da categoria.
#>
param([string]$ResultsDirectory = 'TestResults', [string]$Browser = '')

function Read-Trx([string]$path) {
    if (-not (Test-Path $path)) { return @() }
    [xml]$xml = Get-Content -Path $path -Raw
    @($xml.TestRun.Results.UnitTestResult) | Where-Object { $_ } | ForEach-Object {
        [pscustomobject]@{
            Name    = $_.testName
            Outcome = $_.outcome
            Message = "$($_.Output.ErrorInfo.Message)".Trim()
        }
    }
}

function First-Line([string]$text) {
    $line = ($text -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
    if (-not $line) { return '' }
    return ($line.Trim() -replace '\|', '\|')
}

$gate = @(Read-Trx (Join-Path $ResultsDirectory 'gate.trx'))
$known = @(Read-Trx (Join-Path $ResultsDirectory 'known-bugs.trx'))
$out = [System.Collections.Generic.List[string]]::new()

$passed = @($gate | Where-Object Outcome -eq 'Passed').Count
$failed = @($gate | Where-Object Outcome -eq 'Failed')
$other = $gate.Count - $passed - $failed.Count

$suffix = if ($Browser) { " ($Browser)" } else { '' }
$out.Add("## Gate$suffix")
$out.Add('')
$out.Add('| Passaram | Falharam | Não executados / inconclusivos |')
$out.Add('|---:|---:|---:|')
$out.Add("| $passed | $($failed.Count) | $other |")
if ($failed.Count -gt 0) {
    $out.Add('')
    $out.Add('| Teste que falhou | Mensagem |')
    $out.Add('|---|---|')
    foreach ($test in $failed) { $out.Add("| ``$($test.Name)`` | $(First-Line $test.Message) |") }
}

$out.Add('')
$out.Add("## Bugs conhecidos (devem falhar)$suffix")
$out.Add('')
if ($known.Count -eq 0) {
    $out.Add('Sem resultados.')
} else {
    $out.Add('| Teste | Estado |')
    $out.Add('|---|---|')
    foreach ($test in $known) {
        $state = switch ($test.Outcome) {
            'Failed' { 'Continua a falhar, como esperado' }
            'Passed' {
                Write-Output "::warning title=KnownBug passou$suffix::$($test.Name) passou. O bug pode ter sido corrigido: tira o teste da categoria KnownBug."
                '**Passou: o bug pode ter sido corrigido**'
            }
            default { 'Não executado (sem baseline ou inconclusivo)' }
        }
        $out.Add("| ``$($test.Name)`` | $state |")
    }
}

$out | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
$out
