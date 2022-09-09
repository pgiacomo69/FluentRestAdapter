using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FluentRestAdapter.Test.TestUtils;

public class XUnitLogger : ILogger, IDisposable
{
    private readonly LogLevel _logLevel;

    private readonly string _name = "";


    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitLogger(string name, ITestOutputHelper testOutputHelper, LogLevel logLevel)
    {
        _logLevel = logLevel;
        _testOutputHelper = testOutputHelper;
        _name = name;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var stateStr = state != null ? state.ToString() : "";
        if (logLevel >= _logLevel)
            _testOutputHelper.WriteLine($"{_name}: {stateStr}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }
}

public class XUnitLogger<T> : ILogger<T>, IDisposable where T : class
{
    private readonly LogLevel _logLevel;

    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitLogger(ITestOutputHelper testOutputHelper, LogLevel logLevel)
    {
        _logLevel = logLevel;
        _testOutputHelper = testOutputHelper;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var stateStr = state != null ? state.ToString() : "";
        if (logLevel >= _logLevel)
            _testOutputHelper.WriteLine($"{typeof(T)}: {stateStr}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return this;
    }
}