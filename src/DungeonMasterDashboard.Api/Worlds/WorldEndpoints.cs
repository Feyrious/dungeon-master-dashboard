using Microsoft.EntityFrameworkCore;
using DungeonMasterDashboard.Api.Common;
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
        var validation = Validate(request);
        if (validation.Any)
        {
            return TypedResults.ValidationProblem(validation.Errors);
        }

        // New worlds go last.
        var world = new World { SortOrder = (await db.Worlds.MaxAsync(w => (int?)w.SortOrder, cancellationToken) ?? 0) + 1 };
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

        var validation = Validate(request);
        if (validation.Any)
        {
            return TypedResults.ValidationProblem(validation.Errors);
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

    private static async Task<IResult> GetImageAsync(int id, DashboardDbContext db, CancellationToken cancellationToken) =>
        ImageUpload.Serve(await db.WorldImages.AsNoTracking().FirstOrDefaultAsync(i => i.WorldId == id, cancellationToken));

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

        var image = await db.WorldImages.FindAsync([id], cancellationToken)
            ?? db.WorldImages.Add(new WorldImage { WorldId = id, Data = [], ContentType = "" }).Entity;
        ImageUpload.Store(image, upload, time.GetUtcNow());
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
        world.Tagline = TextInput.Clean(request.Tagline);
        world.Label = TextInput.Clean(request.Label);
        world.Url = TextInput.Clean(request.Url);
        world.WikiSoftware = TextInput.Clean(request.WikiSoftware);
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

    private static ValidationErrors Validate(WorldDto request)
    {
        var validation = new ValidationErrors();
        validation.Name(request.Name, required: true);
        validation.MaxLength(nameof(request.Tagline), request.Tagline, TaglineMaxLength);
        validation.MaxLength(nameof(request.Label), request.Label, LabelMaxLength);

        if (!string.IsNullOrWhiteSpace(request.Url) && !IsWebAddress(request.Url.Trim()))
        {
            validation.Add(nameof(request.Url), "Use a full web address starting with https:// or http://.");
        }

        if (!string.IsNullOrWhiteSpace(request.WikiSoftware) && (request.WikiSoftware.Length > WikiSoftwareMaxLength || !Id().IsMatch(request.WikiSoftware)))
        {
            validation.Add(nameof(request.WikiSoftware), "Unknown wiki software.");
        }

        validation.Theme(request.Theme);
        return validation;
    }
}
