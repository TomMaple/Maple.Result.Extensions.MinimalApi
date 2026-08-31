using Maple.Result.Extensions.MinimalApi.Mappers;

namespace Maple.Result.Extensions.MinimalApi.Tests.Unit.Mappers;

public class ErrorMapperTests
{
    #region consts

    private const string ErrorsKey = "errors";
    private const string DetailTemplatedKey = "detailTemplated";

    private const string TypeUri = "tag:test.com,2026:failure";
    private const string Title = "Failure title";
    private const string ErrorDetail = "Failure detail.";
    private const string InstanceUri = "https://test.com/instances/failure";
    private const string TemplateId = "errors.failure.detail";

    #endregion

    #region no extensions

    [Fact]
    public void MapExtensions_ErrorWithoutDetailsAndTemplatedDetail_ReturnsNull()
    {
        // Arrange
        var error = Error.Failure(ErrorUri.Tag(TypeUri), Title);

        // Act
        var extensions = ErrorMapper.MapExtensions(error);

        // Assert
        extensions.ShouldBeNull();
    }

    [Fact]
    public void MapExtensions_ErrorWithDetailAndInstanceOnly_ReturnsNull()
    {
        // Arrange
        var error = Error.Failure(ErrorUri.Tag(TypeUri), Title, ErrorDetail, ErrorUri.Locator(InstanceUri));

        // Act
        var extensions = ErrorMapper.MapExtensions(error);

        // Assert
        extensions.ShouldBeNull();
    }

    #endregion

    #region templated detail

    [Fact]
    public void MapExtensions_ErrorWithTemplatedDetail_ReturnsDetailTemplatedOnly()
    {
        // Arrange
        var error = Error.Failure(ErrorUri.Tag(TypeUri), Title, ErrorDetail, ErrorUri.Locator(InstanceUri),
            TemplateId, ("key1", "value1"), ("key2", 123));

        // Act
        var extensions = ErrorMapper.MapExtensions(error);

        // Assert
        extensions.ShouldNotBeNull();
        extensions.Keys.ShouldBe([DetailTemplatedKey]);

        var detailTemplated = extensions[DetailTemplatedKey].ShouldBeOfType<ViewModels.TemplatedMessage>();
        detailTemplated.TemplateId.ShouldBe(TemplateId);
        detailTemplated.Params.ShouldNotBeNull();
        detailTemplated.Params["key1"].ShouldBe("value1");
        detailTemplated.Params["key2"].ShouldBe(123);
    }

    #endregion

    #region error details

    [Fact]
    public void MapExtensions_ErrorWithErrorDetails_ReturnsErrorsOnly()
    {
        // Arrange
        var error = Error.Failure(ErrorUri.Tag(TypeUri), Title)
            .AddDetail("#/property1", "Property 1 failure detail.", "errors.failure.property1", ("pk1", "pv1"))
            .AddDetail("#/property2", "Property 2 failure detail.");

        // Act
        var extensions = ErrorMapper.MapExtensions(error);

        // Assert
        extensions.ShouldNotBeNull();
        extensions.Keys.ShouldBe([ErrorsKey]);

        var errorDetails = extensions[ErrorsKey].ShouldBeOfType<ViewModels.ErrorDetail[]>();
        errorDetails.Length.ShouldBe(2);

        var templatedErrorDetail = errorDetails[0];
        templatedErrorDetail.PropertyPointer.ShouldBe("#/property1");
        templatedErrorDetail.Detail.ShouldBe("Property 1 failure detail.");

        var detailTemplated = templatedErrorDetail.DetailTemplated;
        detailTemplated.ShouldNotBeNull();
        detailTemplated.TemplateId.ShouldBe("errors.failure.property1");
        detailTemplated.Params.ShouldNotBeNull();
        detailTemplated.Params["pk1"].ShouldBe("pv1");

        var plainErrorDetail = errorDetails[1];
        plainErrorDetail.PropertyPointer.ShouldBe("#/property2");
        plainErrorDetail.Detail.ShouldBe("Property 2 failure detail.");
        plainErrorDetail.DetailTemplated.ShouldBeNull();
    }

    #endregion

    #region error details and templated detail

    [Fact]
    public void MapExtensions_ErrorWithErrorDetailsAndTemplatedDetail_ReturnsBothEntries()
    {
        // Arrange
        var error = Error.Failure(ErrorUri.Tag(TypeUri), Title, ErrorDetail, ErrorUri.Locator(InstanceUri),
                TemplateId, ("key1", "value1"))
            .AddDetail("#/property1", "Property 1 failure detail.");

        // Act
        var extensions = ErrorMapper.MapExtensions(error);

        // Assert
        extensions.ShouldNotBeNull();
        extensions.Count.ShouldBe(2);
        extensions.ShouldContainKey(ErrorsKey);
        extensions.ShouldContainKey(DetailTemplatedKey);

        extensions[ErrorsKey].ShouldBeOfType<ViewModels.ErrorDetail[]>().Length.ShouldBe(1);
        extensions[DetailTemplatedKey].ShouldBeOfType<ViewModels.TemplatedMessage>().TemplateId.ShouldBe(TemplateId);
    }

    #endregion
}
