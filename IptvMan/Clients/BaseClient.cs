using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
 
namespace IptvMan.Clients;

public class BaseClient(HttpClient httpClient, IMemoryCache memoryCache)
{
    private readonly int _cacheExpirationMinutes = Configuration.CacheTime;

    private Task<string> DoHttpCall(string baseUri, string resource)
    {
        return httpClient.GetStringAsync($"{baseUri}{resource}");
    }
    
    private static byte[] CompressString(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
        {
            input.CopyTo(gzip);
        }
        return output.ToArray();
    }
    
    private static string DecompressString(byte[] compressedBytes)
    {
        using var input = new MemoryStream(compressedBytes);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            gzip.CopyTo(output);
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }
    
    protected async Task<T> DoCachedHttpCall<T>(string baseUri, string resource)
    {
        // Use full URL as cache key to prevent collisions between different hosts using same credentials
        var cacheKey = $"{baseUri}{resource}";

        if (memoryCache.TryGetValue<byte[]>(cacheKey, out var compressedJson) && compressedJson != null)
        {
            var jsonString = DecompressString(compressedJson);
            return JsonSerializer.Deserialize<T>(jsonString)!;
        }
        
        var apiResponse = await DoHttpCall(baseUri, resource);
        
        var compressed = CompressString(apiResponse);
        memoryCache.Set(cacheKey, compressed, TimeSpan.FromMinutes(_cacheExpirationMinutes));
        
        return JsonSerializer.Deserialize<T>(apiResponse)!;
    }
    
    protected Task<byte[]> DoHttpGetBytes(string requestUrl)
    {
        return httpClient.GetByteArrayAsync($"{requestUrl}");
    }
}