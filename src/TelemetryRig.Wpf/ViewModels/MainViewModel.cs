using System.Collections.ObjectModel;
using System.Windows;
using TelemetryRig.Core.Api;
using TelemetryRig.Core.Database;
using TelemetryRig.Core.Devices;
using TelemetryRig.Core.Haptics;
using TelemetryRig.Core.Models;
using TelemetryRig.Core.Parsing;
using TelemetryRig.Core.Performance;
using TelemetryRig.Core.Sdk;
using TelemetryRig.Wpf.Services;
using System.IO;

namespace TelemetryRig.Wpf.ViewModels;

/// <summary>
/// Main screen ViewModel.
///
/// MVVM beginner idea:
/// - View: MainWindow.xaml. It only displays controls and bindings.
/// - ViewModel: this class. It holds UI state and commands.
/// - Model/Services: Core project classes that do parsing, database, device and API work.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private const int MaxRowsOnScreen = 500;

    private readonly WpfUiDispatcher _uiDispatcher;
    private readonly RealtimeTelemetryService _telemetryService;
    private readonly ForceFeedbackCalculator _feedbackCalculator = new();
    private readonly ITelemetryApiClient _apiClient = new FakeTelemetryApiClient();

    private long _framesReceived;
    private long _framesParsed;
    private long _framesSaved;
    private long _parserErrors;
    private double _averageParseMicroseconds;

    public MainViewModel()
    {
        _uiDispatcher = new WpfUiDispatcher(Application.Current.Dispatcher);

        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TelemetryRig",
            "telemetry.db");

        var sdk = new FakeGameTelemetrySdk(framesPerSecond: 60);
        var parser = new BinaryTelemetryParser();
        var repository = new SqliteTelemetryRepository(databasePath);
        var hapticDevice = new DebugHapticDevice();
        var deviceControl = new DeviceControlService(_feedbackCalculator, hapticDevice);

        _telemetryService = new RealtimeTelemetryService(
            sdk,
            parser,
            repository,
            deviceControl,
            new TelemetryPipelineOptions
            {
                QueueCapacity = 512,
                DatabaseBatchSize = 50,
                UiPublishIntervalMilliseconds = 100
            });

        _telemetryService.BatchReady += OnTelemetryBatchReady;
        _telemetryService.DiagnosticMessage += message => _ = AddLogAsync(message);

        StartCommand = new AsyncRelayCommand(StartAsync, () => !_telemetryService.IsRunning);
        StopCommand = new AsyncRelayCommand(StopAsync, () => _telemetryService.IsRunning);
        LoadApiProfileCommand = new AsyncRelayCommand(LoadApiProfileAsync);
        UploadLatestCommand = new AsyncRelayCommand(UploadLatestAsync, () => Rows.Count > 0);
        RowDoubleClickCommand = new RelayCommand<TelemetryRowViewModel>(OnRowDoubleClicked);

        Logs.Add("Ready. Click Start Telemetry to simulate SDK telemetry frames.");
        Logs.Add("Double-click a DataGrid row to see WPF routed event bubbling sent into an MVVM command.");
    }

    public ObservableCollection<TelemetryRowViewModel> Rows { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

    public AsyncRelayCommand StartCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand LoadApiProfileCommand { get; }
    public AsyncRelayCommand UploadLatestCommand { get; }
    public RelayCommand<TelemetryRowViewModel> RowDoubleClickCommand { get; }

    public long FramesReceived
    {
        get => _framesReceived;
        set => SetProperty(ref _framesReceived, value);
    }

    public long FramesParsed
    {
        get => _framesParsed;
        set => SetProperty(ref _framesParsed, value);
    }

    public long FramesSaved
    {
        get => _framesSaved;
        set => SetProperty(ref _framesSaved, value);
    }

    public long ParserErrors
    {
        get => _parserErrors;
        set => SetProperty(ref _parserErrors, value);
    }

    public double AverageParseMicroseconds
    {
        get => _averageParseMicroseconds;
        set => SetProperty(ref _averageParseMicroseconds, value);
    }

    private async Task StartAsync()
    {
        await _telemetryService.StartAsync(CancellationToken.None);
        await AddLogAsync("Started: producer/consumer tasks are running on background threads.");
        RefreshCommandStates();
    }

    private async Task StopAsync()
    {
        await _telemetryService.StopAsync();
        await AddLogAsync("Stopped: cancellation token requested and device disconnected.");
        RefreshCommandStates();
    }

    private async Task LoadApiProfileAsync()
    {
        var profile = await _apiClient.GetGameProfileAsync("SimRace Pro", CancellationToken.None);
        await AddLogAsync($"API profile loaded: {profile.GameName}, HapticsGain={profile.HapticsGain}, FFGain={profile.ForceFeedbackGain}. {profile.Notes}");
    }

    private async Task UploadLatestAsync()
    {
        var latestPackets = Rows.Take(20).Select(r => r.Packet).ToList();
        var result = await _apiClient.UploadTelemetryBatchAsync(latestPackets, CancellationToken.None);
        await AddLogAsync($"API upload result: {result.Message} Count={result.UploadedCount}");
    }

    private void OnTelemetryBatchReady(IReadOnlyList<TelemetryPacket> packets, PipelineMetrics metrics)
    {
        // This event is raised from the pipeline's background consumer task.
        // ObservableCollection must be updated on the WPF UI thread, so we use Dispatcher.
        _ = _uiDispatcher.InvokeAsync(() =>
        {
            foreach (var packet in packets)
            {
                var hapticCommand = _feedbackCalculator.Calculate(packet);
                Rows.Insert(0, new TelemetryRowViewModel(packet, hapticCommand));
            }

            while (Rows.Count > MaxRowsOnScreen)
                Rows.RemoveAt(Rows.Count - 1);

            FramesReceived = metrics.FramesReceived;
            FramesParsed = metrics.FramesParsed;
            FramesSaved = metrics.FramesSaved;
            ParserErrors = metrics.ParserErrors;
            AverageParseMicroseconds = metrics.AverageParseMicroseconds;
            UploadLatestCommand.RaiseCanExecuteChanged();
        });
    }

    private void OnRowDoubleClicked(TelemetryRowViewModel? row)
    {
        if (row is null)
            return;

        Logs.Insert(0, $"Row double-click bubbled to DataGrid: Frame {row.FrameId}, Speed {row.SpeedKph:F1} kph, Surface {row.Surface}.");

        while (Logs.Count > 100)
            Logs.RemoveAt(Logs.Count - 1);
    }

    private Task AddLogAsync(string message)
    {
        return _uiDispatcher.InvokeAsync(() =>
        {
            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");
            while (Logs.Count > 100)
                Logs.RemoveAt(Logs.Count - 1);
        });
    }

    private void RefreshCommandStates()
    {
        StartCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        UploadLatestCommand.RaiseCanExecuteChanged();
    }
}
