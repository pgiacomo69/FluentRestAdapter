using System.Diagnostics;
using Newtonsoft.Json;

namespace FluentRestAdapter;

/// <summary>
///     FluentRestRequest class allows passing requests to a Backend.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             This class can't be instantiated from outside <see cref="FluentRestAdapter" /> library.
///             To create a <see cref="FluentRestRequest" /> instance, use
///             <see cref="FluentRestClient.Host">FluentRestClient.Host()</see> method.
///         </item>
///         <item>
///             Using Fluent methods, the final Endpoint can be modified, and Query Parameters and Headers can be added.
///             The generated instance can used more than once.
///         </item>
///         <item>
///             Fluent methods have the side effect of modifying the request state, but this is clearly stated in their
///             signature.
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// var personRequest = RestClient.Host(ACMEHost)
///                               .SetEndpointPath
///                               .AddHeader("Authorization", "Bearer xyz");           
///  var person=personRequest.get("1");
///  ...... do something
///  person=personRequest.get("2");
///  RestClient.Endpoint(RolesEndPoint)
/// </code>
/// </example>
public sealed class FluentRestRequest
{
    private readonly List<KeyValue> _headers = new();
    private readonly string _hostName;
    private readonly HttpClient _httpClient;
    private readonly List<KeyValue> _parameters = new();
    private string _body = "";
    private string _endpointPath = "";

    internal FluentRestRequest(HttpClient httpClient, string hostName)
    {
        _httpClient = httpClient;
        while (hostName.EndsWith("/"))
        {
            hostName=hostName.Remove(hostName.Length - 1);
        }
        _hostName = hostName;
    }

    private string EndPoint => $"{_hostName}{_endpointPath}";

    private string CompleteUri(string finalPath)
    {
        var result = EndPoint;
        if (finalPath != "") result = $"{result}/{finalPath}";

        if (_parameters.Count > 0)
        {
            var pars = _parameters.Select(p => $"{p.Key}={p.Value}").ToArray();
            var queryString = string.Join("&", pars);
            result = $"{result}?{queryString}";
        }

        return result;
    }

    /// <summary>
    ///     Set the endpoint path for requests
    /// </summary>
    /// <remarks>
    ///     Trailing slashes, if present, will be removed. If a leading slash is not present, it will be inserted;
    /// </remarks>
    /// <param name="endpointPath">the final endpoint path for the request</param>
    /// <returns><see cref="FluentRestRequest" /> for chaining.</returns>
    /// <example>
    ///     <code>
    ///   fluentRestRequest.SetEndpointPath("/api/v1/people");
    /// </code>
    /// </example>
    public FluentRestRequest SetEndpointPath(string endpointPath)
    {
        while (endpointPath.EndsWith("/"))
        {
            endpointPath=endpointPath.Remove(endpointPath.Length - 1);
        }

        if (endpointPath != "" && !endpointPath.StartsWith("/")) endpointPath = $"/{endpointPath}";
        _endpointPath = endpointPath;

        return this;
    }

    /// <summary>
    ///     Adds or change the header specified by <c>name</c> parameter whith specified <c>value</c>.
    ///     <br />
    ///     Headers will be sent in subsequents requests that will be made.
    /// </summary>
    /// <param name="name">Header Name</param>
    /// <param name="value">Header Value</param>
    /// <returns><see cref="FluentRestRequest" /> for chaining.</returns>
    /// <example>
    ///     <code>
    ///   fluentRestRequest.AddOrChangeHeader("Athentication","Bearer xyz");
    /// </code>
    /// </example>
    public FluentRestRequest AddOrChangeHeader(string name, string value)
    {
        var header = _headers.FirstOrDefault(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (header == null)
        {
            header = new KeyValue(name, value);
            _headers.Add(header);
        }
        else
        {
            header.Value = value;
        }

        return this;
    }

    /// <summary>
    ///     Adds or change the query parameter specified by <c>name</c> whith specified <c>value</c>.
    ///     <br />
    ///     Query parameter will be appended at every subsequent request that will be made.
    /// </summary>
    /// <param name="name">Query Parameter Name</param>
    /// <param name="value">Query Parameter Value</param>
    /// <returns><see cref="FluentRestRequest" /> for chaining.</returns>
    /// <example>
    ///     <code>
    ///   var request = fluentRestRequest.AddOrChangeParameter("Page","1");
    /// </code>
    /// </example>
    public FluentRestRequest AddOrChangeQueryParameter(string name, string value)
    {
        var queryParameter = _parameters.FirstOrDefault(h => h.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (queryParameter == null)
        {
            queryParameter = new KeyValue(name, value);
            _parameters.Add(queryParameter);
        }
        else
        {
            queryParameter.Value = value;
        }

        return this;
    }

    /// <summary>
    ///     Adds or change the Body to be appended at subsequents requests
    /// </summary>
    /// <param name="body">Body Content</param>
    /// <returns><see cref="FluentRestRequest" /> for chaining.</returns>
    /// <example>
    ///     <code>
    ///   fluentRestRequest.SetBody("{firstName:\"John\",lastName:\"Doe\"}");
    /// </code>
    /// </example>
    public FluentRestRequest SetBody(string body)
    {
        _body = body;
        return this;
    }

    /// <summary>
    ///     <para>
    ///         Warning: Experimental, do not use it in production code!
    ///     </para>
    ///     <para>
    ///         This Method allows retrieving of Objects of Type <c>TResult</c> while receiving data from Backend.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Instead of receiving the entire JSon array in memory, and process it as a whole, it will <c>yield</c> a
    ///         <see cref="FluentRestResult&lt;TResult&gt;" /> as soon as <see cref="Newtonsoft.Json.JsonReader" /> can
    ///         deserialize it.
    ///     </para>
    ///     <list type="bullet">
    ///         <listheader>
    ///             <description>Notes:</description>
    ///         </listheader>
    ///         <item>
    ///             <description>
    ///                 This method uses <see cref="System.Net.Http.HttpClient.GetStreamAsync(string)" />, so at
    ///                 present it is not possible to add headers or parameters.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Newtonsoft.Json is used instead of System.Text.Json because its
    ///                 <see cref="Newtonsoft.Json.JsonSerializer.Deserialize&lt;T&gt;(JsonReader)" />method can wait for
    ///                 receiving a complete object from stream.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Exceptions are managed only in calling the Backend, in such case a
    ///                 <see cref="FluentRestResult&lt;TResult&gt;" /> will returned, with no object included but with
    ///                 <see cref="HttpStatusCode" /> received.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>When cicling while deserializing objects from stream, exceptions are not managed.</description>
    ///         </item>
    ///         <item>
    ///             <description>Currently it works only on arrays containing classes, not discrete types.</description>
    ///         </item>
    ///         <item>
    ///             <description>Empty answer from backend was not tested.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <returns>
    ///     <see cref="IAsyncEnumerable&lt;T&gt;">IAsyncEnumerable</see>&lt;<see cref="FluentRestResult&lt;TResult&gt;" />
    ///     &gt;
    /// </returns>
    /// <typeparam name="TResult">The type of object to retrieve in response Json array from backend.</typeparam>
    public async IAsyncEnumerable<FluentRestResult<TResult>> GetStream<TResult>(string finalPath) where TResult : class
    {
        var result = new FluentRestResult<TResult>();
        var timer = new Stopwatch();
        Stream? stream = null;
        timer.Start();
        try
        {
            stream = await _httpClient.GetStreamAsync(CompleteUri(finalPath));
            result.StatusCode = HttpStatusCode.OK;
            result.RequestTime = timer.Elapsed;
            timer.Restart();
        }
        catch (HttpRequestException e)
        {
            result.RequestTime = timer.Elapsed;
            result.StatusCode = (HttpStatusCode) e.StatusCode!;
            result.ErrorDescription = e.Message;
            if (stream!=null) await stream.DisposeAsync();
            timer.Stop();
        }

        if (stream == null)
        {
            yield return result;
            yield break;
        }


        var serializer = new JsonSerializer();
        int i = 0;
        await using (stream)
        {
            using (var sr = new StreamReader(stream)) {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    bool errorInStream = false;
                    Exception deserializingException=null;
                    try
                    {
                        errorInStream = true;
                        while (reader.Read())
                            if (reader.TokenType == JsonToken.StartObject)
                            {
                                TResult? o=null;
                                try
                                {
                                    o = serializer.Deserialize<TResult>(reader);
                                }
                                catch (Exception e)
                                {
                                    deserializingException = e;
                                    break;
                                }
                                if (o != null)
                                {
                                    result = result.CloneToNext();
                                    result.DeserializationTime = timer.Elapsed;
                                    result.Value = o;
                                    i++;
                                    Console.WriteLine($"Received Object {result.Seq}, Time: {result.DeserializationTime.TotalMilliseconds} msec");
                                    yield return result;
                                }
                            }

                        errorInStream = false;
                    }
                    finally
                    {
                        if (errorInStream)
                        {
                            result = result.CloneToNext();
                            result.RequestTime = timer.Elapsed;
                            result.StatusCode = 0;
                            if (deserializingException != null)
                            {
                                result.ErrorDescription = deserializingException.Message;
                            }
                            else
                            {
                                result.ErrorDescription = "Error Receiving data";
                            }
                        }
                    }
                }
            }
        }

        timer.Stop();
    }


    private HttpRequestMessage PrepareRequest(HttpMethod method, string finalPath)
    {
        var request = new HttpRequestMessage(method, CompleteUri(finalPath));
        foreach (var header in _headers) request.Headers.Add(header.Key, header.Value);
        request.Content = new StringContent(_body);
        return request;
    }

    /// <summary>
    ///     This Method allows retrieving of strings  from Backend.
    /// </summary>
    /// <returns><see cref="FluentRestResult&lt;TResult&gt;">FluentRestResult&lt;string&gt;</see>&gt;</returns>
    /// <param name="finalPath">The last part of resource URI</param>
    public async Task<FluentRestResult<string>> Get(string finalPath)
    {
        var result = new FluentRestResult<string>();
        var timer = new Stopwatch();
        timer.Start();
        var request = PrepareRequest(HttpMethod.Get, finalPath);
        var response = await _httpClient.SendAsync(request);
        result.StatusCode = response.StatusCode;
        result.Value = await response.Content.ReadAsStringAsync();
        result.RequestTime = timer.Elapsed;
        timer.Stop();
        return result;
    }

    /// <summary>
    ///     This Method allows retrieving of objects from Backend.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <listheader>
    ///             <description>Notes:</description>
    ///         </listheader>
    ///         <item>
    ///             <description>
    ///                 Exceptions are managed, in such cases a <see cref="FluentRestResult&lt;TResult&gt;" /> will
    ///                 returned, with <see cref="HttpStatusCode" /> received.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <returns><see cref="FluentRestResult&lt;TResult&gt;"></see>&gt;</returns>
    /// <param name="finalPath">The last part of resource URI</param>
    /// <typeparam name="TResult">Type of object to retrieve</typeparam>
    public async Task<FluentRestResult<TResult>> Get<TResult>(string finalPath)
    {
        var stringResult = await Get(finalPath);
        var result = stringResult.ToType<TResult>();
        if (result.StatusCode == HttpStatusCode.OK)
        {
            var timer = new Stopwatch();
            try
            {
                timer.Start();
                result.Value = JsonConvert.DeserializeObject<TResult>(stringResult.Value!);
                timer.Stop();
                result.DeserializationTime = timer.Elapsed;
            }
            catch (Exception e)
            {
                result.StatusCode = 0;
                result.ErrorDescription = e.Message;
            }
        }

        return result;
    }


    private sealed  class KeyValue
    {
        public KeyValue(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Value { get; internal set; }
    }
}