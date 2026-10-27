using System.Diagnostics;
using RelayDesk.Models;

namespace RelayDesk.Services;

public static class FfmpegCommandBuilder
{
    public static void ValidateDestination(Destination destination)
    {
        if (string.IsNullOrWhiteSpace(destination.Name))
            throw new ArgumentException("Each destination needs a name.");
        if (!Uri.TryCreate(destination.Server, UriKind.Absolute, out var uri) ||
            !new[] { "rtmp", "rtmps" }.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException($"{destination.Name}: enter an RTMP/RTMPS server URL without a query, login or stream key.");
        if (string.IsNullOrWhiteSpace(destination.EncryptedStreamKey))
            throw new ArgumentException($"{destination.Name}: enter a stream key.");
        var spec = StreamProfiles.Get(destination.Profile);
        if (destination.Platform.Equals("LinkedIn", StringComparison.OrdinalIgnoreCase) &&
            (spec.Width > 1920 || spec.Height > 1080 || spec.Fps > 30 ||
             (destination.Profile != OutputProfile.CopySource && destination.PlannedVideoKbps > 6000)))
            throw new ArgumentException($"{destination.Name}: LinkedIn supports up to 1080p30 and 6000 kbps. Choose a smaller profile.");
        // Vertical platforms ingest portrait video only; a 4K landscape feed wastes tens of Mbps of upload.
        if ((destination.Platform.Equals("Instagram", StringComparison.OrdinalIgnoreCase) ||
             destination.Platform.Equals("TikTok", StringComparison.OrdinalIgnoreCase)) &&
            (spec.IsCopy || !spec.IsPortrait || spec.Fps > 30 || destination.PlannedVideoKbps > 6000))
            throw new ArgumentException($"{destination.Name}: {destination.Platform} needs a portrait profile (720 × 1280 or 1080 × 1920, 30 fps) at 6000 kbps or less.");
        if (destination.Profile != OutputProfile.CopySource &&
            (destination.PlannedVideoKbps < 300 || destination.PlannedVideoKbps > 60000))
            throw new ArgumentException($"{destination.Name}: bitrate must be between 300 and 60,000 kbps.");
    }

    // Construct commands with ArgumentList: no cmd.exe or shell interpolation.
    public static ProcessStartInfo Build(string ffmpegExe, string ingestUrl, string outputUrl,
        Destination destination, VideoEncoder encoder)
    {
        var info = new ProcessStartInfo(ffmpegExe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false
        };
        void Add(params string[] parts)
        {
            foreach (var part in parts) info.ArgumentList.Add(part);
        }
        Add("-hide_banner", "-nostdin", "-loglevel", "warning", "-stats_period", "2", "-progress", "pipe:2");
        Add("-i", ingestUrl, "-map", "0:v:0", "-map", "0:a:0?");
        var spec = StreamProfiles.Get(destination.Profile);
        if (spec.IsCopy)
        {
            Add("-c:v", "copy", "-c:a", "copy");
        }
        else
        {
            string filter;
            if (spec.IsPortrait && destination.VerticalFill == VerticalFillMode.CenterCrop)
            {
                filter = $"scale={spec.Width}:{spec.Height}:force_original_aspect_ratio=increase:force_divisible_by=2," +
                         $"crop={spec.Width}:{spec.Height},setsar=1";
            }
            else
            {
                filter = $"scale={spec.Width}:{spec.Height}:force_original_aspect_ratio=decrease:force_divisible_by=2," +
                         $"pad={spec.Width}:{spec.Height}:(ow-iw)/2:(oh-ih)/2,setsar=1";
            }
            Add("-vf", filter, "-r", spec.Fps.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (encoder == VideoEncoder.NvidiaNvenc)
                Add("-c:v", "h264_nvenc", "-preset", "p4", "-tune", "ll", "-rc", "cbr");
            else
                Add("-c:v", "libx264", "-preset", "veryfast", "-tune", "zerolatency", "-x264-params", "nal-hrd=cbr");

            // LinkedIn accepts H.264/AAC, recommends trying baseline for problem streams.
            Add("-profile:v", destination.Platform.Equals("LinkedIn", StringComparison.OrdinalIgnoreCase) ? "baseline" : "high");
            var rate = destination.PlannedVideoKbps.ToString(System.Globalization.CultureInfo.InvariantCulture) + "k";
            Add("-b:v", rate, "-minrate", rate, "-maxrate", rate,
                "-bufsize", (destination.PlannedVideoKbps * 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + "k",
                "-g", (spec.Fps * 2).ToString(System.Globalization.CultureInfo.InvariantCulture),
                "-bf", "0", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "128k", "-ar", "48000", "-ac", "2");
        }
        Add("-f", "flv", outputUrl);
        return info;
    }
}
