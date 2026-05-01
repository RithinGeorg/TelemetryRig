using TelemetryRig.Core.Haptics;

namespace TelemetryRig.Core.Devices;

/// <summary>
/// Hardware abstraction for haptics/force-feedback.
///
/// You can implement this interface using:
/// - USB HID calls
/// - DirectInput
/// - Vendor SDK DLL
/// - Serial port commands
/// - A network-connected motion platform
/// </summary>
public interface IHapticDevice
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task SendAsync(HapticCommand command, CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}
