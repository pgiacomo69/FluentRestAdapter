using System.Net;
using FluentRestAdapter.Test.TestUtils;
using FluentRestAdapter.TestServer;
using FluentRestAdapter.TestServer.Dtos;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FluentRestAdapter.Test.Controllers;

public class FluentRestAdapterTest : BaseControllerTests
{
    private readonly XUnitLogger<FluentRestAdapterTest> _logger;

    public FluentRestAdapterTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper) :
        base(factory, testOutputHelper)
    {
        _logger = new XUnitLogger<FluentRestAdapterTest>(testOutputHelper, LogLevel.Debug);
    }

    
   
    
    
    [Fact]
    public async Task GetNumbersStream_ShouldReceive()
    {
        var desideredCount = 10;
        var i = 0;
        var enumerator = GetNewClient().Host("https://localhost:5000/")
            .SetEndpointPath($"/Test/NumbersStream/")
            .AddOrChangeQueryParameter("delay", "1000")
            .GetStream<TestDto>(desideredCount.ToString());

        var first = true;
        await foreach (var element in enumerator)
        {
            Assert.NotNull(element.Value);
            Assert.Equal(HttpStatusCode.OK,element.StatusCode);
            _logger.LogError("<- Got {Counter} at {ElementCount}", element.Value!.Value, i);
            Assert.Equal(i, element.Value.Value);
            i++;
            if (first)
            {
                await Task.Delay(50);
                first = false;
            }
        }

        Assert.Equal(desideredCount, i);
    }
    
    [Fact]
    public async Task GetNumbersStream_ShouldNotReceive()
    {
        var enumerator = GetNewClient().Host("https://localhost:5000/")
            .SetEndpointPath($"/WrongEndpointPath: **/")
            .AddOrChangeQueryParameter("delay", "100")
            .GetStream<TestDto>("100");
        var receivedResponses = 0;
        await foreach (var element in enumerator)
        {
             Assert.NotEqual(HttpStatusCode.OK,element.StatusCode);
             Assert.Null(element.Value);
             receivedResponses++;
        }
        Assert.Equal(1,receivedResponses);
   }
    
    [Fact]
    public async Task GetDto_ShouldReceive()
    {
        var numberToTest = 100;
        var dto = await GetNewClient().Host("https://localhost:5000/")
            .SetEndpointPath($"/Test/FinaUrlToDto/")
            .Get<TestDto>(numberToTest.ToString());
        Assert.NotNull(dto);
        Assert.NotNull(dto.Value);
        Assert.Equal(numberToTest, dto.Value!.Value);
        Assert.Equal(numberToTest.ToString(), dto.Value.ValueString);
    }
    
    [Fact]
    public async Task GetDto_ShouldNotReceive()
    {
        
        var dto = await GetNewClient().Host("https://localhost:5000/")
            .SetEndpointPath($"/Test/FinaUrlToDto/")
            .Get<TestDto>("-1");
        Assert.NotNull(dto);
        Assert.Null(dto.Value);
    }
    
    [Fact]
    public async Task GetDto_ShouldNotDeserialize()
    {
        
        var dto = await GetNewClient().Host("https://localhost:5000/")
            .SetEndpointPath($"/Test/MalformedJson")
            .Get<TestDto>("");
        Assert.NotNull(dto);
        Assert.Null(dto.Value);
    }
    
    [Fact]
    public async Task GetDto_ShouldHaveSentHeader()
    {

        var valueToTest = 10;
        var valueStringToTest = "Ten";
        var dto = await GetNewClient().Host("https://localhost:5000/")
            .SetEndpointPath($"/Test/HeadersToDto/")
            .AddOrChangeHeader("Value","123456")
            .AddOrChangeHeader("Value",valueToTest.ToString())
            .AddOrChangeHeader("ValueString","Ten")
            .Get<TestDto>("");
        Assert.NotNull(dto);
        Assert.NotNull(dto.Value);
        Assert.Equal(valueToTest, dto.Value!.Value);
        Assert.Equal(valueStringToTest, dto.Value.ValueString);
    }
}