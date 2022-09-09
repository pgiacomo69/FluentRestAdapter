using FluentRestAdapter.TestServer;
using Xunit.Abstractions;

namespace FluentRestAdapter.Test.Controllers;

public class BaseControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;


    protected ITestOutputHelper _testOutputHelper;


    public BaseControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
    }

    public FluentRestClient GetNewClient()
    {
        var newClient = _factory.WithWebHostBuilder(builder =>
        {
            _factory.CustomConfigureServices(builder, _testOutputHelper);
        }).CreateClient();

        return new FluentRestClient(newClient);
    }
}