using Maple.Result.Extensions.MinimalApi.Mappers;
using System.Collections.Generic;

namespace Maple.Result.Extensions.MinimalApi.Tests.Unit.Mappers;

public class TemplatedMessageMapperTests
{
    #region consts

    private const string TemplateId = "errors.failure.detail";

    #endregion

    [Fact]
    public void Map_NullSource_ReturnsNull()
    {
        // Act
        var templatedMessage = TemplatedMessageMapper.Map(null);

        // Assert
        templatedMessage.ShouldBeNull();
    }

    [Fact]
    public void Map_SourceWithoutParams_ReturnsTemplateIdWithoutParams()
    {
        // Arrange
        var source = new TemplatedMessage(TemplateId);

        // Act
        var templatedMessage = TemplatedMessageMapper.Map(source);

        // Assert
        templatedMessage.ShouldNotBeNull();
        templatedMessage.TemplateId.ShouldBe(TemplateId);
        templatedMessage.Params.ShouldBeNull();
    }

    [Fact]
    public void Map_SourceWithParams_ReturnsTemplateIdWithParams()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 123 };
        var source = new TemplatedMessage(TemplateId, parameters);

        // Act
        var templatedMessage = TemplatedMessageMapper.Map(source);

        // Assert
        templatedMessage.ShouldNotBeNull();
        templatedMessage.TemplateId.ShouldBe(TemplateId);
        templatedMessage.Params.ShouldNotBeNull();
        templatedMessage.Params.Count.ShouldBe(2);
        templatedMessage.Params["key1"].ShouldBe("value1");
        templatedMessage.Params["key2"].ShouldBe(123);
    }
}
