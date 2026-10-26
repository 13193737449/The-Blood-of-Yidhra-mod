param([string]$GamePath = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$managed = Join-Path $GamePath 'The Blood of Yidhra_Data\Managed'
$core = Join-Path $GamePath 'BepInEx\core'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe = Join-Path $PSScriptRoot 'bin\PatchSmokeTests.exe'
$refs = @((Join-Path $managed 'Assembly-CSharp.dll'), (Join-Path $managed 'UnityEngine.dll'), (Join-Path $managed 'UnityEngine.CoreModule.dll'), (Join-Path $core '0Harmony.dll'), (Join-Path $root 'dist\YidhraTrainer.dll'))
$arguments = @('/nologo','/target:exe','/platform:anycpu','/codepage:65001',"/out:$exe")
$arguments += $refs | ForEach-Object { "/reference:$_" }
$arguments += Join-Path $PSScriptRoot 'PatchSmokeTests.cs'
& $csc @arguments
if ($LASTEXITCODE -ne 0) { throw 'Patch smoke test compilation failed.' }
# Bullet.Create contains Unity internal calls; registration must run under the game's Mono, not desktop CLR.
$hostExe = Join-Path $PSScriptRoot 'bin\MonoHost.exe'
& $csc /nologo /target:exe /platform:x64 "/out:$hostExe" (Join-Path $PSScriptRoot 'MonoHost.cs')
if ($LASTEXITCODE -ne 0) { throw 'Mono host compilation failed.' }
$output = & $hostExe $GamePath $exe $GamePath $root 2>&1
$result = $LASTEXITCODE
$output | Tee-Object -FilePath (Join-Path $PSScriptRoot 'patch-smoke-result.txt')
if ($result -ne 0) { throw "Patch smoke test failed (exit $result)." }

