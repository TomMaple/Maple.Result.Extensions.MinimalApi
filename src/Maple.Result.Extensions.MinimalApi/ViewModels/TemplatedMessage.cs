using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Maple.Result.Extensions.MinimalApi.ViewModels;

/// <summary>
///     Represents a templated message with a template identifier and optional parameters.
/// </summary>
/// <remarks>
///     The purpose of this structure is to provide a way to generate client-side localized messages.
/// </remarks>
public sealed record TemplatedMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TemplatedMessage" /> class with the specified template identifier and
    ///     optional parameters.
    /// </summary>
    /// <param name="templateId">The identifier of the message template.</param>
    /// <param name="parameters">
    ///     The optional collection of parameters (names and values) that might be required
    ///     to generate a message from the specific template.
    /// </param>
    public TemplatedMessage(string templateId, IReadOnlyDictionary<string, object>? parameters = null)
    {
        TemplateId = templateId;
        Params = parameters;
    }

    /// <summary>
    ///     Gets the identifier of the message template.
    /// </summary>
    [JsonPropertyName("templateId")]
    public string TemplateId { get; init; }

    /// <summary>
    ///     Gets the optional collection of parameters (names and values) that might be required
    ///     to generate a message from the specific template.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("params")]
    public IReadOnlyDictionary<string, object>? Params { get; init; }
}
