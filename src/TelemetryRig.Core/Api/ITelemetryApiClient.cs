using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Api;

public interface ITelemetryApiClient
{
    Task<GameProfile> GetGameProfileAsync(string gameName, CancellationToken cancellationToken);
    Task<ApiUploadResult> UploadTelemetryBatchAsync(IReadOnlyList<TelemetryPacket> packets, CancellationToken cancellationToken);
}
