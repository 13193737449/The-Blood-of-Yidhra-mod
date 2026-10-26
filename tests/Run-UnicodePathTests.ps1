param([string]$GamePath = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$core=Join-Path $GamePath 'BepInEx\core'
$dist=Join-Path $root 'dist'
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $PSScriptRoot 'bin\UnicodePathTests.exe'
& $csc /nologo /target:exe /codepage:65001 "/out:$exe" "/reference:$dist\YidhraTrainer.dll" "/reference:$core\Mono.Cecil.dll" "/reference:$core\MonoMod.Utils.dll" (Join-Path $PSScriptRoot 'UnicodePathTests.cs')
if($LASTEXITCODE -ne 0){throw 'Unicode regression compilation failed.'}
& $exe $core $dist
if($LASTEXITCODE -ne 0){throw 'Unicode regression failed.'}
