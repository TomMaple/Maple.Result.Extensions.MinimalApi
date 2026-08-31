using Maple.Result.Extensions.MinimalApi.Tests.Unit.TestingInfrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System;

namespace Maple.Result.Extensions.MinimalApi.Tests.Unit;

// The aliases are declared inside the namespace so that they take precedence over the Maple.Result types
// of the same name.
using IResult = Microsoft.AspNetCore.Http.IResult;
using ErrorDetail = MinimalApi.ViewModels.ErrorDetail;
using TemplatedMessage = MinimalApi.ViewModels.TemplatedMessage;

public class MinimalApiResultExtensionsTests
{
    #region consts

    private const string FailureTypeUri = "tag:test.com,2026:failure";
    private const string FailureInstanceUri = "https://test.com/instances/failure";

    #endregion

    #region (Result) default mapping

    [Fact]
    public void ToMinimalApiResult_SuccessfulResult_ReturnsNoContent()
    {
        // Arrange
        var sut = Result.Success();

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        var noContent = result.ShouldBeOfType<NoContent>();
        noContent.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void ToMinimalApiResult_Error_ReturnsProblemDetailsMappedFromTheError()
    {
        // Arrange
        var sut = Result.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        problem.ProblemDetails.Status.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        problem.ProblemDetails.Type.ShouldBe(FailureTypeUri);
        problem.ProblemDetails.Title.ShouldBe("Failure title");
        problem.ProblemDetails.Detail.ShouldBe("Failure detail.");
        problem.ProblemDetails.Instance.ShouldBe(FailureInstanceUri);
        problem.ProblemDetails.Extensions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ErrorCategory.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorCategory.Unauthenticated, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorCategory.Unauthorized, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorCategory.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorCategory.Timeout, StatusCodes.Status408RequestTimeout)]
    [InlineData(ErrorCategory.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorCategory.Failure, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(ErrorCategory.Critical, StatusCodes.Status500InternalServerError)]
    [InlineData(ErrorCategory.NotImplemented, StatusCodes.Status501NotImplemented)]
    [InlineData(ErrorCategory.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    public void ToMinimalApiResult_ErrorOfTheGivenCategory_ReturnsExpectedStatusCode(
        ErrorCategory category, int expectedStatusCode)
    {
        // Arrange
        var sut = Result.FromError(CreateError(category));

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(expectedStatusCode);
        problem.ProblemDetails.Status.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public void ToMinimalApiResult_ErrorWithTemplatedDetail_AddsTheDetailTemplatedExtension()
    {
        // Arrange
        var error = Error.Failure(
            ErrorUri.Tag(FailureTypeUri),
            "Failure title",
            "Failure detail.",
            ErrorUri.Locator(FailureInstanceUri),
            "errors.failure.detail",
            ("key1", "value1"), ("key2", 123));

        var sut = Result.FromError(error);

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.ProblemDetails.Extensions.ShouldNotContainKey("errors");

        var detailTemplated = problem.ProblemDetails.Extensions["detailTemplated"].ShouldBeOfType<TemplatedMessage>();
        detailTemplated.TemplateId.ShouldBe("errors.failure.detail");
        detailTemplated.Params.ShouldNotBeNull();
        detailTemplated.Params["key1"].ShouldBe("value1");
        detailTemplated.Params["key2"].ShouldBe(123);
    }

    [Fact]
    public void ToMinimalApiResult_ErrorWithDetails_AddsTheErrorsExtension()
    {
        // Arrange
        var error = Error.Validation(
                ErrorUri.Tag("tag:test.com,2026:validation"),
                "Validation title",
                "Validation detail.",
                ErrorUri.Locator("https://test.com/instances/validation"))
            .AddDetail("#/property1", "Property 1 failure detail.", "errors.failure.property1", ("pk1", "pv1"))
            .AddDetail("#/property2", "Property 2 failure detail.");

        var sut = Result.FromError(error);

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.ProblemDetails.Extensions.ShouldNotContainKey("detailTemplated");

        var errors = problem.ProblemDetails.Extensions["errors"].ShouldBeOfType<ErrorDetail[]>();
        errors.Length.ShouldBe(2);

        var firstError = errors[0];
        firstError.PropertyPointer.ShouldBe("#/property1");
        firstError.Detail.ShouldBe("Property 1 failure detail.");
        firstError.DetailTemplated.ShouldNotBeNull();
        firstError.DetailTemplated.TemplateId.ShouldBe("errors.failure.property1");
        firstError.DetailTemplated.Params.ShouldNotBeNull();
        firstError.DetailTemplated.Params["pk1"].ShouldBe("pv1");

        var secondError = errors[1];
        secondError.PropertyPointer.ShouldBe("#/property2");
        secondError.Detail.ShouldBe("Property 2 failure detail.");
        secondError.DetailTemplated.ShouldBeNull();
    }

    #endregion

    #region (Result) custom error mapping

    [Fact]
    public void ToMinimalApiResult_ErrorMatchingTheCustomMapping_ReturnsCustomMappedResult()
    {
        // Arrange
        var sut = Result.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(MapFailureToPaymentRequired);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status402PaymentRequired);
        json.Value.ShouldBe(new TestValue(11, "Failure title"));
    }

    [Fact]
    public void ToMinimalApiResult_ErrorNotMatchingTheCustomMapping_ReturnsDefaultProblemDetails()
    {
        // Arrange
        var sut = Result.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(MapConflictToPaymentRequired);

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithCustomMapping_DoesNotInvokeTheCustomMapping()
    {
        // Arrange
        var sut = Result.Success();
        var invoked = false;

        // Act
        var result = sut.ToMinimalApiResult(_ =>
        {
            invoked = true;
            return null;
        });

        // Assert
        result.ShouldBeOfType<NoContent>();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public void ToMinimalApiResult_ErrorWithCustomMapping_PassesTheErrorToTheCustomMapping()
    {
        // Arrange
        var error = CreateFailureError();
        var sut = Result.FromError(error);
        Error? mapped = null;

        // Act
        sut.ToMinimalApiResult(e =>
        {
            mapped = e;
            return null;
        });

        // Assert
        mapped.ShouldBeSameAs(error);
    }

    #endregion

    #region (Result) success status code

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithSuccessStatusCode_ReturnsTheGivenStatusCode()
    {
        // Arrange
        var sut = Result.Success();

        // Act
        var result = sut.ToMinimalApiResult(StatusCodes.Status202Accepted);

        // Assert
        var statusCode = result.ShouldBeOfType<StatusCodeHttpResult>();
        statusCode.StatusCode.ShouldBe(StatusCodes.Status202Accepted);
    }

    [Fact]
    public void ToMinimalApiResult_ErrorWithSuccessStatusCode_ReturnsProblemDetails()
    {
        // Arrange
        var sut = Result.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(StatusCodes.Status202Accepted);

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public void ToMinimalApiResult_ErrorWithSuccessStatusCodeAndCustomMapping_ReturnsCustomMappedResult()
    {
        // Arrange
        var sut = Result.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(StatusCodes.Status202Accepted, MapFailureToPaymentRequired);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status402PaymentRequired);
    }

    #endregion

    #region (Result) success mapping

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithSuccessMapping_ReturnsMappedResult()
    {
        // Arrange
        var sut = Result.Success();

        // Act
        var result = sut.ToMinimalApiResult(MapSuccess);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status202Accepted);
        json.Value.ShouldBe(new TestValue(31, "Mapped success"));
    }

    [Fact]
    public void ToMinimalApiResult_ErrorWithSuccessMapping_DoesNotInvokeTheSuccessMapping()
    {
        // Arrange
        var sut = Result.FromError(CreateFailureError());
        var invoked = false;

        // Act
        var result = sut.ToMinimalApiResult(() =>
        {
            invoked = true;
            return Results.Ok();
        });

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public void ToMinimalApiResult_ErrorWithSuccessMappingAndCustomMapping_ReturnsCustomMappedResult()
    {
        // Arrange
        var sut = Result.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(MapSuccess, MapFailureToPaymentRequired);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status402PaymentRequired);
    }

    #endregion

    #region (Result<T>) default mapping

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithValue_ReturnsOkWithTheValue()
    {
        // Arrange
        var value = new TestValue(13, "Test value");
        var sut = Result<TestValue>.FromValue(value);

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        var ok = result.ShouldBeOfType<Ok<TestValue>>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        ok.Value.ShouldBeSameAs(value);
    }

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithNullValue_ReturnsNoContent()
    {
        // Arrange
        var sut = Result<TestValue?>.FromValue(null);

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        result.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithError_ReturnsProblemDetails()
    {
        // Arrange
        var sut = Result<TestValue>.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult();

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithErrorMatchingTheCustomMapping_ReturnsCustomMappedResult()
    {
        // Arrange
        var sut = Result<TestValue>.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(MapFailureToPaymentRequired);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status402PaymentRequired);
    }

    #endregion

    #region (Result<T>) success status code

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithValueAndSuccessStatusCode_ReturnsTheGivenStatusCodeWithTheValue()
    {
        // Arrange
        var value = new TestValue(13, "Test value");
        var sut = Result<TestValue>.FromValue(value);

        // Act
        var result = sut.ToMinimalApiResult(StatusCodes.Status201Created);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status201Created);
        json.Value.ShouldBeSameAs(value);
    }

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithNullValueAndNoSuccessNoResponseStatusCode_ReturnsTheSuccessStatusCode()
    {
        // Arrange
        var sut = Result<TestValue?>.FromValue(null);

        // Act
        var result = sut.ToMinimalApiResult(StatusCodes.Status226IMUsed);

        // Assert
        var statusCode = result.ShouldBeOfType<StatusCodeHttpResult>();
        statusCode.StatusCode.ShouldBe(StatusCodes.Status226IMUsed);
    }

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithNullValueAndSuccessNoResponseStatusCode_ReturnsTheSuccessNoResponseStatusCode()
    {
        // Arrange
        var sut = Result<TestValue?>.FromValue(null);

        // Act
        var result = sut.ToMinimalApiResult(
            StatusCodes.Status203NonAuthoritative, StatusCodes.Status205ResetContent);

        // Assert
        var statusCode = result.ShouldBeOfType<StatusCodeHttpResult>();
        statusCode.StatusCode.ShouldBe(StatusCodes.Status205ResetContent);
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithErrorAndSuccessStatusCode_ReturnsProblemDetails()
    {
        // Arrange
        var sut = Result<TestValue>.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(StatusCodes.Status201Created);

        // Assert
        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithErrorAndBothStatusCodesAndCustomMapping_ReturnsCustomMappedResult()
    {
        // Arrange
        var sut = Result<TestValue>.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(
            StatusCodes.Status201Created, StatusCodes.Status205ResetContent, MapFailureToPaymentRequired);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status402PaymentRequired);
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithErrorAndSuccessStatusCodeAndCustomMapping_ReturnsCustomMappedResult()
    {
        // Arrange
        var sut = Result<TestValue>.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(StatusCodes.Status201Created, MapFailureToPaymentRequired);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status402PaymentRequired);
    }

    #endregion

    #region (Result<T>) success mapping

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithValueAndSuccessMapping_ReturnsMappedValue()
    {
        // Arrange
        var sut = Result<TestValue>.FromValue(new TestValue(13, "Test value"));

        // Act
        var result = sut.ToMinimalApiResult(MapSuccessValue);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status203NonAuthoritative);
        json.Value.ShouldBe(new TestValue(26, "Test value"));
    }

    [Fact]
    public void ToMinimalApiResult_SuccessfulResultWithNullValueAndSuccessMapping_InvokesTheSuccessMappingWithNull()
    {
        // Arrange
        var sut = Result<TestValue?>.FromValue(null);

        // Act
        var result = sut.ToMinimalApiResult(MapSuccessValue);

        // Assert
        var statusCode = result.ShouldBeOfType<StatusCodeHttpResult>();
        statusCode.StatusCode.ShouldBe(StatusCodes.Status205ResetContent);
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithErrorAndSuccessMapping_DoesNotInvokeTheSuccessMapping()
    {
        // Arrange
        var sut = Result<TestValue>.FromError(CreateFailureError());
        var invoked = false;

        // Act
        // The lambda parameter is typed explicitly so that the success mapping overload is selected
        // instead of the one taking the custom error mapping.
        var result = sut.ToMinimalApiResult((TestValue _) =>
        {
            invoked = true;
            return Results.Ok();
        });

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithErrorAndSuccessMappingAndCustomMapping_ReturnsCustomMappedResult()
    {
        // Arrange
        var sut = Result<TestValue>.FromError(CreateFailureError());

        // Act
        var result = sut.ToMinimalApiResult(MapSuccessValue, MapFailureToPaymentRequired);

        // Assert
        var json = result.ShouldBeOfType<JsonHttpResult<TestValue>>();
        json.StatusCode.ShouldBe(StatusCodes.Status402PaymentRequired);
    }

    #endregion

    #region guard clauses

    [Fact]
    public void ToMinimalApiResult_NullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result)null!).ToMinimalApiResult();

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToMinimalApiResult_NullResultWithSuccessStatusCode_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result)null!).ToMinimalApiResult(StatusCodes.Status202Accepted);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToMinimalApiResult_NullResultWithSuccessMapping_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result)null!).ToMinimalApiResult(MapSuccess);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToMinimalApiResult_NullSuccessMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = Result.Success();

        // Act
        var act = () => sut.ToMinimalApiResult((Func<IResult>)null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("successMapping");
    }

    [Fact]
    public void ToMinimalApiResult_NullResultAndNullSuccessMapping_ThrowsArgumentNullExceptionForTheResult()
    {
        // Act
        var act = () => ((Result)null!).ToMinimalApiResult((Func<IResult>)null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToMinimalApiResult_NullGenericResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<TestValue>)null!).ToMinimalApiResult();

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToMinimalApiResult_NullGenericResultWithSuccessStatusCode_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<TestValue>)null!).ToMinimalApiResult(StatusCodes.Status201Created);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToMinimalApiResult_NullGenericResultWithSuccessMapping_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ((Result<TestValue>)null!).ToMinimalApiResult(MapSuccessValue);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    [Fact]
    public void ToMinimalApiResult_GenericResultWithNullSuccessMapping_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = Result<TestValue>.FromValue(new TestValue(13, "Test value"));

        // Act
        var act = () => sut.ToMinimalApiResult((Func<TestValue, IResult>)null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("successMapping");
    }

    [Fact]
    public void ToMinimalApiResult_NullGenericResultAndNullSuccessMapping_ThrowsArgumentNullExceptionForTheResult()
    {
        // Act
        var act = () => ((Result<TestValue>)null!).ToMinimalApiResult((Func<TestValue, IResult>)null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("result");
    }

    #endregion

    #region helper methods

    private static Error CreateFailureError()
    {
        return Error.Failure(
            ErrorUri.Tag(FailureTypeUri),
            "Failure title",
            "Failure detail.",
            ErrorUri.Locator(FailureInstanceUri));
    }

    private static Error CreateError(ErrorCategory category)
    {
        var typeUri = ErrorUri.Tag($"tag:test.com,2026:{category}".ToLowerInvariant());
        var title = $"{category} title";

        return category switch
        {
            ErrorCategory.Validation => Error.Validation(typeUri, title),
            ErrorCategory.Unauthenticated => Error.Unauthenticated(typeUri, title),
            ErrorCategory.Unauthorized => Error.Unauthorized(typeUri, title),
            ErrorCategory.NotFound => Error.NotFound(typeUri, title),
            ErrorCategory.Timeout => Error.Timeout(typeUri, title),
            ErrorCategory.Conflict => Error.Conflict(typeUri, title),
            ErrorCategory.Failure => Error.Failure(typeUri, title),
            ErrorCategory.Critical => Error.Critical(typeUri, title),
            ErrorCategory.NotImplemented => Error.NotImplemented(typeUri, title),
            ErrorCategory.Unavailable => Error.Unavailable(typeUri, title),
            _ => throw new NotSupportedException($"Unsupported ErrorCategory: {category}")
        };
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
