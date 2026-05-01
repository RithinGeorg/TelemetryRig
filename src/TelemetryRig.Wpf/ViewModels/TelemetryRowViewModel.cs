using TelemetryRig.Core.Haptics;
using TelemetryRig.Core.Models;

namespace TelemetryRig.Wpf.ViewModels;

/// <summary>
/// Row-specific ViewModel used by the DataGrid.
/// It formats raw model values for display.
/// </summary>
public sealed class TelemetryRowViewModel
{
    public TelemetryRowViewModel(TelemetryPacket packet, HapticCommand hapticCommand)
    {
        Packet = packet;
        HapticCommand = hapticCommand;
    }

    public TelemetryPacket Packet { get; }
    public HapticCommand HapticCommand { get; }

    public int FrameId => Packet.FrameId;
    public string LocalTime => Packet.TimestampUtc.ToLocalTime().ToString("HH:mm:ss.fff");
    public double SpeedKph => Packet.SpeedKph;
    public int Rpm => Packet.Rpm;
    public int Gear => Packet.Gear;
    public double ThrottlePercent => Packet.Throttle * 100;
    public double BrakePercent => Packet.Brake * 100;
    public double Steering => Packet.Steering;
    public double WheelSlip => Packet.WheelSlip;
    public string Surface => Packet.Surface;
    public string HapticHint => HapticCommand.Reason;
}
