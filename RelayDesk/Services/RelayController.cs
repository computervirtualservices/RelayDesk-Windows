using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using RelayDesk.Models;

namespace RelayDesk.Services;

/// <summary>Runs one loopback RTMP ingest and independently restartable FFmpeg outputs.</summary>
public sealed class RelayController
{
    public const string ObsServer = "rtmp://127.0.0.1:1935/live";
    public const string ObsStreamKey = "studio";
    private const string InputUrl = ObsServer + "/" + ObsStreamKey;

    private sealed class Run(CancellationTokenSource cancellation)
    {
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public Task Worker { get; set; } = Task.CompletedTask;
        public Process? Process { get; set; }
    }

    private readonly ConcurrentDictionary<Guid, Run> _sessions = new();
    private CancellationTokenSource? _serverCancellation;
    private Process? _mediaServer;
    private string? _ffmpeg;
    private VideoEncoder _encoder;

    public bool IsRunning => _serverCancellation is { IsCancellationRequested: false };
    public event Action<string>? Log;
    public event Action<Guid, string, string>? DestinationUpdated;

    public async Task StartAsync(IReadOnlyList<Destination> destinations, VideoEncoder encoder)
    {
        if (IsRunning) throw new InvalidOperationException("Relay is already active. Use Start Selected to add a stream.");
        if (destinations.Count == 0) throw new InvalidOperationException("Enable and configure at least one destination.");
        foreach (var destination in destinations) FfmpegCommandBuilder.ValidateDestination(destination);

        _ffmpeg = ToolLocator.Find("ffmpeg.exe")
            ?? throw new FileNotFoundException("FFmpeg not found. Add ffmpeg.exe in the tools folder or PATH.");
        var mediamtx = ToolLocator.Find("mediamtx.exe")
            ?? throw new FileNotFoundException("MediaMTX not found. Add mediamtx.exe in the tools folder or PATH.");
        if (!PortIsAvailable())
            throw new InvalidOperationException("TCP 127.0.0.1:1935 is already in use. Close the other RTMP server.");

        _encoder = encoder;
        _serverCancellation = new CancellationTokenSource();
        try
        {
            Directory.CreateDirectory(ConfigStore.DirectoryPath);
            var configPath = Path.Combine(ConfigStore.DirectoryPath, "mediamtx.yml");
            // Latest MediaMTX uses these values; bind RTMP to loopback only.
            await File.WriteAllTextAsync(configPath, """
                logLevel: warn
                rtsp: false
                rtmp: true
                rtmpAddress: 127.0.0.1:1935
                hls: false
                webrtc: false
                srt: false
                moq: false
                api: false
                metrics: false
                pprof: false
                playback: false
                paths:
                  all_others:
                    source: publisher
                """);

            var start = new ProcessStartInfo(mediamtx)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = ConfigStore.DirectoryPath
            };
            start.ArgumentList.Add(configPath);
            _mediaServer = new Process { StartInfo = start };
            if (!_mediaServer.Start()) throw new InvalidOperationException("Could not start MediaMTX.");
            _ = DrainMediaOutputAsync(_mediaServer.StandardOutput);
            _ = DrainMediaOutputAsync(_mediaServer.StandardError);
            await WaitForMediaServerAsync(_mediaServer, _serverCancellation.Token);
            Log?.Invoke("Local RTMP ingest ready. Start streaming in OBS to rtmp://127.0.0.1:1935/live, key: studio.");
            foreach (var destination in destinations) StartDestination(destination);
        }
        catch
        {
            await StopAsync();
            throw;
        }
    }

    public void StartDestination(Destination destination)
    {
        if (!IsRunning || _serverCancellation is null || _ffmpeg is null)
            throw new InvalidOperationException("First start the relay server with Start All.");
        FfmpegCommandBuilder.ValidateDestination(destination);
        if (_sessions.ContainsKey(destination.Id))
            throw new InvalidOperationException($"{destination.Name} is already started.");
        var linked = CancellationTokenSource.CreateLinkedTokenSource(_serverCancellation.Token);
        var session = new Run(linked);
        if (!_sessions.TryAdd(destination.Id, session))
        {
            linked.Dispose();
            throw new InvalidOperationException($"{destination.Name} is already started.");
        }
        var ffmpeg = _ffmpeg!;
        // The worker remains independent: a failed destination must not stop the others.
        session.Worker = Task.Run(() => ForwardWithRetryAsync(destination, ffmpeg, session), linked.Token);
        Log?.Invoke($"[{destination.Name}] Output worker started: {destination.ProfileLabel}.");
    }

    public bool HasDestination(Guid id) => _sessions.ContainsKey(id);

    public async Task StopDestinationAsync(Guid id)
    {
        if (!_sessions.TryRemove(id, out var session)) return;
        session.Cancellation.Cancel();
        if (session.Process is not null) SafeKill(session.Process);
        try { await session.Worker.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception) { /* Already killed; a stuck process must not hang the interface. */ }
        session.Cancellation.Dispose();
        DestinationUpdated?.Invoke(id, "Stopped", "—");
    }

    public async Task StopAsync()
    {
        var cancel = _serverCancellation;
        _serverCancellation = null;
        cancel?.Cancel();
        var ids = _sessions.Keys.ToList();
        await Task.WhenAll(ids.Select(StopDestinationAsync));
        if (_mediaServer is not null)
        {
            SafeKill(_mediaServer);
            _mediaServer.Dispose();
            _mediaServer = null;
        }
        cancel?.Dispose();
        Log?.Invoke("Relay stopped.");
    }

    public void KillImmediately()
    {
        _serverCancellation?.Cancel();
        foreach (var run in _sessions.Values)
        {
            run.Cancellation.Cancel();
            if (run.Process is not null) SafeKill(run.Process);
        }
        if (_mediaServer is not null) SafeKill(_mediaServer);
    }

    private async Task ForwardWithRetryAsync(Destination destination, string ffmpeg, Run session)
    {
        var token = session.Cancellation.Token;
        string key;
        try { key = SecretStore.Unprotect(destination.EncryptedStreamKey); }
        catch
        {
            DestinationUpdated?.Invoke(destination.Id, "Key unavailable", "—");
            Log?.Invoke($"[{destination.Name}] Cannot decrypt stream key for this Windows account.");
            return;
        }
        var output = destination.Server.TrimEnd('/') + "/" + key;
        var reconnectDelays = new[] { 2, 4, 8, 15, 30, 60 };
        int attempts = 0;
        while (!token.IsCancellationRequested)
        {
            Process? process = null;
            try
            {
                DestinationUpdated?.Invoke(destination.Id, "Connecting / waiting for OBS", "—");
                var start = FfmpegCommandBuilder.Build(ffmpeg, InputUrl, output, destination, _encoder);
                process = new Process { StartInfo = start };
                session.Process = process;
                if (!process.Start()) throw new InvalidOperationException("Cannot launch FFmpeg.");
                using var registration = token.Register(() => SafeKill(process));
                string currentBitrate = "—";
                bool hasProgress = false;
                string? line;
                while ((line = await process.StandardError.ReadLineAsync(token)) is not null)
                {
                    if (line.StartsWith("bitrate=", StringComparison.Ordinal))
                    {
                        currentBitrate = line["bitrate=".Length..].Trim();
                    }
                    else if (line.StartsWith("progress=continue", StringComparison.Ordinal))
                    {
                        if (!hasProgress)
                        {
                            hasProgress = true;
                            Log?.Invoke($"[{destination.Name}] Sending video. Confirm actual live status on the destination platform.");
                        }
                        DestinationUpdated?.Invoke(destination.Id, "Sending", currentBitrate);
                        attempts = 0;
                    }
                    else if (line.StartsWith("progress=end", StringComparison.Ordinal))
                    {
                        DestinationUpdated?.Invoke(destination.Id, "Input ended", "—");
                    }
                    else if (line.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                             line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                             line.Contains("Unknown encoder", StringComparison.OrdinalIgnoreCase))
                    {
                        Log?.Invoke($"[{destination.Name}] {Redact(line, key, output, destination.Server)}");
                    }
                }
                await process.WaitForExitAsync(token);
                if (!token.IsCancellationRequested)
                    Log?.Invoke($"[{destination.Name}] FFmpeg exited ({process.ExitCode}); will retry.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                Log?.Invoke($"[{destination.Name}] {Redact(ex.Message, key, output, destination.Server)}");
            }
            finally
            {
                session.Process = null;
                if (process is not null) { SafeKill(process); process.Dispose(); }
            }

            if (token.IsCancellationRequested) break;
            var delay = reconnectDelays[Math.Min(attempts++, reconnectDelays.Length - 1)];
            DestinationUpdated?.Invoke(destination.Id, $"Retry in {delay}s", "—");
            try { await Task.Delay(TimeSpan.FromSeconds(delay), token); }
            catch (OperationCanceledException) { break; }
        }
        DestinationUpdated?.Invoke(destination.Id, "Stopped", "—");
    }

    private static string Redact(string text, string key, string url, string server)
    {
        text = text.Replace(url, "[destination URL redacted]", StringComparison.OrdinalIgnoreCase)
            .Replace(server, "[server redacted]", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(key))
            text = text.Replace(key, "[key redacted]", StringComparison.Ordinal);
        return text;
    }

    private async Task DrainMediaOutputAsync(StreamReader reader)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                if (line.Contains("ERR", StringComparison.OrdinalIgnoreCase))
                    Log?.Invoke("[MediaMTX] " + line);
            }
        }
        catch (Exception) { /* Process stopped. */ }
    }

    private static bool PortIsAvailable()
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, 1935);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException) { return false; }
    }

    private static async Task WaitForMediaServerAsync(Process server, CancellationToken token)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (server.HasExited)
                throw new InvalidOperationException($"MediaMTX exited on startup (code {server.ExitCode}). Check version and mediamtx.yml.");
            using var tcp = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(150));
            try
            {
                await tcp.ConnectAsync(IPAddress.Loopback, 1935, timeout.Token);
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception) { /* RTMP server has not started yet. */ }
            await Task.Delay(150, token);
        }
        throw new TimeoutException("MediaMTX did not open local RTMP port 1935.");
    }

    private static void SafeKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (Exception) { /* Best-effort cancellation. */ }
    }
}
