namespace IptvMan.Services;

public interface IApiService
{
    Task<string> DoPlayerApiCall(
        string id,
        string action,
        string username,
        string password,
        string? categoryId = null,
        string? streamId = null);
    
    Task<byte[]> DoEpgApiCall(
        string id,
        string username,
        string password);
}