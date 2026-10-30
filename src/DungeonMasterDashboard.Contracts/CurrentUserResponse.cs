namespace DungeonMasterDashboard.Contracts;

public sealed record CurrentUserResponse(string Id, string Name, string? Email);
