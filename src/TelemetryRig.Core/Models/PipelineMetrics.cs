namespace TelemetryRig.Core.Models;

/// <summary>
/// Real-time counters shown on the screen.
/// </summary>
public sealed record PipelineMetrics(
    long FramesReceived,
    long FramesParsed,
    long FramesSaved,
    long ParserErrors,
    double AverageParseMicroseconds,
    DateTimeOffset LastUpdatedUtc);
