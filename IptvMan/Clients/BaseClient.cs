using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
 
namespace IptvMan.Clients;

public class BaseClient(HttpClient httpClient, IMemoryCache memoryCache)
{
    private const int CacheExpirationMinutes = 60;

    private Task<string> DoHttpCall(string baseUri, string resource)
    {
        return httpClient.GetStringAsync($"{baseUri}{resource}");
    }
    
    protected async Task<T> DoCachedHttpCall<T>(string baseUri, string resource)
    {
        if (memoryCache.TryGetValue(resource, out T response))
        {
            if(response != null)
                return response;
        }
        var apiResponse = await DoHttpCall(baseUri, resource);

        response = JsonSerializer.Deserialize<T>(apiResponse);
        memoryCache.Set(resource, response, TimeSpan.FromMinutes(CacheExpirationMinutes));
        return response;
    }
    
    protected Task<byte[]> DoHttpGetBytes(string requestUrl)
    {
        return httpClient.GetByteArrayAsync($"{requestUrl}");
    }
    
    protected async Task<string> DoCachedHttpCall(string baseUri, string resource)
    {
        if (memoryCache.TryGetValue(resource, out string response))
        {
            if(response != null)
                return response;
        }
        var apiResponse = await DoHttpCall(baseUri, resource);
        
        memoryCache.Set(resource, apiResponse, TimeSpan.FromMinutes(CacheExpirationMinutes));
        return apiResponse;
    }
}