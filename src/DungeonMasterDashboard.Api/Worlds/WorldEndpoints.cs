using Microsoft.EntityFrameworkCore;
using DungeonMasterDashboard.Api.Data;
using DungeonMasterDashboard.Api.Images;
using DungeonMasterDashboard.Contracts;
using DungeonMasterDashboard.Contracts.Worlds;
using static DungeonMasterDashboard.Contracts.SettingsLimits;

namespace DungeonMasterDashboard.Api.Worlds;

internal static class WorldEndpoints
{
    public static RouteGroupBuilder MapWorlds(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/worlds");

        group.MapGet("/", GetAllAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);
        group.MapGet("/{id:int}/image", GetImageAsync);
        group.MapPut("/{id:int}/image", UploadImageAsync);
        group.MapDelete("/{id:int}/image", DeleteImageAsync);

        return group;
    }

    private static async Task<IResult> GetAllAsync(DashboardDbContext db, CancellationToken cancellationToken)
    {
        var worlds = await db.Worlds.AsNoTracking().OrderBy(w => w.SortOrder).ThenBy(w => w.Id).ToListAsync(cancellationToken);
        var images = await db.WorldImages.AsNoTracking()
            .Select(i => new { i.WorldId, i.UpdatedAt })
            .ToDictionaryAsync(i => i.WorldId, i => i.UpdatedAt, cancellationToken);

        return TypedResults.Ok(worlds.Select(w => ToDto(w, images.TryGetValue(w.Id, out var updatedAt) ? updatedAt : null)).ToList());
    }

    private static async Task<IResult> CreateAsync(WorldDto request, DashboardDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var world = new World
        {
            Name = "", Background = "", Surface = "", Text = "", MutedText = "",
            Accent = "", AccentText = "", Border = "", HeadingFont = "", BodyFont = "",
            SortOrder = (await db.Worlds.MaxAsync(w => (int?)w.SortOrder, cancellationToken) ?? 0) + 1,
        };
        Apply(world, request, time);
        db.Worlds.Add(world);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/worlds/{world.Id}", ToDto(world, null));
    }

    private static async Task<IResult> UpdateAsync(int id, WorldDto request, DashboardDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        var world = await db.Worlds.FindAsync([id], cancellationToken);
        if (world is null)
        {
            return TypedResults.NotFound();
        }

        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        Apply(world, request, time);
        await db.SaveChangesAsync(cancellationToken);

        var imageUpdatedAt = await db.WorldImages.Where(i => i.WorldId == id).Select(i => (DateTimeOffset?)i.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
        return TypedResults.Ok(ToDto(world, imageUpdatedAt));
    }

    private static async Task<IResult> DeleteAsync(int id, DashboardDbContext db, CancellationToken cancellationToken)
    {
        await db.WorldImages.Where(i => i.WorldId == id).ExecuteDeleteAsync(cancellationToken);
        var deleted = await db.Worlds.Where(w => w.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static async Task<IResult> GetImageAsync(int id, DashboardDbContext db, CancellationToken cancellationToken)
    {
        var image = await db.WorldImages.AsNoTracking().FirstOrDefaultAsync(i => i.WorldId == id, cancellationToken);
        return image is null
            ? TypedResults.NotFound()
            : TypedResults.File(image.Data, image.ContentType, lastModified: image.UpdatedAt);
    }

    private static async Task<IResult> UploadImageAsync(
        int id, HttpRequest request, DashboardDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        if (!await db.Worlds.AnyAsync(w => w.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var (upload, error) = await ImageUpload.ReadAsync(request, cancellationToken);
        if (upload is null)
        {
            return error!;
        }

        var image = await db.WorldImages.FirstOrDefaultAsync(i => i.WorldId == id, cancellationToken);
        if (image is null)
        {
            image = new WorldImage { WorldId = id, Data = upload.Data, ContentType = upload.ContentType };
            db.WorldImages.Add(image);
        }

        image.Data = upload.Data;
        image.ContentType = upload.ContentType;
        image.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteImageAsync(int id, DashboardDbContext db, CancellationToken cancellationToken)
    {
        await db.WorldImages.Where(i => i.WorldId == id).ExecuteDeleteAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static void Apply(World world, WorldDto request, TimeProvider time)
    {
        world.Name = request.Name.Trim();
        world.Tagline = Clean(request.Tagline);
        world.Label = Clean(request.Label);
        world.Url = Clean(request.Url);
        world.WikiSoftware = Clean(request.WikiSoftware);
        world.IsDark = request.Theme.IsDark;
        world.Background = request.Theme.Background;
        world.Surface = request.Theme.Surface;
        world.Text = request.Theme.Text;
        world.MutedText = request.Theme.MutedText;
        world.Accent = request.Theme.Accent;
        world.AccentText = request.Theme.AccentText;
        world.Border = request.Theme.Border;
        world.HeadingFont = request.Theme.HeadingFont.Trim();
        world.BodyFont = request.Theme.BodyFont.Trim();
        world.UpdatedAt = time.GetUtcNow();
    }

    private static WorldDto ToDto(World world, DateTimeOffset? imageUpdatedAt) => new()
    {
        Id = world.Id,
        Name = world.Name,
        Tagline = world.Tagline,
        Label = world.Label,
        Url = world.Url,
        WikiSoftware = world.WikiSoftware,
        Theme = new ThemeDto(
            world.IsDark, world.Background, world.Surface, world.Text, world.MutedText,
            world.Accent, world.AccentText, world.Border, world.HeadingFont, world.BodyFont),
        BuiltInImage = world.BuiltInImage,
        ImageUpdatedAt = imageUpdatedAt,
    };

    private static Dictionary<string, string[]> Validate(WorldDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > NameMaxLength)
        {
            errors[nameof(request.Name)] = [$"Name must be 1 to {NameMaxLength} characters."];
        }

        if (request.Tagline is { Length: > TaglineMaxLength })
        {
            errors[nameof(request.Tagline)] = [$"Tagline can be at most {TaglineMaxLength} characters."];
        }

        if (request.Label is { Length: > LabelMaxLength })
        {
            errors[nameof(request.Label)] = [$"Label can be at most {LabelMaxLength} characters."];
        }

        if (!string.IsNullOrWhiteSpace(request.Url) && !IsWebAddress(request.Url.Trim()))
        {
            errors[nameof(request.Url)] = ["Use a full web address starting with https:// or http://."];
        }

        if (!string.IsNullOrWhiteSpace(request.WikiSoftware) && (request.WikiSoftware.Length > WikiSoftwareMaxLength || !Id().IsMatch(request.WikiSoftware)))
        {
            errors[nameof(request.WikiSoftware)] = ["Unknown wiki software."];
        }

        ThemeValidation.Validate(request.Theme, errors);
        return errors;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class ThemeValidation
{
    // Theme values end up in a style attribute, so only plain hex colors and safe font lists pass.
    public static void Validate(ThemeDto? theme, Dictionary<string, string[]> errors)
    {
        if (theme is null)
        {
            errors["Theme"] = ["A theme is required."];
            return;
        }

        (string Field, string Value)[] colors =
        [
            (nameof(theme.Background), theme.Background), (nameof(theme.Surface), theme.Surface),
            (nameof(theme.Text), theme.Text), (nameof(theme.MutedText), theme.MutedText),
            (nameof(theme.Accent), theme.Accent), (nameof(theme.AccentText), theme.AccentText),
            (nameof(theme.Border), theme.Border),
        ];
        foreach (var (field, _) in colors.Where(c => c.Value is null || !Color().IsMatch(c.Value)))
        {
            errors[$"Theme.{field}"] = ["Use a hex color like #1a2b3c."];
        }

        (string Field, string Value)[] fonts = [(nameof(theme.HeadingFont), theme.HeadingFont), (nameof(theme.BodyFont), theme.BodyFont)];
        foreach (var (field, _) in fonts.Where(f => f.Value is null || f.Value.Length > FontMaxLength || !FontFamily().IsMatch(f.Value)))
        {
            errors[$"Theme.{field}"] = ["Not a valid font list."];
        }
    }
}
