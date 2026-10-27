using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace RelayDesk.Models;

public sealed class Destination : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Platform { get; set; } = "Custom";
    public string Name { get; set; } = "";
    public string Server { get; set; } = "";
    // User-scoped Windows DPAPI ciphertext; do not place secrets in Server.
    public string EncryptedStreamKey { get; set; } = "";
    public OutputProfile Profile { get; set; } = OutputProfile.CopySource;
    public VerticalFillMode VerticalFill { get; set; } = VerticalFillMode.Letterbox;
    public int VideoBitrateKbps { get; set; }

    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; OnPropertyChanged(); }
    }

    private string _status = "Idle";
    [JsonIgnore]
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private string _bitrate = "—";
    [JsonIgnore]
    public string Bitrate
    {
        get => _bitrate;
        set { _bitrate = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string ProfileLabel => StreamProfiles.Get(Profile).Label;

    [JsonIgnore]
    public bool Configured => !string.IsNullOrWhiteSpace(Server) &&
                              !string.IsNullOrWhiteSpace(EncryptedStreamKey);

    [JsonIgnore]
    public int PlannedVideoKbps => Profile == OutputProfile.CopySource ? 0 :
        VideoBitrateKbps > 0 ? VideoBitrateKbps : StreamProfiles.Get(Profile).VideoKbps;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? property = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}

public sealed class AppSettings
{
    public string Encoder { get; set; } = nameof(VideoEncoder.NvidiaNvenc);
    public List<Destination> Destinations { get; set; } = new();
}
