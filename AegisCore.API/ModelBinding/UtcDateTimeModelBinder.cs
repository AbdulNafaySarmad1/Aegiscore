using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AegisCore.API.ModelBinding;

/// <summary>
/// Binds <see cref="DateTime"/> query and route values as UTC. ASP.NET parses "2026-09-01" with
/// Kind=Unspecified; Npgsql refuses to compare or write such a value against a
/// <c>timestamp with time zone</c> column, so every date-range report 500'd on PostgreSQL while
/// passing on SQLite. Every timestamp this system stores is UTC, so an unqualified value is UTC and
/// an offset-qualified value is converted to UTC.
/// </summary>
public sealed class UtcDateTimeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (result == ValueProviderResult.None) return Task.CompletedTask;
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
        var text = result.FirstValue;
        if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value))
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"'{text}' is not a valid date.");
            return Task.CompletedTask;
        }
        bindingContext.Result = ModelBindingResult.Success(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        return Task.CompletedTask;
    }
}

public sealed class UtcDateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = context.Metadata.UnderlyingOrModelType;
        if (type != typeof(DateTime)) return null;
        var source = context.BindingInfo.BindingSource;
        // Body payloads are handled by System.Text.Json (which already yields UTC for offset-qualified values).
        if (source is not null && source.CanAcceptDataFrom(BindingSource.Body)) return null;
        return new UtcDateTimeModelBinder();
    }
}
