param(
    [string]$GamePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'The Blood of Yidhra'),
    [switch]$Uninstall
)
$ErrorActionPreference = 'Stop'
$GamePath = (Resolve-Path -LiteralPath $GamePath).Path
if (!(Test-Path -LiteralPath (Join-Path $GamePath 'The Blood of Yidhra.exe'))) { throw 'Selected folder is not The Blood of Yidhra.' }
if (!(Test-Path -LiteralPath (Join-Path $GamePath 'BepInEx\core\BepInEx.dll'))) { throw 'BepInEx is required in the game folder.' }
$source = Join-Path $PSScriptRoot 'dist\YidhraTrainer.dll'
$folder = Join-Path $GamePath 'BepInEx\plugins\YidhraTrainer'
$target = Join-Path $folder 'YidhraTrainer.dll'
if ($Uninstall) {
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target
        Write-Host '修改器插件已卸载；配置文件已保留。重启游戏后生效。'
    } else { Write-Host '未找到本修改器插件，无需卸载。' }
} else {
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw 'Missing dist\YidhraTrainer.dll. Run Build.ps1 first.' }
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Installed file verification failed.' }
    Write-Host '修改器已安装。请正常退出并重新启动游戏，按 F8 打开中文面板。'
    Write-Host 'F1-F4 资源 / F5 伤害倍率 / F6 无后座力 / F7 秒杀 / F9 不散射；金币在面板修改。'
}

