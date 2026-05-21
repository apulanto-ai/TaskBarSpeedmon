$csc  = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$refs = "/r:System.Windows.Forms.dll", "/r:System.Drawing.dll", "/r:System.dll"
$src  = "Program.cs", "NetworkMonitor.cs", "AppSettings.cs", "SettingsForm.cs", "TrayApp.cs"

Write-Host "Compiling TaskBarSpeedmon..."
& $csc /target:winexe /out:TaskBarSpeedmon.exe $refs $src

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build erfolgreich: TaskBarSpeedmon.exe" -ForegroundColor Green
} else {
    Write-Host "Build fehlgeschlagen." -ForegroundColor Red
}
