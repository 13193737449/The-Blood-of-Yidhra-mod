param([string]$GamePath = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$managed = Join-Path $GamePath 'The Blood of Yidhra_Data\Managed'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$log = Join-Path $PSScriptRoot 'gold-last-result.txt'
$sources = @((Join-Path $PSScriptRoot 'GoldTests.cs'), (Join-Path $root 'src\GoldOperations.cs'))
$refs = @('Assembly-CSharp.dll', 'UnityEngine.dll', 'UnityEngine.CoreModule.dll') | ForEach-Object { Join-Path $managed $_ }
$exe = Join-Path $bin 'GoldTests.exe'
$arguments = @('/nologo', '/target:exe', '/platform:anycpu', '/codepage:65001', "/out:$exe")
$arguments += $refs | ForEach-Object { "/reference:$_" }
$arguments += $sources
$build = & $csc @arguments 2>&1
$build | Set-Content -LiteralPath $log -Encoding UTF8
$build | Write-Output
if ($LASTEXITCODE -ne 0) { throw "Gold tests compilation failed. See $log" }
$output = & $exe $managed 2>&1
$result = $LASTEXITCODE
$output | Add-Content -LiteralPath $log -Encoding UTF8
$output | Write-Output
if ($result -ne 0) { throw "Gold tests failed (exit $result). See $log" }
