param([string]$GamePath = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$hostExe=Join-Path $PSScriptRoot 'bin\MonoHost.exe'
& $csc /nologo /target:exe /platform:x64 "/out:$hostExe" (Join-Path $PSScriptRoot 'MonoHost.cs')
if($LASTEXITCODE -ne 0){throw 'Mono host compilation failed.'}
foreach($test in @('UnicodePathTests','PatchSmokeTests','CombatTests','GoldTests','BehaviorTests')) {
    $testExe=Join-Path $PSScriptRoot "bin\$test.exe"
    if($test -eq 'UnicodePathTests'){$testArgs=@((Join-Path $GamePath 'BepInEx\core'),(Join-Path $root 'dist'));$log='mono-provider-regression.txt'}
    elseif($test -eq 'PatchSmokeTests'){$testArgs=@($GamePath,$root);$log='mono-smoke-result.txt'}
    else {$testArgs=@((Join-Path $GamePath 'The Blood of Yidhra_Data\Managed'));$log=('mono-'+$test+'-result.txt')}
    $output=& $hostExe $GamePath $testExe @testArgs 2>&1
    $code=$LASTEXITCODE
    $output | Tee-Object -FilePath (Join-Path $PSScriptRoot $log)
    if($code -ne 0){throw "$test failed under actual Unity Mono (exit $code)."}
}

