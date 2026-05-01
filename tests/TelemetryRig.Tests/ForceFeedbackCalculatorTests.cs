using TelemetryRig.Core.Haptics;
using TelemetryRig.Core.Models;

namespace TelemetryRig.Tests;

[TestClass]
public sealed class ForceFeedbackCalculatorTests
{
    [TestMethod]
    public void Calculate_HighSlip_ProducesRumble()
    {
        var packet = new TelemetryPacket(
            DateTimeOffset.UtcNow,
            FrameId: 1,
            GameName: "Test",
            SpeedKph: 150,
            Rpm: 5000,
            Gear: 5,
            Throttle: 0.9,
            Brake: 0.0,
            Steering: 0.3,
            SuspensionTravelMm: 35,
            WheelSlip: 0.6,
            Surface: "Asphalt",
            RawBytesLength: 61);

        var calculator = new ForceFeedbackCalculator();
        var command = calculator.Calculate(packet);

        Assert.IsTrue(command.LeftMotorIntensity > 0.5);
        Assert.IsTrue(command.RightMotorIntensity > 0.5);
        Assert.AreEqual("High wheel slip or rough surface", command.Reason);
    }

    [TestMethod]
    public void Calculate_BrakingWithSlip_ProducesBrakePulse()
    {
        var packet = new TelemetryPacket(
            DateTimeOffset.UtcNow,
            FrameId: 1,
            GameName: "Test",
            SpeedKph: 100,
            Rpm: 3500,
            Gear: 3,
            Throttle: 0.1,
            Brake: 0.8,
            Steering: 0.0,
            SuspensionTravelMm: 35,
            WheelSlip: 0.3,
            Surface: "Asphalt",
            RawBytesLength: 61);

        var calculator = new ForceFeedbackCalculator();
        var command = calculator.Calculate(packet);

        Assert.IsTrue(command.BrakePulseIntensity > 0.3);
    }
}
