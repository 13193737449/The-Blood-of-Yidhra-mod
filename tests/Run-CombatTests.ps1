param([string]$GamePath=(Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$managed=Join-Path $GamePath 'The Blood of Yidhra_Data\Managed'
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $PSScriptRoot 'bin\CombatTests.exe'
& $csc /nologo /target:exe /codepage:65001 "/out:$exe" "/reference:$managed\Assembly-CSharp.dll" "/reference:$managed\UnityEngine.dll" "/reference:$managed\UnityEngine.CoreModule.dll" (Join-Path $PSScriptRoot 'CombatTests.cs') (Join-Path $root 'src\TrainerState.cs') (Join-Path $root 'src\CombatPatches.cs')
if($LASTEXITCODE -ne 0){throw 'Combat tests compilation failed'}
$output=& $exe $managed 2>&1
$code=$LASTEXITCODE
$output|Tee-Object -FilePath (Join-Path $PSScriptRoot 'combat-last-result.txt')
if($code -ne 0){throw 'Combat tests failed'}
