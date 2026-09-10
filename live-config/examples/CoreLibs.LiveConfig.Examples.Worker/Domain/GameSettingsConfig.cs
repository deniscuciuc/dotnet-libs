namespace CoreLibs.LiveConfig.Examples.Worker.Domain;

/// <summary>
/// Example game settings DTO.
/// </summary>
public record GameSettingsConfig(
    int MaxPlayers,
    int RoundDurationSeconds,
    decimal MinBetAmount,
    decimal MaxBetAmount,
    bool MaintenanceMode);
