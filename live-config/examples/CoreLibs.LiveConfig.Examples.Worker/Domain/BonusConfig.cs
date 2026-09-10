namespace CoreLibs.LiveConfig.Examples.Worker.Domain;

/// <summary>
/// Example bonus configuration DTO.
/// In a real application, this would match your game's bonus structure.
/// </summary>
public record BonusConfig(
    string Name,
    decimal Amount,
    string Currency,
    int MinLevel,
    bool IsActive);
