using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Api;

/// <summary>
/// Offline API implementation used by the sample app.
/// It teaches API flow without requiring a real server.
/// Replace this with TelemetryApiClient when your backend is ready.
/// </summary>
public sealed class FakeTelemetryApiClient : ITelemetryApiClient
{
    public async Task<GameProfile> GetGameProfileAsync(string gameName, CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken).ConfigureAwait(false); // Simulate network latency.

        return new GameProfile(
            gameName,
            HapticsGain: 1.15,
            ForceFeedbackGain: 0.9,
            Notes: "Fake API profile: haptics slightly boosted, force feedback softened.");
    }

    public async Task<ApiUploadResult> UploadTelemetryBatchAsync(
        IReadOnlyList<TelemetryPacket> packets,
        CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        return new ApiUploadResult(true, packets.Count, "Fake upload completed.");
    }
}
