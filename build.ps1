$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$csc  = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$refs = "/r:System.dll", "/r:System.Drawing.dll", "/r:System.Windows.Forms.dll"
$src  = Get-ChildItem -Filter *.cs | ForEach-Object { $_.Name }

Write-Host "Kompiliere TaskBarSpeedmon ..."
& $csc /nologo /target:winexe /optimize+ /codepage:65001 /win32manifest:app.manifest /out:TaskBarSpeedmon.exe $refs $src

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build erfolgreich: TaskBarSpeedmon.exe" -ForegroundColor Green
} else {
    Write-Host "Build fehlgeschlagen." -ForegroundColor Red
    exit $LASTEXITCODE
}
