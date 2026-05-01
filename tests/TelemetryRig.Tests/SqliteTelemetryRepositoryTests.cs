using TelemetryRig.Core.Database;
using TelemetryRig.Core.Models;

namespace TelemetryRig.Tests;

[TestClass]
public sealed class SqliteTelemetryRepositoryTests
{
    [TestMethod]
    public async Task InsertBatchAsync_ThenListRecentAsync_ReturnsSavedRows()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "TelemetryRigTests", Guid.NewGuid() + ".db");
        var repository = new SqliteTelemetryRepository(databasePath);
        await repository.InitializeAsync(CancellationToken.None);

        var packets = new[]
        {
            new TelemetryPacket(DateTimeOffset.UtcNow, 1, "Test", 50, 2000, 2, 0.4, 0, 0.1, 31, 0.02, "Asphalt", 61),
            new TelemetryPacket(DateTimeOffset.UtcNow, 2, "Test", 55, 2200, 2, 0.5, 0, 0.2, 32, 0.03, "Asphalt", 61)
        };

        await repository.InsertBatchAsync(packets, CancellationToken.None);
        var recent = await repository.ListRecentAsync(10, CancellationToken.None);

        Assert.AreEqual(2, recent.Count);
        Assert.AreEqual(2, recent[0].FrameId); // newest first
    }
}
