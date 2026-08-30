namespace Maple.Result.Extensions.MinimalApi.Mappers;

internal static class ErrorDetailsMapper
{
    internal static ViewModels.ErrorDetail Map(ErrorDetail source)
    {
        var detailTemplated = source.DetailTemplated is null
            ? null
            : TemplatedMessageMapper.Map(source.DetailTemplated);

        return new ViewModels.ErrorDetail(source.PropertyPointer, source.Detail, detailTemplated);
    }
}
