namespace TelemetryRig.Core.Models;

/// <summary>
/// Represents one raw frame received from a game SDK or hardware device.
/// It is intentionally still "raw" because the parser has not interpreted the bytes yet.
/// </summary>
public sealed record RawTelemetryFrame(
    DateTimeOffset TimestampUtc,
    string GameName,
    byte[] Payload);
