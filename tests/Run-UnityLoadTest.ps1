param([string]$GamePath = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'The Blood of Yidhra'))
$ErrorActionPreference='Stop'
if(Get-Process -Name 'The Blood of Yidhra' -ErrorAction SilentlyContinue){throw 'Game is already running; diagnostic test will not interrupt it.'}
$exe=Join-Path $GamePath 'The Blood of Yidhra.exe'
$engineLog=Join-Path $PSScriptRoot 'unity-diagnostic.log'
$pluginLog=Join-Path $GamePath 'BepInEx\LogOutput.log'
$captured=Join-Path $PSScriptRoot 'unity-load-result.log'
$diagnostic=Start-Process -FilePath $exe -WorkingDirectory $GamePath -ArgumentList @('-batchmode','-nographics','-logFile',('"'+$engineLog+'"')) -WindowStyle Hidden -PassThru
$loaded=$false
try {
    for($attempt=0;$attempt -lt 35;$attempt++){
        Start-Sleep -Seconds 1
        if(Test-Path -LiteralPath $pluginLog){
            $content=Get-Content -LiteralPath $pluginLog -Raw
            if($content -match 'Loading \[Yidhra Trainer 1\.1\.0\]'){
                if($content -match 'Yidhra Trainer ready v1\.1\.0; 17 patches installed'){$loaded=$true;break}
                if($content -match 'Yidhra Trainer failed'){break}
            }
        }
        if($diagnostic.HasExited){break}
    }
    if(Test-Path -LiteralPath $pluginLog){Copy-Item -LiteralPath $pluginLog -Destination $captured -Force}
    if(!$loaded){throw "Unity load test did not confirm success. Read $captured and $engineLog"}
    Write-Output 'PASS: actual Unity game started, loaded Yidhra Trainer 1.1.0 and registered all 17 patches.'
} finally {
    if(!$diagnostic.HasExited){Stop-Process -Id $diagnostic.Id -ErrorAction SilentlyContinue}
}

