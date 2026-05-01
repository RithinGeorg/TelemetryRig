using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Database;

public interface ITelemetryRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task InsertBatchAsync(IReadOnlyList<TelemetryPacket> packets, CancellationToken cancellationToken);
    Task<IReadOnlyList<TelemetryPacket>> ListRecentAsync(int take, CancellationToken cancellationToken);
}
