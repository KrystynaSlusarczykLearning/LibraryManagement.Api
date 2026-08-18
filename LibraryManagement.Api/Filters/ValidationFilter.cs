using FluentValidation;
using LibraryManagement.Api.DTOs.Categories;

namespace LibraryManagement.Api.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        var dto = context.Arguments.OfType<T>().FirstOrDefault();
        var validator = context.HttpContext.RequestServices
            .GetRequiredService<IValidator<T>>();

        var validationResult = await validator.ValidateAsync(dto!);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}
