using TelemetryRig.Core.Parsing;
using TelemetryRig.Core.Sdk;

namespace TelemetryRig.Tests;

[TestClass]
public sealed class BinaryTelemetryParserTests
{
    [TestMethod]
    public void TryParse_ValidFrame_ReturnsTelemetryPacket()
    {
        var frame = FakeGameTelemetrySdk.CreateFrame(
            frameId: 10,
            speedKph: 120.5,
            rpm: 4500,
            gear: 4,
            throttle: 0.8,
            brake: 0.1,
            steering: -0.25,
            suspensionTravelMm: 32,
            wheelSlip: 0.2,
            surface: SurfaceCode.Asphalt);

        var parser = new BinaryTelemetryParser();

        var ok = parser.TryParse(frame, out var packet, out var error);

        Assert.IsTrue(ok, error);
        Assert.IsNotNull(packet);
        Assert.AreEqual(10, packet.FrameId);
        Assert.AreEqual(120.5, packet.SpeedKph, 0.001);
        Assert.AreEqual(4500, packet.Rpm);
        Assert.AreEqual("Asphalt", packet.Surface);
    }

    [TestMethod]
    public void TryParse_ShortPayload_ReturnsFalse()
    {
        var parser = new BinaryTelemetryParser();
        var frame = new TelemetryRig.Core.Models.RawTelemetryFrame(DateTimeOffset.UtcNow, "BrokenGame", new byte[5]);

        var ok = parser.TryParse(frame, out var packet, out var error);

        Assert.IsFalse(ok);
        Assert.IsNull(packet);
        Assert.IsTrue(error!.Contains("Payload too small"));
    }
}
