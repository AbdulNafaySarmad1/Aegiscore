using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AegisCore.API.Validation;

/// <summary>
/// Runs FluentValidation asynchronously so validators may safely perform tenant-scoped existence checks.
/// </summary>
public sealed class AsyncFluentValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var modelType = argument!.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
            var validators = services.GetServices(validatorType).Cast<IValidator>().ToArray();
            if (validators.Length == 0) continue;

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(modelType);
            var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument)!;
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                if (result.IsValid) continue;
                foreach (var failure in result.Errors)
                    context.ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        await next();
    }
}
