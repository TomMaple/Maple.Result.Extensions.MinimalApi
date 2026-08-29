using Maple.Result.Extensions.MinimalApi.Tests.Functional.TestingInfrastructure.Application;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maple.Result.Extensions.MinimalApi.Tests.Functional.TestingInfrastructure.Fixtures;

public class TestApplicationFixture : IAsyncLifetime
{
    private TestApplicationFactory _application = null!;

    internal HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _application = await TestApplicationFactory.CreateAsync();
        Client = _application.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();

        await _application.DisposeAsync();
    }
}
