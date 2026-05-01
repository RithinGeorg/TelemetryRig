namespace TelemetryRig.Core.Models;

/// <summary>
/// A clean, parsed telemetry model used by the rest of the application.
/// Think of this as one row of useful game data.
///
/// The raw SDK might send bytes like this:
///   00 01 A4 88 23 00 ...
/// The parser converts those bytes into meaningful properties like SpeedKph and Rpm.
/// </summary>
public sealed record TelemetryPacket(
    DateTimeOffset TimestampUtc,
    int FrameId,
    string GameName,
    double SpeedKph,
    int Rpm,
    int Gear,
    double Throttle,
    double Brake,
    double Steering,
    double SuspensionTravelMm,
    double WheelSlip,
    string Surface,
    int RawBytesLength)
{
    /// <summary>
    /// Helpful calculated property: speed in metres per second.
    /// </summary>
    public double SpeedMetersPerSecond => SpeedKph / 3.6;
}
