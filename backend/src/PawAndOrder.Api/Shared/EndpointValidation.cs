using FluentValidation;

namespace PawAndOrder.Api.Shared;

// Every validating endpoint returns the same 400 problem payload, so validation lives here.
internal static class EndpointValidation
{
    internal static async Task<IResult?> ValidateAsync<T>(T request, IValidator<T> validator)
    {
        var result = await validator.ValidateAsync(request);
        return result.IsValid ? null : Results.ValidationProblem(result.ToDictionary());
    }
}
