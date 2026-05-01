using TelemetryRig.Core.Interop;

namespace TelemetryRig.Tests;

[TestClass]
public sealed class NativeTelemetryMathTests
{
    [TestMethod]
    public void CalculateSlipRatio_WorksEvenWhenNativeDllIsMissing()
    {
        // On machines without the C++ DLL, the wrapper falls back to managed C# code.
        var ratio = NativeTelemetryMath.CalculateSlipRatio(12, 10);
        Assert.AreEqual(0.2, ratio, 0.0001);
    }

    [TestMethod]
    public void Clamp_WorksEvenWhenNativeDllIsMissing()
    {
        var result = NativeTelemetryMath.Clamp(2.0, 0.0, 1.0);
        Assert.AreEqual(1.0, result, 0.0001);
    }
}
