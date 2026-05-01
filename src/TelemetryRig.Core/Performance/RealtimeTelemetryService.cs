using System.Diagnostics;
using System.Threading.Channels;
using TelemetryRig.Core.Database;
using TelemetryRig.Core.Devices;
using TelemetryRig.Core.Models;
using TelemetryRig.Core.Parsing;
using TelemetryRig.Core.Sdk;

namespace TelemetryRig.Core.Performance;

/// <summary>
/// Real-time processing pipeline.
///
/// Beginner mental model:
/// 1. Producer receives raw telemetry frames from the game SDK.
/// 2. Producer puts frames into a queue.
/// 3. Consumer reads from the queue on a background thread.
/// 4. Consumer parses bytes, saves to SQLite in batches, and sends haptic commands.
/// 5. UI receives batches occasionally instead of every single frame.
///
/// This avoids blocking the WPF UI thread.
/// </summary>
public sealed class RealtimeTelemetryService : IAsyncDisposable
{
    private readonly IGameTelemetrySdk _sdk;
    private readonly ITelemetryParser _parser;
    private readonly ITelemetryRepository _repository;
    private readonly DeviceControlService _deviceControl;
    private readonly TelemetryPipelineOptions _options;

    private CancellationTokenSource? _cts;
    private Task? _producerTask;
    private Task? _consumerTask;
    private Channel<RawTelemetryFrame>? _channel;

    private long _framesReceived;
    private long _framesParsed;
    private long _framesSaved;
    private long _parserErrors;
    private double _totalParseMicroseconds;

    public event Action<IReadOnlyList<TelemetryPacket>, PipelineMetrics>? BatchReady;
    public event Action<string>? DiagnosticMessage;

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    public RealtimeTelemetryService(
        IGameTelemetrySdk sdk,
        ITelemetryParser parser,
        ITelemetryRepository repository,
        DeviceControlService deviceControl,
        TelemetryPipelineOptions? options = null)
    {
        _sdk = sdk;
        _parser = parser;
        _repository = repository;
        _deviceControl = deviceControl;
        _options = options ?? new TelemetryPipelineOptions();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsRunning)
            return;

        await _repository.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _deviceControl.ConnectAsync(cancellationToken).ConfigureAwait(false);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;

        _channel = Channel.CreateBounded<RawTelemetryFrame>(new BoundedChannelOptions(_options.QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = true,

            // Performance/backpressure policy:
            // If the app cannot keep up, keep the newest frames and drop old ones.
            // For real-time telemetry, latest data is usually more important than old data.
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _producerTask = Task.Run(() => ProduceAsync(_channel.Writer, token), token);
        _consumerTask = Task.Run(() => ConsumeAsync(_channel.Reader, token), token);

        DiagnosticMessage?.Invoke("Telemetry pipeline started.");
    }

    public async Task StopAsync()
    {
        if (_cts is null)
            return;

        _cts.Cancel();

        try
        {
            if (_producerTask is not null)
                await _producerTask.ConfigureAwait(false);
            if (_consumerTask is not null)
                await _consumerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user clicks Stop.
        }
        finally
        {
            await _deviceControl.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            _cts.Dispose();
            _cts = null;
            DiagnosticMessage?.Invoke("Telemetry pipeline stopped.");
        }
    }

    private async Task ProduceAsync(ChannelWriter<RawTelemetryFrame> writer, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var frame in _sdk.ReadFramesAsync(cancellationToken).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _framesReceived);

                // With DropOldest mode, this call will not allow memory to grow forever.
                await writer.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task ConsumeAsync(ChannelReader<RawTelemetryFrame> reader, CancellationToken cancellationToken)
    {
        var dbBatch = new List<TelemetryPacket>(_options.DatabaseBatchSize);
        var uiBatch = new List<TelemetryPacket>(_options.DatabaseBatchSize);
        var publishEvery = TimeSpan.FromMilliseconds(_options.UiPublishIntervalMilliseconds);
        var lastPublish = Stopwatch.StartNew();
        var stopwatch = new Stopwatch();

        await foreach (var frame in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            stopwatch.Restart();
            var ok = _parser.TryParse(frame, out var packet, out var error);
            stopwatch.Stop();

            if (!ok || packet is null)
            {
                Interlocked.Increment(ref _parserErrors);
                DiagnosticMessage?.Invoke(error ?? "Unknown parse error.");
                continue;
            }

            Interlocked.Increment(ref _framesParsed);
            InterlockedAddDouble(ref _totalParseMicroseconds, stopwatch.Elapsed.TotalMicroseconds);

            // Device I/O is not done on the UI thread.
            await _deviceControl.ApplyTelemetryAsync(packet, cancellationToken).ConfigureAwait(false);

            dbBatch.Add(packet);
            uiBatch.Add(packet);

            if (dbBatch.Count >= _options.DatabaseBatchSize)
            {
                await _repository.InsertBatchAsync(dbBatch, cancellationToken).ConfigureAwait(false);
                Interlocked.Add(ref _framesSaved, dbBatch.Count);
                dbBatch.Clear();
            }

            if (lastPublish.Elapsed >= publishEvery)
            {
                PublishUiBatch(uiBatch);
                uiBatch.Clear();
                lastPublish.Restart();
            }
        }

        if (dbBatch.Count > 0)
        {
            await _repository.InsertBatchAsync(dbBatch, cancellationToken).ConfigureAwait(false);
            Interlocked.Add(ref _framesSaved, dbBatch.Count);
        }

        PublishUiBatch(uiBatch);
    }

    private void PublishUiBatch(IReadOnlyList<TelemetryPacket> uiBatch)
    {
        if (uiBatch.Count == 0)
            return;

        BatchReady?.Invoke(uiBatch.ToArray(), CreateMetricsSnapshot());
    }

    private PipelineMetrics CreateMetricsSnapshot()
    {
        var parsed = Math.Max(1, Interlocked.Read(ref _framesParsed));
        var averageParse = _totalParseMicroseconds / parsed;

        return new PipelineMetrics(
            Interlocked.Read(ref _framesReceived),
            Interlocked.Read(ref _framesParsed),
            Interlocked.Read(ref _framesSaved),
            Interlocked.Read(ref _parserErrors),
            averageParse,
            DateTimeOffset.UtcNow);
    }

    private static void InterlockedAddDouble(ref double location, double value)
    {
        double newCurrentValue;
        double currentValue;
        do
        {
            currentValue = location;
            newCurrentValue = currentValue + value;
        }
        while (Math.Abs(Interlocked.CompareExchange(ref location, newCurrentValue, currentValue) - currentValue) > double.Epsilon);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
