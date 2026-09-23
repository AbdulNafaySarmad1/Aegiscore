using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AegisCore.Application.Integrations;

namespace AegisCore.Infrastructure.Integrations.Forms;

/// <summary>
/// The documented AegisCore contract (docs/integrations/external-forms.md): a JSON body with
/// <c>submissionId</c>, an optional <c>formId</c>, and <c>answers</c> as either an array of
/// <c>{ fieldId, label?, value }</c> or an object of field id → value. Any form tool, or a small
/// relay in front of one, can send this.
/// </summary>
public sealed class GenericFormPayloadAdapter : IFormPayloadAdapter
{
    public string Provider => "generic";

    public FormSubmissionEnvelope Parse(string contentType, string body, IReadOnlyDictionary<string, string> formFields)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new FormatException("The body is empty.");
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new FormatException("The body must be a JSON object.");
        var submissionId = FormPayloadText.Scalar(root, "submissionId") ?? throw new FormatException("submissionId is required.");
        var formId = FormPayloadText.Scalar(root, "formId");
        if (!root.TryGetProperty("answers", out var answers)) throw new FormatException("answers is required.");

        var result = new List<FormAnswer>();
        if (answers.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in answers.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) throw new FormatException("Each answer must be an object.");
                var fieldId = FormPayloadText.Scalar(item, "fieldId") ?? throw new FormatException("Each answer needs a fieldId.");
                var label = FormPayloadText.Scalar(item, "label");
                if (!item.TryGetProperty("value", out var value)) throw new FormatException($"Answer {fieldId} has no value.");
                FormPayloadText.Flatten(fieldId, label, value, result);
            }
        }
        else if (answers.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in answers.EnumerateObject()) FormPayloadText.Flatten(property.Name, null, property.Value, result);
        }
        else throw new FormatException("answers must be an array or an object.");

        return new FormSubmissionEnvelope(formId, submissionId, result);
    }
}

/// <summary>
/// JotForm's webhook: <c>multipart/form-data</c> with <c>formID</c>, <c>submissionID</c> and
/// <c>rawRequest</c>, a JSON string whose question keys look like <c>q12_instructorKnowledge</c>.
/// Keys that are not questions (<c>slug</c>, <c>event_id</c>, <c>timeToSubmit</c>, …) are dropped.
/// Matrix (Likert table) questions arrive as nested objects and become one field per row.
/// </summary>
public sealed partial class JotFormPayloadAdapter : IFormPayloadAdapter
{
    public string Provider => "jotform";

    public FormSubmissionEnvelope Parse(string contentType, string body, IReadOnlyDictionary<string, string> formFields)
    {
        if (!formFields.TryGetValue("rawRequest", out var raw) || string.IsNullOrWhiteSpace(raw))
            throw new FormatException("rawRequest is missing; JotForm sends multipart/form-data with a rawRequest field.");
        formFields.TryGetValue("submissionID", out var submissionId);
        if (string.IsNullOrWhiteSpace(submissionId)) throw new FormatException("submissionID is missing.");
        formFields.TryGetValue("formID", out var formId);

        using var document = JsonDocument.Parse(raw);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new FormatException("rawRequest must be a JSON object.");
        var answers = new List<FormAnswer>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var match = QuestionKey().Match(property.Name);
            if (!match.Success) continue;
            FormPayloadText.Flatten(property.Name, FormPayloadText.Humanize(match.Groups["name"].Value), property.Value, answers);
        }
        return new FormSubmissionEnvelope(string.IsNullOrWhiteSpace(formId) ? null : formId.Trim(), submissionId.Trim(), answers);
    }

    [GeneratedRegex("^q\\d+_(?<name>[A-Za-z0-9_]+)$")]
    private static partial Regex QuestionKey();
}

public static partial class FormPayloadText
{
    public const int MaxFieldIdLength = 200;
    public const int MaxValueLength = 2000;

    public static string? Scalar(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>Scalars become one answer; objects and arrays become <c>field[key]</c> answers, so a matrix row is its own field.</summary>
    public static void Flatten(string fieldId, string? label, JsonElement value, List<FormAnswer> into, int depth = 0)
    {
        if (depth > 3) return;
        if (fieldId.Length > MaxFieldIdLength) throw new FormatException("A field id is longer than 200 characters.");
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                    Flatten($"{fieldId}[{property.Name}]", label is null ? null : $"{label} — {property.Name}", property.Value, into, depth + 1);
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in value.EnumerateArray())
                {
                    Flatten($"{fieldId}[{index}]", label is null ? null : $"{label} — {index + 1}", item, into, depth + 1);
                    index++;
                }
                break;
            case JsonValueKind.String:
                var text = value.GetString() ?? "";
                into.Add(new FormAnswer(fieldId, label, text.Length > MaxValueLength ? text[..MaxValueLength] : text));
                break;
            case JsonValueKind.Number:
                into.Add(new FormAnswer(fieldId, label, value.GetRawText()));
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                into.Add(new FormAnswer(fieldId, label, value.GetBoolean() ? "true" : "false"));
                break;
        }
    }

    /// <summary><c>instructorKnowledge</c> → "Instructor knowledge", the best a JotForm key offers until the real question text is loaded.</summary>
    public static string Humanize(string slug)
    {
        var spaced = CamelBoundary().Replace(slug.Replace('_', ' '), " $1").Trim();
        return spaced.Length == 0 ? slug : char.ToUpperInvariant(spaced[0]) + spaced[1..].ToLowerInvariant();
    }

    private static readonly Dictionary<string, decimal> Likert = new(StringComparer.OrdinalIgnoreCase)
    {
        ["strongly disagree"] = 1, ["disagree"] = 2, ["neutral"] = 3, ["neither agree nor disagree"] = 3, ["undecided"] = 3,
        ["agree"] = 4, ["strongly agree"] = 5,
        ["very poor"] = 1, ["poor"] = 2, ["fair"] = 3, ["average"] = 3, ["good"] = 4, ["very good"] = 5, ["excellent"] = 5,
        ["very dissatisfied"] = 1, ["dissatisfied"] = 2, ["satisfied"] = 4, ["very satisfied"] = 5,
    };

    /// <summary>
    /// A rating from a number ("4"), a numbered option ("4 - Agree") or a five-point label ("Agree").
    /// Labels are only understood on a five-point scale; other scales must send numbers.
    /// </summary>
    public static decimal? Score(string value, int scaleMax)
    {
        var text = value.Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)) return number;
        var leading = LeadingNumber().Match(text);
        if (leading.Success && decimal.TryParse(leading.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out number)) return number;
        return scaleMax == 5 && Likert.TryGetValue(text, out var label) ? label : null;
    }

    [GeneratedRegex("(?<=[a-z0-9])([A-Z])")]
    private static partial Regex CamelBoundary();

    [GeneratedRegex("^(\\d+(?:\\.\\d+)?)\\s*[-–:.)]")]
    private static partial Regex LeadingNumber();
}
