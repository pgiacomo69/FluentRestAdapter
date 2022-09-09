<a name='assembly'></a>
# FluentRestAdapter

#### Searching for efficiency in deserializing a large JSON

## Introduction

I had to exchange a large list of object between two systems.
The solution could be to paginate responses, but I wanted to explore if there could be another simpler way.

Since paginating involves repeating the GET requests repeating the query on backend skipping in some ways previous pages, 
this still introduces latencies and unnecessary memory consumption.  

GRPC can be a modern and efficient solution to problem like these, it can work by streaming entities while getting from source, 
but this was not an option in this case, I ddidn't want to implement Grpc, set HTTP2 on server, etc, just for optimizin 1 query.

## Server side

.NET 6 introduced an [interesting change](https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/6.0/iasyncenumerable-not-buffered-by-mvc 'interesting changes') in the way tha an IAsynEnumerable is serialized in JSON.
Basically, when yielding an object it is immediately serialized and sent to the client. In this way only an object at a time is buffered in memory.

Example code:
```c#
[HttpGet("NumbersStream/{max}", Name = "GetNumbersStream")]
public async IAsyncEnumerable<TestDto> GetNumbersStream(int max, [FromQuery] int delay)
{
    for (var i = 0; i < max; i++)
    {
          await Task.Delay(delay); // simulating content retrieve
          _logger.LogInformation("-> Send Number {Number}", i);
          yield return new TestDto {Value = i, ValueString = $"Thread: {Thread.CurrentThread.ManagedThreadId}"};
     }
}
```    
Running the TestServer project and calling the endpoint from browser:  

https://localhost:5000/Test/NumbersStream/1000?delay=1000

Results can be seen "streaming" at 1 second interval. 
This test response object contains also the current thread id, with a delay>0 we'll see 
that this value sometimes changes between answers, this is because when awaiting 
for a result to produce the thread is returned to pool,
and when the awaited task ends the job can be resumed by a different thread got from pool.

   

 

## Client Side
Whe have solved server-size. However when it comes to consume that API, if we use standard methods on CLient-SIde we face the same problem, 
normally whe have to jet the whole JSON array, and then, deserialize it to a list of objects.  
We want to avoid this latency an memory consumption, rigth?
So for my particular problem I tried to working on a solution, parsing response stream while it arrives, 
buffering as little as possible raw data, yielding one object at time.  
To do so I used NewtonSoft.Json, that allows this kind of parsing.
Indeed it "waits" the input stream to "close" an object and deserialize it.
System.Text.Json from Microsoft allows also to parse a single object from a stream,
but it throws an exception if the Object it is still not "closed" and complete.

this how it works:

```
        stream = await _httpClient.GetStreamAsync("https://localhost:5000/Test/NumbersStream/1000?delay=1000");
        var serializer = new JsonSerializer();
        using (var sr = new StreamReader(stream))
        using (JsonReader reader = new JsonTextReader(sr))
        {
            while (reader.Read())
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var dto = serializer.Deserialize<TestDto>(reader);
                    if (dto != null)
                    {
                        // Here we have our single Dto, we can do something with it 
                    }
                }
        }
```

I decided to make this code of general use, and I made the this library.
The Method:

```
public async IAsyncEnumerable<FluentRestResult<TResult>> GetStream<TResult>(string finalPath)
```

Returns an IAsyncEnumerable in wich it yields single object while they are received, so they can be processed one at time.

 
## Library Description

- [FluentRestClient](#T-FluentRestAdapter-FluentRestClient 'FluentRestAdapter.FluentRestClient')
  - [#ctor(httpClient)](#M-FluentRestAdapter-FluentRestClient-#ctor-System-Net-Http-HttpClient- 'FluentRestAdapter.FluentRestClient.#ctor(System.Net.Http.HttpClient)')
  - [Host(host)](#M-FluentRestAdapter-FluentRestClient-Host-System-String- 'FluentRestAdapter.FluentRestClient.Host(System.String)')
- [FluentRestRequest](#T-FluentRestAdapter-FluentRestRequest 'FluentRestAdapter.FluentRestRequest')
  - [AddOrChangeHeader(name,value)](#M-FluentRestAdapter-FluentRestRequest-AddOrChangeHeader-System-String,System-String- 'FluentRestAdapter.FluentRestRequest.AddOrChangeHeader(System.String,System.String)')
  - [AddOrChangeQueryParameter(name,value)](#M-FluentRestAdapter-FluentRestRequest-AddOrChangeQueryParameter-System-String,System-String- 'FluentRestAdapter.FluentRestRequest.AddOrChangeQueryParameter(System.String,System.String)')
  - [Get(finalPath)](#M-FluentRestAdapter-FluentRestRequest-Get-System-String- 'FluentRestAdapter.FluentRestRequest.Get(System.String)')
  - [GetStream&lt;TResult>()](#M-FluentRestAdapter-FluentRestRequest-GetStream&lt;TResult>-System-String- 'FluentRestAdapter.FluentRestRequest.GetStream&lt;TResult>(System.String)')
  - [Get&lt;TResult>(finalPath)](#M-FluentRestAdapter-FluentRestRequest-Get&lt;TResult>-System-String- 'FluentRestAdapter.FluentRestRequest.Get&lt;TResult>(System.String)')
  - [SetBody(body)](#M-FluentRestAdapter-FluentRestRequest-SetBody-System-String- 'FluentRestAdapter.FluentRestRequest.SetBody(System.String)')
  - [SetEndpointPath(endpointPath)](#M-FluentRestAdapter-FluentRestRequest-SetEndpointPath-System-String- 'FluentRestAdapter.FluentRestRequest.SetEndpointPath(System.String)')
- [FluentRestResult](#T-FluentRestAdapter-Classes-FluentRestResult 'FluentRestAdapter.Classes.FluentRestResult')
  - [DeserializationTime](#P-FluentRestAdapter-Classes-FluentRestResult-DeserializationTime 'FluentRestAdapter.Classes.FluentRestResult.DeserializationTime')
  - [ErrorDescription](#P-FluentRestAdapter-Classes-FluentRestResult-ErrorDescription 'FluentRestAdapter.Classes.FluentRestResult.ErrorDescription')
  - [RequestTime](#P-FluentRestAdapter-Classes-FluentRestResult-RequestTime 'FluentRestAdapter.Classes.FluentRestResult.RequestTime')
  - [StatusCode](#P-FluentRestAdapter-Classes-FluentRestResult-StatusCode 'FluentRestAdapter.Classes.FluentRestResult.StatusCode')
  - [TotalTime](#P-FluentRestAdapter-Classes-FluentRestResult-TotalTime 'FluentRestAdapter.Classes.FluentRestResult.TotalTime')
- [FluentRestResult&lt;TResult>](#T-FluentRestAdapter-Classes-FluentRestResult&lt;TResult> 'FluentRestAdapter.Classes.FluentRestResult&lt;TResult>')
  - [Value](#P-FluentRestAdapter-Classes-FluentRestResult&lt;TResult>-Value 'FluentRestAdapter.Classes.FluentRestResult&lt;TResult>.Value')

<a name='T-FluentRestAdapter-FluentRestClient'></a>
## FluentRestClient `type`

##### Namespace

FluentRestAdapter

##### Summary

FluentRestClient can be used (Also in DI) as  `singleton`, it will use a unique [HttpClient](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Net.Http.HttpClient 'System.Net.Http.HttpClient')
    instance that should be provided to  [FluentRestClient.#ctor](#M-FluentRestAdapter-FluentRestClient-#ctor-System-Net-Http-HttpClient- 'FluentRestAdapter.FluentRestClient.#ctor(System.Net.Http.HttpClient)') constructor.

<a name='M-FluentRestAdapter-FluentRestClient-#ctor-System-Net-Http-HttpClient-'></a>
### #ctor(httpClient) `constructor`

##### Summary

FluentRestClient Constructor

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| httpClient | [System.Net.Http.HttpClient](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Net.Http.HttpClient 'System.Net.Http.HttpClient') | An [HttpClient](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Net.Http.HttpClient 'System.Net.Http.HttpClient') instance that will be used for the requests requests |

##### Remarks

This is the main class to istantiate [FluentRestRequest](#T-FluentRestAdapter-FluentRestRequest 'FluentRestAdapter.FluentRestRequest')

<a name='M-FluentRestAdapter-FluentRestClient-Host-System-String-'></a>
### Host(host) `method`

##### Summary



##### Returns

[FluentRestRequest](#T-FluentRestAdapter-FluentRestRequest 'FluentRestAdapter.FluentRestRequest')

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| host | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Partial url of the endpoint to be called. |

##### Example

```
  var request = fluentRestClient.Host("https://contoso.com");
```

<a name='T-FluentRestAdapter-FluentRestRequest'></a>
## FluentRestRequest `type`

##### Namespace

FluentRestAdapter

##### Summary

FluentRestRequest class allows passing requests to a Backend.

##### Example

```
var personRequest = RestClient.Host(ACMEHost)
                              .SetEndpointPath
                              .AddHeader("Authorization", "Bearer xyz");           
 var person=personRequest.get("1");
 ...... do something
 person=personRequest.get("2");
 RestClient.Endpoint(RolesEndPoint)
```

##### Remarks



<a name='M-FluentRestAdapter-FluentRestRequest-AddOrChangeHeader-System-String,System-String-'></a>
### AddOrChangeHeader(name,value) `method`

##### Summary

Adds or change the header specified by `name` parameter whith specified `value`.
    
    Headers will be sent in subsequents requests that will be made.

##### Returns

[FluentRestRequest](#T-FluentRestAdapter-FluentRestRequest 'FluentRestAdapter.FluentRestRequest') for chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Header Name |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Header Value |

##### Example

```
  fluentRestRequest.AddOrChangeHeader("Athentication","Bearer xyz");
```

<a name='M-FluentRestAdapter-FluentRestRequest-AddOrChangeQueryParameter-System-String,System-String-'></a>
### AddOrChangeQueryParameter(name,value) `method`

##### Summary

Adds or change the query parameter specified by `name` whith specified `value`.
    
    Query parameter will be appended at every subsequent request that will be made.

##### Returns

[FluentRestRequest](#T-FluentRestAdapter-FluentRestRequest 'FluentRestAdapter.FluentRestRequest') for chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Query Parameter Name |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Query Parameter Value |

##### Example

```
  var request = fluentRestRequest.AddOrChangeParameter("Page","1");
```

<a name='M-FluentRestAdapter-FluentRestRequest-Get-System-String-'></a>
### Get(finalPath) `method`

##### Summary

This Method allows retrieving of strings  from Backend.

##### Returns

[FluentRestResult&lt;TResult>](#T-FluentRestAdapter-Classes-FluentRestResult&lt;TResult> 'FluentRestAdapter.Classes.FluentRestResult&lt;TResult>')>

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| finalPath | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The last part of resource URI |

##### Remarks



<a name='M-FluentRestAdapter-FluentRestRequest-GetStream&lt;TResult>-System-String-'></a>
### GetStream&lt;TResult>() `method`

##### Summary

Warning: Experimental, do not use it in production code!

This Method allows retrieving of Objects of Type `TResult` while receiving data from Backend.

##### Returns

[IAsyncEnumerable&lt;TResult>](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IAsyncEnumerable&lt;TResult> 'System.Collections.Generic.IAsyncEnumerable&lt;TResult>')<[FluentRestResult&lt;TResult>](#T-FluentRestAdapter-Classes-FluentRestResult&lt;TResult> 'FluentRestAdapter.Classes.FluentRestResult&lt;TResult>')
    >

##### Parameters

This method has no parameters.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TResult | The type of object to retrieve in response Json array from backend. |

##### Remarks

Instead of receiving the entire JSon array in memory, and process it as a whole, it will `yield` a
        [FluentRestResult&lt;TResult>](#T-FluentRestAdapter-Classes-FluentRestResult&lt;TResult> 'FluentRestAdapter.Classes.FluentRestResult&lt;TResult>') as soon as [JsonReader](#T-Newtonsoft-Json-JsonReader 'Newtonsoft.Json.JsonReader') can
        deserialize it.

<a name='M-FluentRestAdapter-FluentRestRequest-Get&lt;TResult>-System-String-'></a>
### Get&lt;TResult>(finalPath) `method`

##### Summary

This Method allows retrieving of objects from Backend.

##### Returns

[FluentRestResult&lt;TResult>](#T-FluentRestAdapter-Classes-FluentRestResult&lt;TResult> 'FluentRestAdapter.Classes.FluentRestResult&lt;TResult>')>

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| finalPath | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The last part of resource URI |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TResult | Type of object to retrieve |

##### Remarks



<a name='M-FluentRestAdapter-FluentRestRequest-SetBody-System-String-'></a>
### SetBody(body) `method`

##### Summary

Adds or change the Body to be appended at subsequents requests

##### Returns

[FluentRestRequest](#T-FluentRestAdapter-FluentRestRequest 'FluentRestAdapter.FluentRestRequest') for chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| body | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Body Content |

##### Example

```
  fluentRestRequest.SetBody("{firstName:\"John\",lastName:\"Doe\"}");
```

<a name='M-FluentRestAdapter-FluentRestRequest-SetEndpointPath-System-String-'></a>
### SetEndpointPath(endpointPath) `method`

##### Summary

Set the endpoint path for requests

##### Returns

[FluentRestRequest](#T-FluentRestAdapter-FluentRestRequest 'FluentRestAdapter.FluentRestRequest') for chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| endpointPath | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | the final endpoint path for the request |

##### Example

```
  fluentRestRequest.SetEndpointPath("/api/v1/people");
```

##### Remarks

Trailing slashes, if present, will be removed. If a leading slash is not present, it will be inserted;

<a name='T-FluentRestAdapter-Classes-FluentRestResult'></a>
## FluentRestResult `type`

##### Namespace

FluentRestAdapter.Classes

##### Summary

The result of a Rest Call

##### Remarks

It encapsulates Status Code, Status Description, and durations of call and deserialize phases.

<a name='P-FluentRestAdapter-Classes-FluentRestResult-DeserializationTime'></a>
### DeserializationTime `property`

##### Summary

Duration of Deserialization phase

When using [GetStream&lt;TResult>](#M-FluentRestAdapter-FluentRestRequest-GetStream&lt;TResult>-System-String- 'FluentRestAdapter.FluentRestRequest.GetStream&lt;TResult>(System.String)') this time
includes also client-side processing time for every object received.

<a name='P-FluentRestAdapter-Classes-FluentRestResult-ErrorDescription'></a>
### ErrorDescription `property`

##### Summary

Exception Message

<a name='P-FluentRestAdapter-Classes-FluentRestResult-RequestTime'></a>
### RequestTime `property`

##### Summary

Duration of remote call

<a name='P-FluentRestAdapter-Classes-FluentRestResult-StatusCode'></a>
### StatusCode `property`

##### Summary

HTTP Status Code, 0 in case of  exception in code.

<a name='P-FluentRestAdapter-Classes-FluentRestResult-TotalTime'></a>
### TotalTime `property`

##### Summary

Total duration.

<a name='T-FluentRestAdapter-Classes-FluentRestResult&lt;TResult>'></a>
## FluentRestResult&lt;TResult> `type`

##### Namespace

FluentRestAdapter.Classes

##### Summary

The result of a Rest Call

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TResult | The type of object to retrieve in response Json from backend it can be an array alse. |

##### Remarks

It encapsulates Object(s) received, Status Code, Status Description, and durations of call and deserialize phases.

<a name='P-FluentRestAdapter-Classes-FluentRestResult&lt;TResult>-Value'></a>
### Value `property`

##### Summary

Value of object received
