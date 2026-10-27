namespace RelayDesk.Models;

// Presets are conservative ingest starting points, not platform guarantees.
public enum OutputProfile
{
    CopySource,
    Landscape2160p60,
    Landscape2160p30,
    Landscape1080p60,
    Landscape1080p30,
    Landscape720p30,
    Portrait1080p30,
    Portrait720p30
}

public enum VerticalFillMode { Letterbox, CenterCrop }
public enum VideoEncoder { NvidiaNvenc, CpuX264 }

public sealed record VideoSpec(string Label, int Width, int Height, int Fps, int VideoKbps)
{
    public bool IsCopy => Width == 0;
    public bool IsPortrait => Height > Width;
}

public sealed record ProfileChoice(OutputProfile Value, string Label)
{
    public override string ToString() => Label;
}

public static class StreamProfiles
{
    public static readonly IReadOnlyList<ProfileChoice> All =
    [
        new(OutputProfile.CopySource, "Copy OBS source — no re-encoding"),
        new(OutputProfile.Landscape2160p60, "4K • 3840 × 2160 • 60 fps"),
        new(OutputProfile.Landscape2160p30, "4K • 3840 × 2160 • 30 fps"),
        new(OutputProfile.Landscape1080p60, "1080p • 1920 × 1080 • 60 fps"),
        new(OutputProfile.Landscape1080p30, "1080p • 1920 × 1080 • 30 fps"),
        new(OutputProfile.Landscape720p30, "720p • 1280 × 720 • 30 fps"),
        new(OutputProfile.Portrait1080p30, "Portrait • 1080 × 1920 • 30 fps"),
        new(OutputProfile.Portrait720p30, "Portrait • 720 × 1280 • 30 fps")
    ];

    public static VideoSpec Get(OutputProfile profile) => profile switch
    {
        OutputProfile.Landscape2160p60 => new("4K 60", 3840, 2160, 60, 50000),
        OutputProfile.Landscape2160p30 => new("4K 30", 3840, 2160, 30, 42000),
        OutputProfile.Landscape1080p60 => new("1080p 60", 1920, 1080, 60, 9000),
        OutputProfile.Landscape1080p30 => new("1080p 30", 1920, 1080, 30, 6000),
        OutputProfile.Landscape720p30 => new("720p 30", 1280, 720, 30, 3500),
        OutputProfile.Portrait1080p30 => new("Portrait 1080", 1080, 1920, 30, 4500),
        OutputProfile.Portrait720p30 => new("Portrait 720", 720, 1280, 30, 3500),
        _ => new("Copy OBS", 0, 0, 0, 0)
    };
}

public sealed record PlatformPreset(
    string Key, string Name, OutputProfile DefaultProfile, int BitrateKbps,
    string Note)
{
    public override string ToString() => Name;
}

public static class PlatformPresets
{
    public static readonly IReadOnlyList<PlatformPreset> All =
    [
        new("YouTube", "YouTube", OutputProfile.Landscape2160p30, 42000,
            "YouTube supports 4K/30 and 4K/60. 4K/60 H.264 calls for up to 50 Mbps video. Set up a live broadcast and copy its RTMPS ingest URL and key."),
        new("Rumble", "Rumble", OutputProfile.Landscape1080p30, 6000,
            "Use the ingest URL/key provided by Rumble. 4K is selectable as a custom profile only after you verify that your Rumble event accepts it."),
        new("Facebook", "Facebook", OutputProfile.Landscape1080p30, 6000,
            "Start with H.264/AAC, 1080p and 2-second keyframes; live access, endpoints and limits can vary by account."),
        new("TikTok", "TikTok", OutputProfile.Portrait720p30, 3500,
            "Third-party RTMP access depends on your account/region. Use the exact authorized ingest URL and key; a stream key is not available to all accounts."),
        new("Instagram", "Instagram", OutputProfile.Portrait720p30, 3500,
            "Instagram Live Producer generally needs eligible access and a newly issued key for each live event. Complete the Go Live confirmation on Instagram."),
        new("LinkedIn", "LinkedIn", OutputProfile.Landscape1080p30, 3500,
            "LinkedIn requires eligible Live access. Maximum 1080p at 30 fps, 6 Mbps video; create an event/live stream and use its current RTMP/RTMPS information."),
        new("Custom", "Custom RTMP/RTMPS", OutputProfile.CopySource, 0,
            "Provide an authorized RTMP/RTMPS destination. Copy Source requires OBS H.264 video and AAC audio for broad compatibility.")
    ];

    public static PlatformPreset For(string? key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)) ?? All[^1];
}
