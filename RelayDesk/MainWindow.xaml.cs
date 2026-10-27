using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using RelayDesk.Models;
using RelayDesk.Services;

namespace RelayDesk;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Destination> _targets = new();
    private readonly RelayController _relay = new();
    private bool _changingState;

    private VideoEncoder CurrentEncoder =>
        EncoderBox.SelectedItem is VideoEncoder encoder ? encoder : VideoEncoder.NvidiaNvenc;

    public MainWindow()
    {
        InitializeComponent();
        TargetsGrid.ItemsSource = _targets;
        EncoderBox.ItemsSource = Enum.GetValues<VideoEncoder>();
        _relay.Log += message => Ui(() => WriteLog(message));
        _relay.DestinationUpdated += (id, status, bitrate) => Ui(() =>
        {
            var target = _targets.FirstOrDefault(t => t.Id == id);
            if (target is not null)
            {
                target.Status = status;
                target.Bitrate = bitrate;
            }
            SetControls();
        });
        try
        {
            var settings = ConfigStore.Load();
            EncoderBox.SelectedItem = Enum.TryParse<VideoEncoder>(settings.Encoder, out var saved)
                ? saved : VideoEncoder.NvidiaNvenc;
            foreach (var target in settings.Destinations)
            {
                if (!target.Configured) target.Status = "Needs setup";
                AddTarget(target);
            }
            WriteLog("Destination settings loaded. Install ffmpeg.exe and mediamtx.exe in the tools folder.");
        }
        catch (Exception ex)
        {
            EncoderBox.SelectedItem = VideoEncoder.NvidiaNvenc;
            MessageBox.Show(this, "Could not read saved settings: " + ex.Message,
                "Configuration error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        SetControls();
    }

    private void AddTarget(Destination destination)
    {
        _targets.Add(destination);
        destination.PropertyChanged += Target_PropertyChanged;
    }

    private void Target_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Destination.Enabled)) RefreshCapacity();
    }

    private void Ui(Action callback)
    {
        if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            Dispatcher.BeginInvoke(callback);
    }

    private void WriteLog(string message)
    {
        if (LogBox is null) return;
        var now = DateTime.Now;
        var line = $"[{now:HH:mm:ss}] {message}{Environment.NewLine}";
        LogBox.AppendText(line);
        if (LogBox.Text.Length > 90000) LogBox.Text = LogBox.Text[^65000..];
        LogBox.ScrollToEnd();
        // Messages are already redacted of stream keys and destination URLs by RelayController.
        try
        {
            var dir = Path.Combine(ConfigStore.DirectoryPath, "logs");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, $"relaydesk-{now:yyyy-MM-dd}.log"), line);
        }
        catch (Exception) { /* Logging to disk is best-effort; the on-screen log still works. */ }
    }

    private void SaveConfiguration()
    {
        TargetsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        TargetsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        ConfigStore.Save(_targets, CurrentEncoder);
        WriteLog("Saved. Stream keys are encrypted for this Windows user using DPAPI.");
        RefreshCapacity();
    }

    private void RefreshCapacity()
    {
        if (EgressText is null) return;
        var enabled = _targets.Where(t => t.Enabled && t.Configured).ToList();
        var estimatedKbps = enabled.Where(d => d.Profile != OutputProfile.CopySource)
            .Sum(d => d.PlannedVideoKbps + 128);
        var copied = enabled.Count(d => d.Profile == OutputProfile.CopySource);
        var note = copied > 0 ? $" + {copied} copy-source outputs (unknown bitrate)" : "";
        EgressText.Text = enabled.Count == 0
            ? "No configured, enabled outputs. Add destinations and their authorized stream keys."
            : $"Planned upload: ~{estimatedKbps / 1000d:0.0} Mbps{note}; allow 25% or more headroom. Separate encoders use additional GPU/CPU.";
    }

    private void SetControls()
    {
        if (StartButton is null) return;
        var running = _relay.IsRunning;
        var selected = TargetsGrid.SelectedItem as Destination;
        StartButton.IsEnabled = !running && !_changingState;
        StopButton.IsEnabled = running && !_changingState;
        AddSixButton.IsEnabled = AddButton.IsEnabled = EditButton.IsEnabled =
            RemoveButton.IsEnabled = SaveButton.IsEnabled = !running && !_changingState;
        EncoderBox.IsEnabled = !running && !_changingState;
        StartSelectedButton.IsEnabled = !_changingState && selected is { Configured: true } &&
            !_relay.HasDestination(selected.Id);
        StopSelectedButton.IsEnabled = !_changingState && selected is not null &&
            _relay.HasDestination(selected.Id);
        TargetsGrid.IsReadOnly = running || _changingState;
        StateText.Text = running
            ? "Relay active • OBS must stream to 127.0.0.1:1935 • Confirm live status at each destination"
            : "Idle • Local RTMP input: 127.0.0.1:1935";
        RefreshCapacity();
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_changingState || _relay.IsRunning) return;
        _changingState = true;
        SetControls();
        try
        {
            SaveConfiguration();
            var enabled = _targets.Where(t => t.Enabled).ToList();
            await _relay.StartAsync(enabled, CurrentEncoder);
        }
        catch (Exception ex)
        {
            WriteLog("Startup error: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Relay could not start", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { _changingState = false; SetControls(); }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        if (_changingState || !_relay.IsRunning) return;
        _changingState = true;
        SetControls();
        try { await _relay.StopAsync(); }
        finally { _changingState = false; SetControls(); }
    }

    private async void StartSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_changingState || TargetsGrid.SelectedItem is not Destination selected) return;
        _changingState = true;
        SetControls();
        try
        {
            if (!_relay.IsRunning)
            {
                SaveConfiguration();
                await _relay.StartAsync([selected], CurrentEncoder);
            }
            else _relay.StartDestination(selected);
        }
        catch (Exception ex)
        {
            WriteLog("Cannot start output: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Could not start", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { _changingState = false; SetControls(); }
    }

    private async void StopSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_changingState || TargetsGrid.SelectedItem is not Destination selected) return;
        _changingState = true;
        SetControls();
        try { await _relay.StopDestinationAsync(selected.Id); }
        finally { _changingState = false; SetControls(); }
    }

    private void AddSix_Click(object sender, RoutedEventArgs e)
    {
        if (_relay.IsRunning) return;
        var added = 0;
        foreach (var preset in PlatformPresets.All.Where(p => p.Key != "Custom"))
        {
            if (_targets.Any(d => d.Platform == preset.Key)) continue;
            AddTarget(new Destination
            {
                Platform = preset.Key, Name = preset.Name,
                Profile = preset.DefaultProfile, VideoBitrateKbps = preset.BitrateKbps,
                Enabled = false, Status = "Needs setup"
            });
            added++;
        }
        SaveConfiguration();
        WriteLog($"Added {added} platform templates. Edit each to supply the provider's server/key, then check On.");
        SetControls();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DestinationDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is not null)
        {
            AddTarget(dialog.Result);
            SaveConfiguration();
            SetControls();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (TargetsGrid.SelectedItem is not Destination old) return;
        var dialog = new DestinationDialog(old) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is not null)
        {
            var index = _targets.IndexOf(old);
            old.PropertyChanged -= Target_PropertyChanged;
            _targets[index] = dialog.Result;
            dialog.Result.PropertyChanged += Target_PropertyChanged;
            SaveConfiguration();
            SetControls();
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (TargetsGrid.SelectedItem is not Destination target) return;
        if (MessageBox.Show(this, $"Remove {target.Name}?", "Confirm removal",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        target.PropertyChanged -= Target_PropertyChanged;
        _targets.Remove(target);
        SaveConfiguration();
        SetControls();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { SaveConfiguration(); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Encoder_Changed(object sender, SelectionChangedEventArgs e) => RefreshCapacity();
    private void TargetsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetControls();
    private void CopyServer_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(RelayController.ObsServer);
    private void CopySourceKey_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(RelayController.ObsStreamKey);
    private void Window_Closing(object sender, CancelEventArgs e) => _relay.KillImmediately();
}
