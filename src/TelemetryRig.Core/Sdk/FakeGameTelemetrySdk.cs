using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Sdk;

/// <summary>
/// Fake telemetry SDK used for local learning and  demos.
///
/// It simulates a game producing telemetry at a fixed frequency, for example 60 frames per second.
/// In a real project this class would be replaced by an SDK-specific implementation.
/// </summary>
public sealed class FakeGameTelemetrySdk : IGameTelemetrySdk
{
    private readonly int _framesPerSecond;
    private readonly Random _random = new(42);

    public FakeGameTelemetrySdk(int framesPerSecond = 60)
    {
        if (framesPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        _framesPerSecond = framesPerSecond;
    }

    public async IAsyncEnumerable<RawTelemetryFrame> ReadFramesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(1000.0 / _framesPerSecond);
        var frameId = 0;
        var timer = new PeriodicTimer(delay);

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            frameId++;

            // Generate believable values.
            var speed = 80 + 40 * Math.Sin(frameId / 40.0) + _random.NextDouble() * 8;
            var rpm = 2500 + (int)(2200 * Math.Abs(Math.Sin(frameId / 33.0)));
            var gear = Math.Clamp((int)(speed / 35) + 1, 1, 6);
            var throttle = Math.Clamp(0.55 + Math.Sin(frameId / 25.0) * 0.45, 0, 1);
            var brake = frameId % 95 > 80 ? 0.65 : 0.0;
            var steering = Math.Sin(frameId / 20.0);
            var suspension = 30 + _random.NextDouble() * 15;
            var wheelSlip = Math.Max(0, Math.Sin(frameId / 14.0) * 0.4 + _random.NextDouble() * 0.12);
            var surface = frameId % 180 > 145 ? SurfaceCode.Gravel : SurfaceCode.Asphalt;

            yield return CreateFrame(
                frameId,
                speed,
                rpm,
                gear,
                throttle,
                brake,
                steering,
                suspension,
                wheelSlip,
                surface);
        }
    }

    /// <summary>
    /// Public helper used by unit tests to create predictable frames.
    ///
    /// Binary layout, little-endian:
    /// 0-3    int32  frame id
    /// 4-11   double speed kph
    /// 12-15  int32  rpm
    /// 16-19  int32  gear
    /// 20-27  double throttle 0..1
    /// 28-35  double brake    0..1
    /// 36-43  double steering -1..1
    /// 44-51  double suspension travel mm
    /// 52-59  double wheel slip
    /// 60     byte   surface code
    /// </summary>
    public static RawTelemetryFrame CreateFrame(
        int frameId,
        double speedKph,
        int rpm,
        int gear,
        double throttle,
        double brake,
        double steering,
        double suspensionTravelMm,
        double wheelSlip,
        SurfaceCode surface)
    {
        var payload = new byte[61];
        var span = payload.AsSpan();

        BitConverter.TryWriteBytes(span.Slice(0, 4), frameId);
        BitConverter.TryWriteBytes(span.Slice(4, 8), speedKph);
        BitConverter.TryWriteBytes(span.Slice(12, 4), rpm);
        BitConverter.TryWriteBytes(span.Slice(16, 4), gear);
        BitConverter.TryWriteBytes(span.Slice(20, 8), throttle);
        BitConverter.TryWriteBytes(span.Slice(28, 8), brake);
        BitConverter.TryWriteBytes(span.Slice(36, 8), steering);
        BitConverter.TryWriteBytes(span.Slice(44, 8), suspensionTravelMm);
        BitConverter.TryWriteBytes(span.Slice(52, 8), wheelSlip);
        span[60] = (byte)surface;

        return new RawTelemetryFrame(DateTimeOffset.UtcNow, "SimRace Pro", payload);
    }
}

public enum SurfaceCode : byte
{
    Asphalt = 0,
    Gravel = 1,
    Grass = 2,
    Kerb = 3
}
