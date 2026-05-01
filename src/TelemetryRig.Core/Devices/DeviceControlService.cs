using TelemetryRig.Core.Haptics;
using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Devices;

/// <summary>
/// Connects telemetry -> force feedback calculation -> device command.
///
/// The pipeline should call this service on a background thread.
/// The UI should not wait for device I/O.
/// </summary>
public sealed class DeviceControlService
{
    private readonly ForceFeedbackCalculator _calculator;
    private readonly IHapticDevice _device;

    public DeviceControlService(ForceFeedbackCalculator calculator, IHapticDevice device)
    {
        _calculator = calculator;
        _device = device;
    }

    public Task ConnectAsync(CancellationToken cancellationToken) => _device.ConnectAsync(cancellationToken);

    public async Task ApplyTelemetryAsync(TelemetryPacket packet, CancellationToken cancellationToken)
    {
        var command = _calculator.Calculate(packet);
        await _device.SendAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken) => _device.DisconnectAsync(cancellationToken);
}
