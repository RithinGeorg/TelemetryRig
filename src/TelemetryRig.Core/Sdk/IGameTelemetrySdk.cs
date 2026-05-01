using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Sdk;

/// <summary>
/// Abstraction for a game SDK.
///
/// Real world examples:
/// - iRacing SDK
/// - Assetto Corsa shared memory
/// - UDP telemetry from F1 games
/// - A proprietary Next Level Racing simulator SDK
///
/// The WPF app does not care which one you use. It only consumes raw frames.
/// </summary>
public interface IGameTelemetrySdk
{
    IAsyncEnumerable<RawTelemetryFrame> ReadFramesAsync(CancellationToken cancellationToken);
}
