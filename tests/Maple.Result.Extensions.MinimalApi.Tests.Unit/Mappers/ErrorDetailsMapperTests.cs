using Maple.Result.Extensions.MinimalApi.Mappers;
using System.Collections.Generic;

namespace Maple.Result.Extensions.MinimalApi.Tests.Unit.Mappers;

public class ErrorDetailsMapperTests
{
    #region consts

    private const string PropertyPointer = "#/property1";
    private const string Detail = "Property 1 failure detail.";
    private const string TemplateId = "errors.failure.property1";

    #endregion

    [Fact]
    public void Map_SourceWithoutTemplatedDetail_ReturnsPointerAndDetailOnly()
    {
        // Arrange
        var source = new ErrorDetail(PropertyPointer, Detail);

        // Act
        var errorDetail = ErrorDetailsMapper.Map(source);

        // Assert
        errorDetail.PropertyPointer.ShouldBe(PropertyPointer);
        errorDetail.Detail.ShouldBe(Detail);
        errorDetail.DetailTemplated.ShouldBeNull();
    }

    [Fact]
    public void Map_SourceWithTemplatedDetail_ReturnsMappedTemplatedDetail()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["pk1"] = "pv1" };
        var source = new ErrorDetail(PropertyPointer, Detail, new TemplatedMessage(TemplateId, parameters));

        // Act
        var errorDetail = ErrorDetailsMapper.Map(source);

        // Assert
        errorDetail.PropertyPointer.ShouldBe(PropertyPointer);
        errorDetail.Detail.ShouldBe(Detail);
        errorDetail.DetailTemplated.ShouldNotBeNull();
        errorDetail.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        errorDetail.DetailTemplated.Params.ShouldNotBeNull();
        errorDetail.DetailTemplated.Params["pk1"].ShouldBe("pv1");
    }

    [Fact]
    public void Map_SourceWithoutPropertyPointer_ReturnsNullPropertyPointer()
    {
        // Arrange
        var source = new ErrorDetail(null, Detail);

        // Act
        var errorDetail = ErrorDetailsMapper.Map(source);

        // Assert
        errorDetail.PropertyPointer.ShouldBeNull();
        errorDetail.Detail.ShouldBe(Detail);
    }
}
