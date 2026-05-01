using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Parsing;

/// <summary>
/// Parser interface. The parser is kept separate so it can be unit tested without the WPF UI.
/// </summary>
public interface ITelemetryParser
{
    bool TryParse(RawTelemetryFrame frame, out TelemetryPacket? packet, out string? error);
}
