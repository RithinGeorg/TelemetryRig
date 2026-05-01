namespace TelemetryRig.Core.Haptics;

/// <summary>
/// Command sent to a haptic or force-feedback device.
/// Values are normalized to 0..1 where possible, so any hardware implementation can map them.
/// </summary>
public sealed record HapticCommand(
    double LeftMotorIntensity,
    double RightMotorIntensity,
    double BrakePulseIntensity,
    double ForceFeedbackNewtonMeters,
    string Reason);
