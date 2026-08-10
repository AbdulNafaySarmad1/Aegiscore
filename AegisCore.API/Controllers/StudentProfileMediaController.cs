using System.Security.Claims;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/student-profile-media")]
[Authorize(Roles = "Student,SuperAdmin,Admin")]
public sealed class StudentProfileMediaController(
    AegisDbContext db,
    IConfiguration configuration,
    ILogger<StudentProfileMediaController> logger) : ControllerBase
{
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private Guid StudentId => Guid.TryParse(
        User.FindFirstValue("student_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        out var id) ? id : Guid.Empty;

    [HttpGet("me/avatar")]
    public async Task<IActionResult> GetMyAvatar(CancellationToken ct)
    {
        var identity = await ResolveIdentityAsync(ct);
        if (identity is null) return NotFound();

        var avatar = FindAvatar(identity.Value.TenantId, identity.Value.StudentId);
        if (avatar is null) return Ok(new { dataUrl = (string?)null });

        var bytes = await System.IO.File.ReadAllBytesAsync(avatar.Value.Path, ct);
        return Ok(new { dataUrl = $"data:{avatar.Value.ContentType};base64,{Convert.ToBase64String(bytes)}" });
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarBytes + 64 * 1024)]
    public async Task<IActionResult> UploadMyAvatar([FromForm] IFormFile avatar, CancellationToken ct)
    {
        if (avatar is null || avatar.Length == 0) return BadRequest(new { error = "Select an image to upload." });
        if (avatar.Length > MaxAvatarBytes) return BadRequest(new { error = "Profile image must be 2 MB or smaller." });
        if (!AllowedTypes.TryGetValue(avatar.ContentType, out var extension))
            return BadRequest(new { error = "Only JPEG, PNG, and WebP profile images are allowed." });

        var identity = await ResolveIdentityAsync(ct);
        if (identity is null) return NotFound();

        var bytes = new byte[avatar.Length];
        await using (var input = avatar.OpenReadStream())
        {
            var offset = 0;
            while (offset < bytes.Length)
            {
                var read = await input.ReadAsync(bytes.AsMemory(offset), ct);
                if (read == 0) break;
                offset += read;
            }
            if (offset != bytes.Length) return BadRequest(new { error = "Profile image upload was incomplete." });
        }

        if (!LooksLikeImage(bytes, avatar.ContentType))
            return BadRequest(new { error = "The uploaded file does not match its declared image type." });

        var directory = AvatarDirectory(identity.Value.TenantId);
        Directory.CreateDirectory(directory);
        RemoveExisting(identity.Value.TenantId, identity.Value.StudentId);

        var finalPath = Path.Combine(directory, $"{identity.Value.StudentId:D}{extension}");
        var tempPath = finalPath + ".tmp";
        await System.IO.File.WriteAllBytesAsync(tempPath, bytes, ct);
        System.IO.File.Move(tempPath, finalPath, true);

        logger.LogInformation("Updated profile avatar for student {StudentId}", identity.Value.StudentId);
        return Ok(new { dataUrl = $"data:{avatar.ContentType};base64,{Convert.ToBase64String(bytes)}" });
    }

    private async Task<(Guid TenantId, Guid StudentId)?> ResolveIdentityAsync(CancellationToken ct)
    {
        var studentId = StudentId;
        if (studentId == Guid.Empty) return null;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId, ct);
        return user is null ? null : (user.TenantId, studentId);
    }

    private (string Path, string ContentType)? FindAvatar(Guid tenantId, Guid studentId)
    {
        var directory = AvatarDirectory(tenantId);
        if (!Directory.Exists(directory)) return null;
        foreach (var pair in AllowedTypes)
        {
            var path = Path.Combine(directory, $"{studentId:D}{pair.Value}");
            if (System.IO.File.Exists(path)) return (path, pair.Key);
        }
        return null;
    }

    private void RemoveExisting(Guid tenantId, Guid studentId)
    {
        var directory = AvatarDirectory(tenantId);
        foreach (var extension in AllowedTypes.Values)
        {
            var path = Path.Combine(directory, $"{studentId:D}{extension}");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }

    private string AvatarDirectory(Guid tenantId)
    {
        var basePath = configuration["AEGISCORE_STORAGE_LOCAL_PATH"]
            ?? configuration["Storage:LocalPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "storage");
        return Path.Combine(basePath, "profile-images", tenantId.ToString("D"));
    }

    private static bool LooksLikeImage(byte[] bytes, string contentType)
    {
        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            return bytes.Length >= 12
                && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        return false;
    }
}
