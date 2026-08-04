namespace AegisCore.Application.AI;

public interface IDocumentOcrService
{
    Task<OcrResult> ExtractTextAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormField>> ExtractFormFieldsAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
    Task<DocumentClassification> ClassifyDocumentAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default);
}
