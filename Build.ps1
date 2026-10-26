param([string]$GamePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference = 'Stop'
$managed = Join-Path $GamePath 'The Blood of Yidhra_Data\Managed'
$core = Join-Path $GamePath 'BepInEx\core'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$refs = @(
    (Join-Path $managed 'Assembly-CSharp.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $managed 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managed 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $core 'BepInEx.dll'),
    (Join-Path $core '0Harmony.dll'),
    (Join-Path $core 'Mono.Cecil.dll'),
    (Join-Path $core 'MonoMod.Utils.dll'),
    (Join-Path $core 'MonoMod.RuntimeDetour.dll')
)
foreach ($file in @($csc) + $refs) {
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing build dependency: $file" }
}
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$out = Join-Path $dist 'YidhraTrainer.dll'
$argsList = @('/nologo', '/target:library', '/optimize+', '/platform:anycpu', '/codepage:65001', "/out:$out")
$argsList += $refs | ForEach-Object { "/reference:$_" }
$argsList += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
& $csc @argsList
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }
Get-FileHash -LiteralPath $out -Algorithm SHA256 | Format-List



