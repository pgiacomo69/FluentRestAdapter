using FluentRestAdapter.TestServer.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace FluentRestAdapter.Test;

[Route("[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;


    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }


    [HttpGet("NumbersStream/{max}", Name = "GetNumbersStream")]
    public async IAsyncEnumerable<TestDto> GetNumbersStream(int max, [FromQuery] int delay)
    {
        for (var i = 0; i < max; i++)
        {
             await Task.Delay(delay);
            _logger.LogInformation("-> Send Number {Number}", i);
            yield return new TestDto {Value = i, ValueString = $"Thread: {Thread.CurrentThread.ManagedThreadId}"};
            
        }
    }
    
    
    [HttpGet("/Test/FinaUrlToDto/{id}", Name = "GetFinaUrlToDto")]
    public ActionResult<TestDto> FinaUrlToDto(int id)
    {
        if (id == -1) return NotFound();
        return Ok(new TestDto {Value = id, ValueString = id.ToString()});
    }
    
    [HttpGet("/Test/MalformedJson", Name = "GetMalformedJson")]
    public ActionResult<TestDto> FinaUrlToDto()
    {
        return Ok("[ This is not a valid Json]");
    }
    
    [HttpGet("/Test/HeadersToDto", Name = "GetHeadersToDto")]
    public ActionResult<TestDto> HeadersToDto([FromHeader] int value,[FromHeader] string valueString)
    {
        return new TestDto {Value = value, ValueString = valueString};
    }

    // POST: api/Test
    [HttpPost]
    public void Post([FromBody] string value)
    {
        throw new NotImplementedException();
    }

    // PUT: api/Test/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
        throw new NotImplementedException();
    }

    // DELETE: api/Test/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
        throw new NotImplementedException();
    }
}