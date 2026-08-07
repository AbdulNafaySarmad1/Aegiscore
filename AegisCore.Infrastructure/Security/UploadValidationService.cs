namespace AegisCore.Infrastructure.Security;

public sealed record UploadValidationResult(bool IsValid, string? ErrorMessage, string? DetectedContentType = null, string? DetectedExtension = null);

public sealed class UploadValidationService
{
    private readonly long _maxFileSizeBytes;

    public UploadValidationService(long maxFileSizeBytes = 10 * 1024 * 1024)
    {
        _maxFileSizeBytes = maxFileSizeBytes;
    }

    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var sanitized = fileName
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "")
            .Replace("\0", "");

        return Path.GetFileName(sanitized);
    }

    public static bool HasDirectoryTraversal(string? contentDisposition)
    {
        if (string.IsNullOrWhiteSpace(contentDisposition))
            return false;

        return contentDisposition.Contains("..") ||
               contentDisposition.Contains('/') ||
               contentDisposition.Contains('\\');
    }

    public UploadValidationResult ValidateMagicBytes(Stream stream, params string[] allowedTypes)
    {
        if (stream is null)
            return new UploadValidationResult(false, "File stream is null.");

        if (stream.Length > _maxFileSizeBytes)
            return new UploadValidationResult(false, $"File exceeds the maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB.");

        if (stream.Length < 4)
            return new UploadValidationResult(false, "File is too small to validate.");

        Span<byte> magic = stackalloc byte[4];
        var bytesRead = stream.Read(magic);
        stream.Position = 0;

        if (bytesRead < 4)
            return new UploadValidationResult(false, "Could not read file header.");

        var (contentType, extension) = DetermineType(magic);

        if (contentType is null)
            return new UploadValidationResult(false, "Unrecognized file type.");

        if (allowedTypes.Length > 0 && !allowedTypes.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return new UploadValidationResult(false, $"File type '{extension}' is not accepted. Allowed: {string.Join(", ", allowedTypes)}.");

        return new UploadValidationResult(true, null, contentType, extension);
    }

    private static (string? ContentType, string Extension) DetermineType(Span<byte> magic)
    {
        if (magic.Length < 4)
            return (null, string.Empty);

        if (magic[0] == 0x89 && magic[1] == 0x50 && magic[2] == 0x4E && magic[3] == 0x47)
            return ("image/png", "png");

        if (magic[0] == 0xFF && magic[1] == 0xD8 && magic[2] == 0xFF)
            return ("image/jpeg", "jpg");

        if (magic[0] == 0x25 && magic[1] == 0x50 && magic[2] == 0x44 && magic[3] == 0x46)
            return ("application/pdf", "pdf");

        return (null, string.Empty);
    }
}
