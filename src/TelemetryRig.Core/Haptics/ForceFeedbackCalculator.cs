using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Haptics;

/// <summary>
/// Converts telemetry into force feedback / haptic intensities.
///
/// This class has no UI and no hardware dependency, so it is easy to unit test.
/// </summary>
public sealed class ForceFeedbackCalculator
{
    public HapticCommand Calculate(TelemetryPacket packet)
    {
        // More speed means the steering wheel should feel heavier.
        var speedFactor = Math.Clamp(packet.SpeedKph / 220.0, 0.1, 1.0);

        // Steering force pushes against the user's steering direction.
        // Example: user steers right, wheel gives a little force left.
        var steeringForce = -packet.Steering * speedFactor * 6.0;

        // Wheel slip creates rumble. Gravel also adds rumble.
        var slipRumble = Math.Clamp(packet.WheelSlip * 2.0, 0, 1);
        var surfaceRumble = packet.Surface == "Gravel" ? 0.35 : packet.Surface == "Kerb" ? 0.5 : 0.0;
        var rumble = Math.Clamp(slipRumble + surfaceRumble, 0, 1);

        // Brake pulse is useful when the player is braking hard or locking wheels.
        var brakePulse = Math.Clamp(packet.Brake * packet.WheelSlip * 2.5, 0, 1);

        // Slightly bias rumble left/right based on steering.
        var left = Math.Clamp(rumble + Math.Max(0, packet.Steering) * 0.2, 0, 1);
        var right = Math.Clamp(rumble + Math.Max(0, -packet.Steering) * 0.2, 0, 1);

        var reason = rumble > 0.7
            ? "High wheel slip or rough surface"
            : brakePulse > 0.3
                ? "Brake vibration"
                : "Normal road feel";

        return new HapticCommand(left, right, brakePulse, steeringForce, reason);
    }
}
