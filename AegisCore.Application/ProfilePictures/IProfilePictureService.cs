using AegisCore.Domain.Entities;

namespace AegisCore.Application.ProfilePictures;

public interface IProfilePictureService
{
    Task<ProfilePictureResult> UploadAsync(Stream imageStream, string contentType, Guid userId, Guid tenantId, CancellationToken ct);
    Task<Stream?> GetThumbnailAsync(Guid userId, CancellationToken ct);
    Task<Stream?> GetFullAsync(Guid userId, CancellationToken ct);
    Task<ProfilePictureRecord?> GetRecordAsync(Guid userId, CancellationToken ct);
    Task<bool> ValidateIsRealFaceAsync(Stream imageStream, CancellationToken ct);
    Task<bool> DetectEditingAsync(Stream imageStream, CancellationToken ct);
}

public class ProfilePictureResult
{
    public Guid Id { get; set; }
    public string FullUrl { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public bool FaceValidated { get; set; }
    public bool EditingDetected { get; set; }
    public bool Accepted { get; set; }
    public string? RejectionReason { get; set; }
}
