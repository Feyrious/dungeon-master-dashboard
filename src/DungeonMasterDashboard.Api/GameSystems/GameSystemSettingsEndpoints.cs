using Microsoft.EntityFrameworkCore;
using DungeonMasterDashboard.Api.Data;
using DungeonMasterDashboard.Api.Images;
using DungeonMasterDashboard.Contracts.GameSystems;
using static DungeonMasterDashboard.Contracts.SettingsLimits;

namespace DungeonMasterDashboard.Api.GameSystems;

// Settings and tile images for game systems. The systems themselves are defined in the web
// app, so ids are only checked for format here.
internal static class GameSystemSettingsEndpoints
{
    public static RouteGroupBuilder MapGameSystemSettings(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/systems");

        group.MapGet("/settings", GetAllAsync);
        group.MapGet("/{systemId}/settings", GetAsync);
        group.MapPut("/{systemId}/settings", SaveAsync);
        group.MapDelete("/{systemId}/settings", ResetAsync);
        group.MapGet("/{systemId}/image", GetImageAsync);
        group.MapPut("/{systemId}/image", UploadImageAsync);
        group.MapDelete("/{systemId}/image", DeleteImageAsync);

        return group;
    }

    private static async Task<IResult> GetAllAsync(DashboardDbContext db, CancellationToken cancellationToken)
    {
        var settings = await db.GameSystemSettings.AsNoTracking().ToDictionaryAsync(s => s.SystemId, cancellationToken);
        var images = await db.GameSystemImages.AsNoTracking()
            .Select(i => new { i.SystemId, i.UpdatedAt })
            .ToDictionaryAsync(i => i.SystemId, i => i.UpdatedAt, cancellationToken);

        var ids = settings.Keys.Union(images.Keys).Order();
        return TypedResults.Ok(ids.Select(id => ToDto(id, settings.GetValueOrDefault(id), images.GetValueOrDefault(id))).ToList());
    }

    private static async Task<IResult> GetAsync(string systemId, DashboardDbContext db, CancellationToken cancellationToken)
    {
        if (!Id().IsMatch(systemId))
        {
            return TypedResults.NotFound();
        }

        var settings = await db.GameSystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.SystemId == systemId, cancellationToken);
        var imageUpdatedAt = await ImageUpdatedAtAsync(db, systemId, cancellationToken);

        return settings is null && imageUpdatedAt is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToDto(systemId, settings, imageUpdatedAt));
    }

    private static async Task<IResult> SaveAsync(
        string systemId, GameSystemSettingsDto request, DashboardDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (!Id().IsMatch(systemId))
        {
            return TypedResults.NotFound();
        }

        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var settings = await db.GameSystemSettings.FirstOrDefaultAsync(s => s.SystemId == systemId, cancellationToken);
        if (settings is null)
        {
            settings = new GameSystemSettings { SystemId = systemId };
            db.GameSystemSettings.Add(settings);
        }

        settings.Name = Clean(request.Name);
        settings.Tagline = Clean(request.Tagline);
        settings.IsHidden = request.IsHidden;
        settings.HiddenToolIds = request.HiddenToolIds.Distinct().ToList();
        settings.IsDark = request.IsDark;
        settings.Background = request.Background;
        settings.Surface = request.Surface;
        settings.Text = request.Text;
        settings.MutedText = request.MutedText;
        settings.Accent = request.Accent;
        settings.AccentText = request.AccentText;
        settings.Border = request.Border;
        settings.HeadingFont = Clean(request.HeadingFont);
        settings.BodyFont = Clean(request.BodyFont);
        settings.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ToDto(systemId, settings, await ImageUpdatedAtAsync(db, systemId, cancellationToken)));
    }

    // Removes all changes, including an uploaded image.
    private static async Task<IResult> ResetAsync(string systemId, DashboardDbContext db, CancellationToken cancellationToken)
    {
        await db.GameSystemSettings.Where(s => s.SystemId == systemId).ExecuteDeleteAsync(cancellationToken);
        await db.GameSystemImages.Where(i => i.SystemId == systemId).ExecuteDeleteAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetImageAsync(string systemId, DashboardDbContext db, CancellationToken cancellationToken)
    {
        var image = await db.GameSystemImages.AsNoTracking().FirstOrDefaultAsync(i => i.SystemId == systemId, cancellationToken);
        return image is null
            ? TypedResults.NotFound()
            : TypedResults.File(image.Data, image.ContentType, lastModified: image.UpdatedAt);
    }

    private static async Task<IResult> UploadImageAsync(
        string systemId, HttpRequest request, DashboardDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (!Id().IsMatch(systemId))
        {
            return TypedResults.NotFound();
        }

        var (upload, error) = await ImageUpload.ReadAsync(request, cancellationToken);
        if (upload is null)
        {
            return error!;
        }

        var image = await db.GameSystemImages.FirstOrDefaultAsync(i => i.SystemId == systemId, cancellationToken);
        if (image is null)
        {
            image = new GameSystemImage { SystemId = systemId, Data = upload.Data, ContentType = upload.ContentType };
            db.GameSystemImages.Add(image);
        }

        image.Data = upload.Data;
        image.ContentType = upload.ContentType;
        image.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteImageAsync(string systemId, DashboardDbContext db, CancellationToken cancellationToken)
    {
        await db.GameSystemImages.Where(i => i.SystemId == systemId).ExecuteDeleteAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<DateTimeOffset?> ImageUpdatedAtAsync(DashboardDbContext db, string systemId, CancellationToken cancellationToken) =>
        db.GameSystemImages.Where(i => i.SystemId == systemId).Select(i => (DateTimeOffset?)i.UpdatedAt).FirstOrDefaultAsync(cancellationToken);

    private static GameSystemSettingsDto ToDto(string systemId, GameSystemSettings? settings, DateTimeOffset? imageUpdatedAt) => new()
    {
        SystemId = systemId,
        Name = settings?.Name,
        Tagline = settings?.Tagline,
        IsHidden = settings?.IsHidden ?? false,
        HiddenToolIds = settings?.HiddenToolIds ?? [],
        IsDark = settings?.IsDark,
        Background = settings?.Background,
        Surface = settings?.Surface,
        Text = settings?.Text,
        MutedText = settings?.MutedText,
        Accent = settings?.Accent,
        AccentText = settings?.AccentText,
        Border = settings?.Border,
        HeadingFont = settings?.HeadingFont,
        BodyFont = settings?.BodyFont,
        ImageUpdatedAt = imageUpdatedAt,
    };

    private static Dictionary<string, string[]> Validate(GameSystemSettingsDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Name is not null && (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > NameMaxLength))
        {
            errors[nameof(request.Name)] = [$"Name must be 1 to {NameMaxLength} characters."];
        }

        if (request.Tagline is { Length: > TaglineMaxLength })
        {
            errors[nameof(request.Tagline)] = [$"Tagline can be at most {TaglineMaxLength} characters."];
        }

        (string Field, string? Value)[] colors =
        [
            (nameof(request.Background), request.Background), (nameof(request.Surface), request.Surface),
            (nameof(request.Text), request.Text), (nameof(request.MutedText), request.MutedText),
            (nameof(request.Accent), request.Accent), (nameof(request.AccentText), request.AccentText),
            (nameof(request.Border), request.Border),
        ];
        foreach (var (field, value) in colors.Where(c => c.Value is not null && !Color().IsMatch(c.Value)))
        {
            errors[field] = ["Use a hex color like #1a2b3c."];
        }

        (string Field, string? Value)[] fonts = [(nameof(request.HeadingFont), request.HeadingFont), (nameof(request.BodyFont), request.BodyFont)];
        foreach (var (field, value) in fonts.Where(f => f.Value is not null && (f.Value.Length > FontMaxLength || !FontFamily().IsMatch(f.Value))))
        {
            errors[field] = ["Not a valid font list."];
        }

        if (request.HiddenToolIds.Count > 100 || request.HiddenToolIds.Any(id => !Id().IsMatch(id)))
        {
            errors[nameof(request.HiddenToolIds)] = ["Invalid tool ids."];
        }

        return errors;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
