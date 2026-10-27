# RelayDesk Windows — Open-Source OBS Multistreaming and RTMP Relay

**Windows 11 • OBS Studio • Six streaming destination templates • C# / WPF / .NET 10**

> **Status: Early development.** This is the uncompiled/unverified source-code prototype; not a signed or production-tested Windows installer.

RelayDesk is a standalone Windows alternative workflow to OBS Multi-RTMP: send one OBS feed locally, then manage independent RTMP/RTMPS outputs for **YouTube, Rumble, Facebook, LinkedIn, TikTok and Instagram**. It supports custom destinations, 4K and portrait presets, and an optional NVIDIA H.264 NVENC output path.

**Repository website:** Published after GitHub Pages deployment at `https://computervirtualservices.github.io/RelayDesk-Windows/` if using the default repository name and account. This URL is not live until deployment succeeds.

**Public launch resources:** [GitHub publishing steps](docs/PUBLISHING.md) · [SEO / AEO / GEO / WebMCP plan](docs/SEO-AEO-GEO-WEBMCP.md) · [Security](SECURITY.md) · [Contributing](CONTRIBUTING.md) · [MIT License](LICENSE)

---


**Source-code upgrade. Not compiled, live-tested, or signed.**

Standalone **Windows 11** application using **C# / WPF / .NET 10**, intended to be built in Visual Studio 2026. It receives **one OBS RTMP stream** on localhost through MediaMTX, then starts independently managed FFmpeg forwarding processes for YouTube, Rumble, Facebook, TikTok, Instagram and LinkedIn. No OBS plugin is required.

## Features delivered in this source archive

- Six ready-to-configure destination templates via **Add Six**, plus Custom RTMP/RTMPS.
- Individual destination **add/edit/remove**, enable selection, **start/stop selected**, and **start/stop all**.
- Independent output presets: **4K60, 4K30, 1080p60, 1080p30, 720p30, portrait 1080p30, portrait 720p30, or copy source**.
- NVIDIA **H.264 NVENC** encoding with a software **libx264** fallback; H.264 + AAC in FLV/RTMP.
- Portrait **letterbox** (preserve entire 16:9 source) and **center crop** (fill portrait, crops sides). For professional TikTok/Instagram video, create a dedicated portrait OBS composition instead.
- Adjustable per-output video bitrate, 2-second keyframes, estimated aggregate outbound bandwidth, platform tips, status logging and independent auto-retry.
- Windows DPAPI encryption for stream keys. RTMP listener bound to `127.0.0.1`.
- Backward-compatible reading of the first RelayDesk `settings.json` (legacy destinations retain **Copy Source** when no profile is specified).

**What “Sending” means:** FFmpeg reports sending frames; it is **not proof** that YouTube/Facebook/etc. started public broadcasting. You must verify ingest/live status on each provider.

## Build requirements

- Windows 11 x64 + .NET 10 SDK, or Visual Studio 2026 with the .NET desktop development workload.
- FFmpeg Windows executable from an authorized reputable build with **`h264_nvenc` enabled**, or select `CpuX264` for software encoding.
- MediaMTX Windows `mediamtx.exe` (current stable version).
- OBS Studio, H.264/AAC live output, and a GPU/CPU capable of encoding the selected profiles.
- Outbound internet upload capacity exceeding **the sum** of all your outgoing streams by at least 25% plus operational margin; GPU performance and video quality require real measurement.
- Authorized live permissions + valid RTMP/RTMPS server URLs and current stream keys from each site.

Official download links:

- .NET: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- OBS Studio: https://obsproject.com/
- FFmpeg: https://ffmpeg.org/download.html
- MediaMTX: https://github.com/bluenviron/mediamtx/releases

**This ZIP contains NO third-party binaries.** Always download MediaMTX and FFmpeg from verified sources. Stream software itself is free; bandwidth and accounts can have costs/requirements.

### Compile

From PowerShell, in the extracted project root:

```powershell
.\Build-Windows.ps1
```

This should produce `publish\RelayDesk.exe` as a self-contained Windows-x64 executable. It also creates `publish\tools\`.

Copy the real `ffmpeg.exe` and `mediamtx.exe` to `publish\tools\`, or make them available on `PATH`, or under `%LOCALAPPDATA%\RelayDesk\tools\`. These tools are not bundled. Then run:

```powershell
.\Check-Environment.ps1
.\publish\RelayDesk.exe
```

For IDE development, open `RelayDesk\RelayDesk.csproj` in Visual Studio. The `tools` directory needs to be beside the running executable (or tools on PATH).

**Caution:** WPF Windows targets cannot be functionally exercised in this Linux creation environment. The code and XAML were statically inspected only; **a Windows build and end-to-end streaming test remain mandatory**.

## OBS setup

1. Open RelayDesk and click **Add Six**. Six rows appear disabled and marked **Needs setup**.
2. Select each destination, click **Edit**, paste its **RTMP/RTMPS server** and **stream key** from your current live dashboard. Choose/adjust profile and bitrate. TikTok/Instagram use portrait presets. Save each one and check its **On** checkbox.
3. Choose `NvidiaNvenc` (default) or `CpuX264` from the global encoder box.
4. Click **START ALL** in RelayDesk. This starts the local MediaMTX server and one FFmpeg worker per enabled configured destination. Workers retry while they wait for OBS input.
5. In OBS → **Settings → Stream**, select **Custom...**. Enter:
   - Server: `rtmp://127.0.0.1:1935/live`
   - Stream key: `studio`
6. In OBS, set H.264/AAC streaming and your desired source scene. Example: 3840×2160 30 fps; remember that output profiles cannot create frames or detail absent from the source.
7. Click **Start Streaming** in OBS. Observe RelayDesk status, then individually verify each provider receives your stream. Some providers also require clicking **Go Live** in their own interface.
8. You can select one destination and use **■ Selected** to stop it without interrupting the others; then **▶ Selected** to restart it. **STOP ALL** stops all workers and MediaMTX. Stop OBS streaming separately.

OBS sends to the local application, not directly to a destination. RelayDesk must stay running. It does not currently control OBS's Start Streaming button.

## Platform profiles: starting values, not promises

| Platform | Starter resolution / FPS | Video bitrate | Notes |
|---|---|---:|---|
| YouTube | 4K 3840×2160 / 30 | 42 Mbps (H.264) | Select 4K60 at 50 Mbps if source, upload and encoder support it. [YouTube encoder guide](https://support.google.com/youtube/answer/2853702). |
| Rumble | 1080p / 30 | 6 Mbps | Select a custom 4K profile *only after* confirming that Rumble's specific live endpoint permits 4K. The application cannot establish live 4K entitlement. |
| Facebook | 1080p / 30 | 6 Mbps | H.264/AAC and 2-second keyframe; actual limits/access can vary. |
| LinkedIn | 1080p / 30 | 3.5 Mbps | Maximum 1080p30 / 6 Mbps video, 128 kbps audio. [LinkedIn encoder settings](https://www.linkedin.com/help/linkedin/answer/a567498). |
| TikTok | Portrait 720×1280 / 30 | 3.5 Mbps | Streaming key and third-party encoder access are account/region dependent. |
| Instagram | Portrait 720×1280 / 30 | 3.5 Mbps | Live Producer may require a newly issued key per broadcast and a web-side Go Live confirmation. |

### Estimated upload and GPU workload

With all six starter profiles enabled, the target video bitrates alone total **64.5 Mbps** (42+6+6+3.5+3.5+3.5), plus audio and protocol overhead. A rough 25% margin puts required *sustained* uplink capacity around **82 Mbps**. Do an extended test on the actual internet connection.

Each destination currently has its own FFmpeg process and **re-encodes independently** (except `Copy Source`). Sharing a single NVENC encode across destinations that use identical settings is a future optimization, not currently present. Your NVIDIA driver and hardware may limit concurrent NVENC sessions; NVENC does not automatically mean GPU scaling or decoding. Resolution changes in this version use FFmpeg's CPU `scale`/`pad` filters before GPU encoding. Test thermals, dropped frames and CPU/GPU utilization at 4K.

### Copy source

`Copy Source` performs no re-encoding and is lightweight, but a destination must accept exactly the codec, resolution, frame rate and bitrate from OBS. Use **H.264 video + AAC audio** for the most common RTMP/FLV destinations. There is no guarantee that a copied 4K feed is accepted by every platform.

## Security

- Outbound stream keys are protected with **Windows user-scoped DPAPI** before they are saved to `%LOCALAPPDATA%\RelayDesk\settings.json`. Keys must be re-entered when moving data to another account or machine.
- Do not put tokens in the **Server** field, which is stored as plain text. Server URLs with credentials/query parameters are deliberately rejected in this version. Put the supplied token/key into the **Stream Key** field when your platform's RTMP endpoint format permits it.
- FFmpeg needs the complete outbound RTMP/RTMPS publish URL as a command-line argument. Its command line can be inspected by users/processes with sufficient local privileges. DPAPI does **not** prevent that exposure when streaming is active.
- Application errors redact configured server URLs and keys on a best-effort basis. Do not post diagnostic logs or process lists without reviewing them.
- Prefer RTMPS when provided by the streaming platform.
- The RTMP ingest server listens on loopback, **not** the public network; do not expose it to the internet without authentication and TLS.

## Not included yet

- Verified successful live streams on any of the six platform accounts (requires actual user account keys and end-to-end rehearsal).
- OAuth sign-in, automatic permission/eligibility verification, API creation of live events, or automated platform Go Live clicks.
- Separate portrait **OBS scenes** or sophisticated graphic-aware reframing: current options crop or letterbox a horizontal camera/OBS canvas.
- Hardware-accelerated GPU scaling, cross-destination shared encodes, full per-platform analytics or service-side success confirmations.
- Prerecorded video scheduling, persistent recording, OBS WebSocket control, a signed MSI/MSIX installer or auto-updates.

## Architecture

```text
OBS Studio (source H.264/AAC)
    |  rtmp://127.0.0.1:1935/live  [key=studio]
    v
MediaMTX (localhost:1935, single published source)
    |    |    |    |    |    |
    +-- FFmpeg #1 4K           -> YouTube
    +-- FFmpeg #2 1080p        -> Rumble
    +-- FFmpeg #3 1080p        -> Facebook
    +-- FFmpeg #4 portrait     -> TikTok
    +-- FFmpeg #5 portrait     -> Instagram
    +-- FFmpeg #6 1080p        -> LinkedIn
```

- `RelayDesk/Models/StreamProfiles.cs`: codecs/resolution presets and platform hints.
- `RelayDesk/Services/FfmpegCommandBuilder.cs`: argument-safe, per-destination FFmpeg command generation.
- `RelayDesk/Services/RelayController.cs`: one local MediaMTX + independently cancellable FFmpeg workers and reconnect retries.
- `RelayDesk/Services/SecretStore.cs`: Windows DPAPI key protection.
- `RelayDesk/Services/ConfigStore.cs`: application settings persisted atomically.
- `RelayDesk/MainWindow.xaml` / `.cs`: dashboard and stream controls.
- `RelayDesk/DestinationDialog.xaml` / `.cs`: per-destination configuration and key encryption.

## Origin and licensing

This source is an independent application inspired by the multistreaming use case of https://github.com/sorayuki/obs-multi-rtmp; it does not incorporate that plugin's source. Check the original plugin's GPL and all FFmpeg/MediaMTX distribution licensing if you later combine or redistribute binaries/code. The generated source code in this archive is supplied to you for further development.
