namespace TelemetryRig.Core.Api;

/// <summary>
/// Configuration downloaded from an external API.
/// Example: a game-specific profile that tells our app how strongly to apply haptic effects.
/// </summary>
public sealed record GameProfile(
    string GameName,
    double HapticsGain,
    double ForceFeedbackGain,
    string Notes);

public sealed record ApiUploadResult(bool Success, int UploadedCount, string Message);
