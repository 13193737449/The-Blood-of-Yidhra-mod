$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $PSScriptRoot ('bin\InstallFixture-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $fixture 'BepInEx\core'), (Join-Path $fixture 'BepInEx\plugins'), (Join-Path $fixture 'BepInEx\config') -Force | Out-Null
New-Item -ItemType File -Path (Join-Path $fixture 'The Blood of Yidhra.exe'), (Join-Path $fixture 'BepInEx\core\BepInEx.dll') | Out-Null
$existing = Join-Path $fixture 'BepInEx\plugins\ExistingPlugin.txt'
$config = Join-Path $fixture 'BepInEx\config\local.yidhra.trainer.cfg'
'keep existing plugin' | Set-Content -LiteralPath $existing
'keep config' | Set-Content -LiteralPath $config
$oldHash = (Get-FileHash -LiteralPath $existing).Hash
$configHash = (Get-FileHash -LiteralPath $config).Hash
$installed = Join-Path $fixture 'BepInEx\plugins\YidhraTrainer\YidhraTrainer.dll'
& (Join-Path $root 'Install-Trainer.ps1') -GamePath $fixture
if (!(Test-Path -LiteralPath $installed)) { throw 'Plugin was not installed.' }
if ((Get-FileHash -LiteralPath $installed).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root 'dist\YidhraTrainer.dll')).Hash) { throw 'Installed hash mismatch.' }
& (Join-Path $root 'Install-Trainer.ps1') -GamePath $fixture -Uninstall
if (Test-Path -LiteralPath $installed) { throw 'Plugin was not uninstalled.' }
if ((Get-FileHash -LiteralPath $existing).Hash -ne $oldHash) { throw 'Existing plugin changed.' }
if ((Get-FileHash -LiteralPath $config).Hash -ne $configHash) { throw 'Config changed.' }
& (Join-Path $root 'Install-Trainer.ps1') -GamePath $fixture -Uninstall
'PASS: install hash, uninstall, existing files retained, repeat uninstall' | Tee-Object -FilePath (Join-Path $PSScriptRoot 'install-result.txt')
