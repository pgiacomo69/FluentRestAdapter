namespace FluentRestAdapter;

/// <summary>
///     FluentRestClient can be used (Also in DI) as  <c>singleton</c>, it will use a unique <see cref="HttpClient" />
///     instance that should be provided to  <see cref="FluentRestClient(HttpClient)" /> constructor.
/// </summary>
public sealed class FluentRestClient

{
    private readonly HttpClient _httpClient;

    /// <summary>
    ///     FluentRestClient Constructor
    /// </summary>
    /// <remarks>
    ///     This is the main class to istantiate <see cref="FluentRestRequest">FluentRestRequests</see>
    /// </remarks>
    /// <param name="httpClient">An <see cref="HttpClient" /> instance that will be used for the requests requests</param>
    public FluentRestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    /// <summary>
    /// </summary>
    /// <param name="host">
    ///     Partial url of the endpoint to be called.
    /// </param>
    /// <returns>
    ///     <see cref="FluentRestRequest" />
    /// </returns>
    /// <example>
    ///     <code>
    ///   var request = fluentRestClient.Host("https://contoso.com");
    /// </code>
    /// </example>
    public FluentRestRequest Host(string host)
    {
        return new FluentRestRequest(_httpClient, host);
    }
}