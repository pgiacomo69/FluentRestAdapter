using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FluentRestAdapter.Test.TestUtils;

/// <summary>
///     A class representing an <see cref="ILoggerProvider" /> to use with xunit.
/// </summary>
[ProviderAlias("XUnit")]
public class XUnitLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _logLevel;

    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitLoggerProvider(ITestOutputHelper testOutputHelper, LogLevel logLevel)
    {
        _testOutputHelper = testOutputHelper;
        _logLevel = logLevel;
    }

    public virtual ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(categoryName, _testOutputHelper, _logLevel);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~XUnitLoggerProvider()
    {
        Dispose(false);
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true" /> to release both managed and unmanaged resources;
    ///     <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        // Nothing to dispose of
    }
}