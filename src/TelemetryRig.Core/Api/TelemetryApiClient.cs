using System.Net.Http.Json;
using TelemetryRig.Core.Models;

namespace TelemetryRig.Core.Api;

/// <summary>
/// Real HTTP API integration.
///
/// Important beginner idea:
/// HttpClient should usually be reused. Do not create a new HttpClient for every request.
/// Creating many HttpClient instances can waste sockets and hurt performance.
/// </summary>
public sealed class TelemetryApiClient : ITelemetryApiClient
{
    private readonly HttpClient _httpClient;

    public TelemetryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GameProfile> GetGameProfileAsync(string gameName, CancellationToken cancellationToken)
    {
        // Example expected endpoint:
        // GET /api/gameprofiles/SimRace%20Pro
        var url = $"api/gameprofiles/{Uri.EscapeDataString(gameName)}";
        var profile = await _httpClient.GetFromJsonAsync<GameProfile>(url, cancellationToken)
            .ConfigureAwait(false);

        return profile ?? throw new InvalidOperationException("The API returned an empty game profile.");
    }

    public async Task<ApiUploadResult> UploadTelemetryBatchAsync(
        IReadOnlyList<TelemetryPacket> packets,
        CancellationToken cancellationToken)
    {
        // Example expected endpoint:
        // POST /api/telemetry/batch
        var response = await _httpClient.PostAsJsonAsync("api/telemetry/batch", packets, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return new ApiUploadResult(false, 0, $"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        return new ApiUploadResult(true, packets.Count, "Uploaded successfully.");
    }
}
