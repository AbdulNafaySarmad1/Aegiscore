using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.I18n;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.I18n;

public class TranslationService : ITranslationService
{
    private readonly ITranslationKeyRepository _keyRepo;
    private readonly ITranslationValueRepository _valueRepo;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(
        ITranslationKeyRepository keyRepo,
        ITranslationValueRepository valueRepo,
        ILogger<TranslationService> logger)
    {
        _keyRepo = keyRepo;
        _valueRepo = valueRepo;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(string key, string languageCode, CancellationToken cancellationToken = default)
    {
        var translationKey = await _keyRepo.GetByKeyAsync(key, cancellationToken);
        if (translationKey == null) return key;

        var value = translationKey.Values.FirstOrDefault(v => v.LanguageCode == languageCode);
        if (value != null) return value.Value;

        var fallback = translationKey.Values.FirstOrDefault(v => v.LanguageCode == "en");
        return fallback?.Value ?? key;
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(string languageCode, string? module = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TranslationKey> keys;
        if (!string.IsNullOrWhiteSpace(module))
            keys = await _keyRepo.GetByModuleAsync(module, cancellationToken);
        else
            keys = await _keyRepo.GetAllAsync(cancellationToken);

        var result = new Dictionary<string, string>();
        foreach (var key in keys)
        {
            var value = key.Values.FirstOrDefault(v => v.LanguageCode == languageCode)
                     ?? key.Values.FirstOrDefault(v => v.LanguageCode == "en");

            result[key.Key] = value?.Value ?? key.Key;
        }
        return result;
    }

    public async Task<TranslationKey> AddKeyAsync(string key, string module, string defaultValue, CancellationToken cancellationToken = default)
    {
        var existing = await _keyRepo.GetByKeyAsync(key, cancellationToken);
        if (existing != null) return existing;

        var translationKey = new TranslationKey { Id = Guid.NewGuid(), Key = key, Module = module };
        await _keyRepo.AddAsync(translationKey, cancellationToken);

        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            var value = new TranslationValue { Id = Guid.NewGuid(), TranslationKeyId = translationKey.Id, LanguageCode = "en", Value = defaultValue };
            await _valueRepo.AddAsync(value, cancellationToken);
        }

        return translationKey;
    }

    public async Task<TranslationValue> SetTranslationAsync(Guid keyId, string languageCode, string value, CancellationToken cancellationToken = default)
    {
        var existing = await _valueRepo.GetByKeyAndLanguageAsync(keyId, languageCode, cancellationToken);

        if (existing != null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsMachineTranslated = false;
            await _valueRepo.UpdateAsync(existing, cancellationToken);
            return existing;
        }

        var translation = new TranslationValue { Id = Guid.NewGuid(), TranslationKeyId = keyId, LanguageCode = languageCode, Value = value };
        await _valueRepo.AddAsync(translation, cancellationToken);
        return translation;
    }
}
