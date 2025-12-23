# RedisService quick start

This project ships with a small `RedisService` (`backend/Helper/RedisService.cs`) that wraps StackExchange.Redis for simple string/object caching with optional key prefixing.

## 1) Run Redis locally

If you do not already have a Redis instance, you can start one quickly via Docker:

```bash
docker run --name fs-redis -p 6379:6379 -d redis:7-alpine
```

## 2) Configure connection details

Edit `backend/appsettings.json` (or `appsettings.Development.json`) and set the `Redis` section:

```json
"Redis": {
  "ConnectionString": "localhost:6379,abortConnect=false",
  "Database": 0,
  "KeyPrefix": "file-storage"
}
```

- `ConnectionString` follows StackExchange.Redis syntax and can include password, SSL, etc.
- `Database` is optional; omit it to use the connection string default (usually DB 0).
- `KeyPrefix` namespaces all keys, which helps avoid collisions across apps/environments.

The service and `IConnectionMultiplexer` are registered as singletons in `Startup.cs`, so nothing else is required for dependency injection.

## 3) Use the service in a controller or service

Inject `RedisService` anywhere DI is available, then call the helpers:

```csharp
using Backend.Helper;

public class SampleController : ControllerBase
{
    private readonly RedisService _redis;

    public SampleController(RedisService redis)
    {
        _redis = redis;
    }

    [HttpGet("cache-demo")]
    public async Task<IActionResult> CacheDemo()
    {
        await _redis.SetStringAsync("demo:key", "hello", TimeSpan.FromMinutes(5));

        var value = await _redis.GetStringAsync("demo:key");              // "hello"
        var exists = await _redis.ExistsAsync("demo:key");                // true

        await _redis.SetObjectAsync("demo:obj", new { Name = "File" });   // serialized to JSON
        var obj = await _redis.GetObjectAsync<dynamic>("demo:obj");       // deserialized back

        await _redis.RemoveAsync("demo:key");                             // delete when done

        return Ok(new { value, exists, obj });
    }
}
```

That is all you need—once Redis is running and the connection string is set, you can cache strings or JSON-serialized objects with a few lines of code.
