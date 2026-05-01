namespace TelemetryRig.Core.Performance;

public sealed class TelemetryPipelineOptions
{
    /// <summary>
    /// Bounded queue prevents unlimited memory growth if the producer is faster than the consumer.
    /// </summary>
    public int QueueCapacity { get; init; } = 512;

    /// <summary>
    /// Database writes are grouped into batches for speed.
    /// </summary>
    public int DatabaseBatchSize { get; init; } = 50;

    /// <summary>
    /// UI is updated at a lower rate than telemetry input.
    /// Example: receive 60 FPS, update UI 10 times per second.
    /// </summary>
    public int UiPublishIntervalMilliseconds { get; init; } = 100;
}
