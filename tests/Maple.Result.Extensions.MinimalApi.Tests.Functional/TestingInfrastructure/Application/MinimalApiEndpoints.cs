using Maple.Result.Extensions.MinimalApi.Tests.Functional.TestingInfrastructure.Application.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net;

namespace Maple.Result.Extensions.MinimalApi.Tests.Functional.TestingInfrastructure.Application;

// The alias is declared inside the namespace so that it takes precedence over the Maple.Result.IResult type.
using IResult = Microsoft.AspNetCore.Http.IResult;

/// <summary>
///     Maps a separate Minimal API endpoint for every scenario covered by the functional tests.
/// </summary>
internal static class MinimalApiEndpoints
{
    internal static void MapMinimalApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("minimal");

        MapSuccessEndpoints(group);
        MapErrorEndpoints(group);
        MapSuccessStatusCodeEndpoints(group);
        MapSuccessMappingEndpoints(group);
        MapErrorDetailsEndpoints(group);
        MapErrorCategoryEndpoints(group);
    }

    #region helper methods

    private static void MapSuccessEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("success", () => Result.Success().ToMinimalApiResult());

        endpoints.MapGet("success/value",
            () => Result<TestValue>.FromValue(new TestValue(13, "Test value")).ToMinimalApiResult());

        endpoints.MapGet("success/null-value",
            () => Result<TestValue?>.FromValue(null).ToMinimalApiResult());
    }

    private static void MapErrorEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("error", () => Result.FromError(CreateFailureError()).ToMinimalApiResult());

        endpoints.MapGet("error/not-found", () => Result.FromError(Error.NotFound(
            ErrorUri.Tag("tag:test.com,2026:not-found"),
            "Not found title",
            "Not found detail.",
            ErrorUri.Locator("https://test.com/instances/not-found"))).ToMinimalApiResult());

        endpoints.MapGet("error/custom-mapping",
            () => Result.FromError(CreateFailureError()).ToMinimalApiResult(MapFailureToPaymentRequired));

        endpoints.MapGet("error/custom-mapping-not-matching",
            () => Result.FromError(CreateFailureError()).ToMinimalApiResult(MapConflictToPaymentRequired));
    }

    private static void MapSuccessStatusCodeEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("status-code/success",
            () => Result.Success().ToMinimalApiResult(HttpStatusCode.Accepted));

        endpoints.MapGet("status-code/success/value",
            () => Result<TestValue>.FromValue(new TestValue(13, "Test value"))
                .ToMinimalApiResult(HttpStatusCode.Created));

        endpoints.MapGet("status-code/success/null-value",
            () => Result<TestValue?>.FromValue(null).ToMinimalApiResult(HttpStatusCode.IMUsed));

        endpoints.MapGet("status-code/success/no-response-status-code",
            () => Result<TestValue?>.FromValue(null)
                .ToMinimalApiResult(HttpStatusCode.NonAuthoritativeInformation, HttpStatusCode.ResetContent));

        endpoints.MapGet("status-code/error",
            () => Result.FromError(CreateFailureError()).ToMinimalApiResult(HttpStatusCode.Accepted));

        // Result<T> with a status code and a positional custom mapping.
        endpoints.MapGet("status-code/error/custom-mapping",
            () => Result<TestValue>.FromError(CreateFailureError())
                .ToMinimalApiResult(HttpStatusCode.Created, MapFailureToPaymentRequired));

        // Result<T> with both status codes and a positional custom mapping.
        endpoints.MapGet("status-code/error/no-response-status-code/custom-mapping",
            () => Result<TestValue>.FromError(CreateFailureError())
                .ToMinimalApiResult(HttpStatusCode.Created, HttpStatusCode.ResetContent,
                    MapFailureToPaymentRequired));
    }

    private static void MapSuccessMappingEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("success-mapping/success", () => Result.Success().ToMinimalApiResult(MapSuccess));

        endpoints.MapGet("success-mapping/success/value",
            () => Result<TestValue>.FromValue(new TestValue(13, "Test value")).ToMinimalApiResult(MapSuccessValue));

        endpoints.MapGet("success-mapping/success/null-value",
            () => Result<TestValue?>.FromValue(null).ToMinimalApiResult(MapSuccessValue));

        endpoints.MapGet("success-mapping/error",
            () => Result.FromError(CreateFailureError()).ToMinimalApiResult(MapSuccess));

        endpoints.MapGet("success-mapping/error/custom-mapping",
            () => Result.FromError(CreateFailureError())
                .ToMinimalApiResult(MapSuccess, MapFailureToPaymentRequired));
    }

    private static void MapErrorDetailsEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Both RFC 9457 extensions: the "errors" array and the top-level "detailTemplated".
        endpoints.MapGet("error/details", () =>
        {
            var error = Error.Validation(
                ErrorUri.Tag("tag:test.com,2026:validation"),
                "Validation title",
                "Validation detail.",
                ErrorUri.Locator("https://test.com/instances/validation"),
                "test.validation.failed",
                ("errorCode", (object)"TV17"));

            error.AddDetail("/age", "must be a positive integer", "test.age.mustBePositive", ("min", (object)0));

            // No pointer and no templated message, so both are omitted from the serialized detail.
            error.AddDetail(null, "must be provided");

            // A template id without parameters: AddDetail yields an empty parameter collection,
            // which the mapper normalizes away, so "params" is omitted just as it is when the
            // Error factories leave it unset.
            error.AddDetail("/name", "must not be empty", "test.name.required");

            return Result.FromError(error).ToMinimalApiResult();
        });

        // Only the top-level "detailTemplated" extension, with no individual error details.
        endpoints.MapGet("error/detail-templated", () => Result.FromError(Error.Conflict(
            ErrorUri.Tag("tag:test.com,2026:conflict"),
            "Conflict title",
            "Conflict detail.",
            null,
            "test.conflict.occurred")).ToMinimalApiResult());
    }

    private static void MapErrorCategoryEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("error/category/{category}", (ErrorCategory category) => Result.FromError(new Error
        {
            Category = category,
            TypeUri = "about:blank",
            Title = "Category title",
            Detail = "Category detail."
        }).ToMinimalApiResult());
    }

    private static Error CreateFailureError()
    {
        return Error.Failure(
            ErrorUri.Tag("tag:test.com,2026:failure"),
            "Failure title",
            "Failure detail.",
            ErrorUri.Locator("https://test.com/instances/failure"));
    }

    private static IResult MapSuccess()
    {
        return Results.Json(new TestValue(31, "Mapped success"), statusCode: StatusCodes.Status202Accepted);
    }

    private static IResult MapSuccessValue(TestValue? value)
    {
        return value is null
            ? Results.StatusCode(StatusCodes.Status205ResetContent)
            : Results.Json(new TestValue(value.Id * 2, value.Name), statusCode: StatusCodes.Status203NonAuthoritative);
    }

    private static IResult? MapFailureToPaymentRequired(Error error)
    {
        return error.Category == ErrorCategory.Failure
            ? Results.Json(new TestValue(11, error.Title), statusCode: StatusCodes.Status402PaymentRequired)
            : null;
    }

    private static IResult? MapConflictToPaymentRequired(Error error)
    {
        return error.Category == ErrorCategory.Conflict
            ? Results.Json(new TestValue(22, error.Title), statusCode: StatusCodes.Status402PaymentRequired)
            : null;
    }

    #endregion
}
