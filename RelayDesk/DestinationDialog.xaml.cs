using System.Windows;
using System.Windows.Controls;
using RelayDesk.Models;
using RelayDesk.Services;

namespace RelayDesk;

public partial class DestinationDialog : Window
{
    private readonly Destination? _existing;
    private bool _initializing = true;
    public Destination? Result { get; private set; }

    public DestinationDialog(Destination? existing = null, PlatformPreset? preset = null)
    {
        InitializeComponent();
        _existing = existing;
        PlatformBox.ItemsSource = PlatformPresets.All;
        ProfileBox.ItemsSource = StreamProfiles.All;
        VerticalBox.ItemsSource = Enum.GetValues<VerticalFillMode>();

        var chosenPlatform = existing is not null ? PlatformPresets.For(existing.Platform) :
            preset ?? PlatformPresets.All[^1];
        PlatformBox.SelectedItem = chosenPlatform;
        var chosenProfile = existing?.Profile ?? chosenPlatform.DefaultProfile;
        ProfileBox.SelectedItem = StreamProfiles.All.First(p => p.Value == chosenProfile);
        VerticalBox.SelectedItem = existing?.VerticalFill ?? VerticalFillMode.Letterbox;
        NameBox.Text = existing?.Name ?? (chosenPlatform.Key == "Custom" ? "" : chosenPlatform.Name);
        ServerBox.Text = existing?.Server ?? "";
        BitrateBox.Text = (existing?.VideoBitrateKbps > 0 ? existing.VideoBitrateKbps :
            chosenPlatform.BitrateKbps > 0 && chosenProfile == chosenPlatform.DefaultProfile ?
                chosenPlatform.BitrateKbps : StreamProfiles.Get(chosenProfile).VideoKbps).ToString();
        if (existing is not null)
        {
            Title = "Edit Streaming Destination";
            try { if (!string.IsNullOrWhiteSpace(existing.EncryptedStreamKey))
                KeyBox.Password = SecretStore.Unprotect(existing.EncryptedStreamKey); }
            catch
            {
                MessageBox.Show(this, "Stream key cannot be decrypted for this Windows account. Re-enter it.",
                    "Key unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        _initializing = false;
        UpdateHelp();
    }

    private void Platform_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || PlatformBox.SelectedItem is not PlatformPreset preset) return;
        NameBox.Text = preset.Key == "Custom" ? NameBox.Text : preset.Name;
        ProfileBox.SelectedItem = StreamProfiles.All.First(p => p.Value == preset.DefaultProfile);
        BitrateBox.Text = preset.BitrateKbps.ToString();
        UpdateHelp();
    }

    private void Profile_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || ProfileBox.SelectedItem is not ProfileChoice profile) return;
        BitrateBox.Text = StreamProfiles.Get(profile.Value).VideoKbps.ToString();
        UpdateHelp();
    }

    private void UpdateHelp()
    {
        if (HelpText is null) return;
        var platform = PlatformBox.SelectedItem as PlatformPreset;
        var profile = ProfileBox.SelectedItem as ProfileChoice;
        HelpText.Text = (platform?.Note ?? "") + "\n" +
            (profile?.Value == OutputProfile.CopySource
                ? "Copy Source: OBS must already send H.264/AAC at a quality the destination accepts."
                : "Conversion: H.264/AAC at the specified size/bitrate. NVIDIA NVENC is selected in the main window.");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var server = ServerBox.Text.Trim().TrimEnd('/');
        var key = KeyBox.Password.Trim();
        if (PlatformBox.SelectedItem is not PlatformPreset platform ||
            ProfileBox.SelectedItem is not ProfileChoice profile ||
            VerticalBox.SelectedItem is not VerticalFillMode vertical ||
            string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show(this, "Choose a platform and profile, then enter a name, server and stream key.",
                "Incomplete destination", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!int.TryParse(BitrateBox.Text, out var bitrate))
        {
            MessageBox.Show(this, "Enter a bitrate in kbps, e.g. 6000.", "Check bitrate",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            Result = new Destination
            {
                Id = _existing?.Id ?? Guid.NewGuid(),
                Platform = platform.Key,
                Name = name,
                Server = server,
                EncryptedStreamKey = SecretStore.Protect(key),
                Profile = profile.Value,
                VerticalFill = vertical,
                VideoBitrateKbps = profile.Value == OutputProfile.CopySource ? 0 : bitrate,
                Enabled = _existing?.Enabled ?? true
            };
            FfmpegCommandBuilder.ValidateDestination(Result);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Cannot save destination", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
