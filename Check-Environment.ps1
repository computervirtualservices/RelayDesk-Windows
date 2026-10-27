# Run after building RelayDesk; no download or installation is performed.
$ErrorActionPreference = 'Continue'
$tools = @($PSScriptRoot + '\publish\tools', $env:LOCALAPPDATA + '\RelayDesk\tools')
Write-Host 'RelayDesk 2.0 environment check' -ForegroundColor Cyan
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    dotnet --version
} else {
    Write-Warning '.NET SDK missing: install .NET 10 SDK to build. Self-contained release needs no runtime.'
}
foreach ($tool in @('ffmpeg.exe', 'mediamtx.exe')) {
    $resolved = Get-Command $tool -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
    if (-not $resolved) {
        foreach ($dir in $tools) {
            $candidate = Join-Path $dir $tool
            if (Test-Path $candidate) { $resolved = $candidate; break }
        }
    }
    if ($resolved) {
        Write-Host "$tool found at $resolved" -ForegroundColor Green
        if ($tool -eq 'ffmpeg.exe') {
            $encoders = & $resolved -hide_banner -encoders 2>&1 | Out-String
            if ($encoders -match 'h264_nvenc') { Write-Host 'FFmpeg lists h264_nvenc' -ForegroundColor Green }
            else { Write-Warning 'This FFmpeg build does not list h264_nvenc. Use CpuX264 or another FFmpeg build.' }
        }
    } else {
        Write-Warning "$tool missing. Download from the official vendor links in README.md."
    }
}
if (Get-Command nvidia-smi -ErrorAction SilentlyContinue) {
    Write-Host "GPU: $(nvidia-smi --query-gpu=name --format=csv,noheader | Out-String)"
} else { Write-Warning 'nvidia-smi not found. NVENC requires an NVIDIA GPU and driver.' }
Write-Host 'Check valid platform stream keys and run an unlisted/private test before public broadcast.'
