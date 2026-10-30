using static DungeonMasterDashboard.Contracts.SettingsLimits;

namespace DungeonMasterDashboard.Api.Images;

internal sealed record UploadedImage(byte[] Data, string ContentType);

// Reads an uploaded tile image from the request body and checks its type, size and signature.
// Returns either the image or the error response to send.
internal static class ImageUpload
{
    public static async Task<(UploadedImage? Image, IResult? Error)> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var contentType = request.ContentType?.Split(';')[0].Trim().ToLowerInvariant();
        if (contentType is null || !ImageContentTypes.Contains(contentType))
        {
            return (null, TypedResults.Problem("Upload a PNG, JPEG or WebP image.", statusCode: StatusCodes.Status415UnsupportedMediaType));
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > ImageMaxBytes)
            {
                return (null, TypedResults.Problem($"Images can be at most {ImageMaxBytes / 1024 / 1024} MB.", statusCode: StatusCodes.Status413PayloadTooLarge));
            }
        }

        var data = buffer.ToArray();
        if (!LooksLike(contentType, data))
        {
            return (null, TypedResults.Problem("The file is not a valid image.", statusCode: StatusCodes.Status400BadRequest));
        }

        return (new UploadedImage(data, contentType), null);
    }

    // Checks the file signature so a renamed non-image is rejected.
    private static bool LooksLike(string contentType, byte[] data) => contentType switch
    {
        "image/png" => data.AsSpan().StartsWith((byte[])[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        "image/jpeg" => data.AsSpan().StartsWith((byte[])[0xFF, 0xD8, 0xFF]),
        "image/webp" => data.Length > 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        _ => false,
    };
}
