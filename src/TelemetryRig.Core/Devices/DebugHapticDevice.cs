using TelemetryRig.Core.Haptics;

namespace TelemetryRig.Core.Devices;

/// <summary>
/// Safe fake device for learning.
/// It does not move real hardware; it only stores the latest command.
/// </summary>
public sealed class DebugHapticDevice : IHapticDevice
{
    public bool IsConnected { get; private set; }
    public HapticCommand? LastCommand { get; private set; }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task SendAsync(HapticCommand command, CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Device is not connected.");

        LastCommand = command;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }
}
