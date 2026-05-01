using TelemetryRig.Core.Models;
using TelemetryRig.Core.Sdk;

namespace TelemetryRig.Core.Parsing;

/// <summary>
/// Converts raw binary telemetry into a clean C# object.
///
/// Performance note:
/// This parser uses ReadOnlySpan<byte> and BitConverter on slices.
/// That avoids creating temporary arrays for every field.
/// In real-time telemetry, avoiding tiny repeated allocations matters because
/// garbage collection pauses can cause visible UI stutter.
/// </summary>
public sealed class BinaryTelemetryParser : ITelemetryParser
{
    public const int ExpectedPayloadLength = 61;

    public bool TryParse(RawTelemetryFrame frame, out TelemetryPacket? packet, out string? error)
    {
        packet = null;
        error = null;

        if (frame.Payload.Length < ExpectedPayloadLength)
        {
            error = $"Payload too small. Expected {ExpectedPayloadLength} bytes, received {frame.Payload.Length}.";
            return false;
        }

        try
        {
            var span = frame.Payload.AsSpan();

            // In a real SDK, always verify the official byte order/endian format.
            var frameId = BitConverter.ToInt32(span.Slice(0, 4));
            var speed = BitConverter.ToDouble(span.Slice(4, 8));
            var rpm = BitConverter.ToInt32(span.Slice(12, 4));
            var gear = BitConverter.ToInt32(span.Slice(16, 4));
            var throttle = Clamp01(BitConverter.ToDouble(span.Slice(20, 8)));
            var brake = Clamp01(BitConverter.ToDouble(span.Slice(28, 8)));
            var steering = Math.Clamp(BitConverter.ToDouble(span.Slice(36, 8)), -1, 1);
            var suspension = BitConverter.ToDouble(span.Slice(44, 8));
            var wheelSlip = Math.Max(0, BitConverter.ToDouble(span.Slice(52, 8)));
            var surface = DecodeSurface((SurfaceCode)span[60]);

            packet = new TelemetryPacket(
                frame.TimestampUtc,
                frameId,
                frame.GameName,
                speed,
                rpm,
                gear,
                throttle,
                brake,
                steering,
                suspension,
                wheelSlip,
                surface,
                frame.Payload.Length);

            return true;
        }
        catch (Exception ex)
        {
            error = "Parser failed: " + ex.Message;
            return false;
        }
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);

    private static string DecodeSurface(SurfaceCode code) => code switch
    {
        SurfaceCode.Asphalt => "Asphalt",
        SurfaceCode.Gravel => "Gravel",
        SurfaceCode.Grass => "Grass",
        SurfaceCode.Kerb => "Kerb",
        _ => "Unknown"
    };
}
