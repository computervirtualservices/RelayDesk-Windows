# Run with PowerShell on Windows 11 with the .NET 10 SDK installed.
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet restore .\RelayDesk\RelayDesk.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
    dotnet build .\RelayDesk\RelayDesk.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    dotnet publish .\RelayDesk\RelayDesk.csproj -c Release -r win-x64 `
      --self-contained true -p:PublishSingleFile=true `
      -p:EnableCompressionInSingleFile=true -o .\publish
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    New-Item -ItemType Directory -Force -Path .\publish\tools | Out-Null
    Copy-Item .\RelayDesk\tools\README.txt .\publish\tools\README.txt -Force
    Write-Host 'Built: publish\RelayDesk.exe' -ForegroundColor Green
    Write-Host 'Add ffmpeg.exe and mediamtx.exe under publish\tools before running.'
} finally { Pop-Location }
