namespace FluentRestAdapter.Classes;

/// <summary>
///     The result of a Rest Call
/// </summary>
/// <remarks>
///     It encapsulates Status Code, Status Description, and durations of call and deserialize phases.
/// </remarks>
public class FluentRestResult
{   
    /// <summary>
    /// Duration of remote call 
    /// </summary>
    public TimeSpan RequestTime { get; internal set; }

    /// <summary>
    /// Duration of Deserialization phase
    /// <br/>
    /// When using <see cref="FluentRestRequest.GetStream&lt;TResult&gt;"/> this time
    /// includes also client-side processing time for every object received. 
    /// </summary>
    public TimeSpan DeserializationTime { get; internal set; }
    /// <summary>
    /// Total duration. 
    /// </summary>
    public TimeSpan TotalTime => RequestTime + DeserializationTime;
    /// <summary>
    /// HTTP Status Code, 0 in case of  exception in code. 
    /// </summary>
    public HttpStatusCode StatusCode { get; internal set; }
    /// <summary>
    /// Exception Message 
    /// </summary>
    public string ErrorDescription { get; internal set; } = "";
}

/// <summary>
///     The result of a Rest Call
/// </summary>
/// <remarks>
///     It encapsulates Object(s) received, Status Code, Status Description, and durations of call and deserialize phases.
/// </remarks>
/// <typeparam name="TResult">The type of object to retrieve in response Json from backend it can be an array alse.</typeparam>
public class FluentRestResult<TResult> : FluentRestResult
{

    /// <summary>
    /// Sequential number, used with GetStream
    /// </summary>
    public int Seq { get; internal set; } = 0;
    
    /// <summary>
    /// Value of object received
    /// </summary>
    public TResult? Value { get; internal set; }

    internal FluentRestResult<TNewresult> ToType<TNewresult>()
    {
        var newResult = new FluentRestResult<TNewresult>();
        newResult.StatusCode = StatusCode;
        newResult.ErrorDescription = ErrorDescription;
        newResult.RequestTime = RequestTime;
        newResult.DeserializationTime = DeserializationTime;
        return newResult;
    }
    
    internal FluentRestResult<TResult> CloneToNext()
    {
        var newResult = new FluentRestResult<TResult>();
        newResult.StatusCode = StatusCode;
        newResult.ErrorDescription = ErrorDescription;
        newResult.RequestTime = RequestTime;
        newResult.DeserializationTime = DeserializationTime;
        newResult.Seq = Seq + 1;
        return newResult;
    }
}