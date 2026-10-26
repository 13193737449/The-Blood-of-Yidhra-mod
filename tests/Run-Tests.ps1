param([string]$GamePath = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$managed = Join-Path $GamePath 'The Blood of Yidhra_Data\Managed'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$log = Join-Path $PSScriptRoot 'last-result.txt'
$sources = @((Join-Path $PSScriptRoot 'BehaviorTests.cs'), (Join-Path $root 'src\TrainerState.cs'), (Join-Path $root 'src\TrainerPatches.cs'))
$references = @('Assembly-CSharp.dll', 'UnityEngine.dll', 'UnityEngine.CoreModule.dll') | ForEach-Object { Join-Path $managed $_ }
$exe = Join-Path $bin 'BehaviorTests.exe'
$arguments = @('/nologo', '/target:exe', '/platform:anycpu', '/codepage:65001', "/out:$exe")
$arguments += $references | ForEach-Object { "/reference:$_" }
$arguments += $sources
$build = & $csc @arguments 2>&1
$build | Set-Content -LiteralPath $log -Encoding UTF8
$build | Write-Output
if ($LASTEXITCODE -ne 0) { throw "Behavior tests compilation failed. See $log" }
$output = & $exe $managed 2>&1
$result = $LASTEXITCODE
$output | Add-Content -LiteralPath $log -Encoding UTF8
$output | Write-Output
if ($result -ne 0) { throw "Behavior tests failed (exit $result). See $log" }
