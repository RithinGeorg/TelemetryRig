// TelemetryRig.Native.cpp
// Optional native C++ DLL for demonstrating C/C++ interop with .NET P/Invoke.
//
// Build with CMake or Visual Studio, then copy TelemetryRig.Native.dll next to the WPF .exe.

#include <algorithm>

extern "C"
{
    __declspec(dllexport) double CalculateSlipRatioNative(double wheelSpeedMetersPerSecond, double vehicleSpeedMetersPerSecond)
    {
        if (vehicleSpeedMetersPerSecond <= 0.1)
        {
            return 0.0;
        }

        return (wheelSpeedMetersPerSecond - vehicleSpeedMetersPerSecond) / vehicleSpeedMetersPerSecond;
    }

    __declspec(dllexport) double ClampNative(double value, double min, double max)
    {
        return std::max(min, std::min(value, max));
    }
}
