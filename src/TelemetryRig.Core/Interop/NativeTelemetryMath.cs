using System.Runtime.InteropServices;

namespace TelemetryRig.Core.Interop;

/// <summary>
/// Demonstrates C/C++ interop in .NET using P/Invoke.
///
/// The app can run even if the native DLL is missing because this wrapper falls back to managed C# code.
/// That is useful for demos and unit tests.
/// </summary>
public static class NativeTelemetryMath
{
    private const string NativeLibraryName = "TelemetryRig.Native";

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double CalculateSlipRatioNative(double wheelSpeedMetersPerSecond, double vehicleSpeedMetersPerSecond);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double ClampNative(double value, double min, double max);

    public static double CalculateSlipRatio(double wheelSpeedMetersPerSecond, double vehicleSpeedMetersPerSecond)
    {
        try
        {
            return CalculateSlipRatioNative(wheelSpeedMetersPerSecond, vehicleSpeedMetersPerSecond);
        }
        catch (DllNotFoundException)
        {
            return CalculateSlipRatioManaged(wheelSpeedMetersPerSecond, vehicleSpeedMetersPerSecond);
        }
        catch (EntryPointNotFoundException)
        {
            return CalculateSlipRatioManaged(wheelSpeedMetersPerSecond, vehicleSpeedMetersPerSecond);
        }
        catch (BadImageFormatException)
        {
            return CalculateSlipRatioManaged(wheelSpeedMetersPerSecond, vehicleSpeedMetersPerSecond);
        }
    }

    public static double Clamp(double value, double min, double max)
    {
        try
        {
            return ClampNative(value, min, max);
        }
        catch (DllNotFoundException)
        {
            return Math.Clamp(value, min, max);
        }
        catch (EntryPointNotFoundException)
        {
            return Math.Clamp(value, min, max);
        }
        catch (BadImageFormatException)
        {
            return Math.Clamp(value, min, max);
        }
    }

    private static double CalculateSlipRatioManaged(double wheelSpeedMetersPerSecond, double vehicleSpeedMetersPerSecond)
    {
        if (vehicleSpeedMetersPerSecond <= 0.1)
            return 0;

        return (wheelSpeedMetersPerSecond - vehicleSpeedMetersPerSecond) / vehicleSpeedMetersPerSecond;
    }
}
