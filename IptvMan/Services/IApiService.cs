namespace IptvMan.Services;

public interface IApiService
{
    Task<string> DoPlayerApiCall(
        string id,
        string? action,
        string? username,
        string? password,
        string? categoryId,
        string? streamId,
        string? vodId,
        string? seriesId);
    
    Task<byte[]> DoEpgApiCall(
        string id,
        string? username,
        string? password);
    
    Task<byte[]> DoM3uApiCall(
        string id,
        string? username,
        string? password,
        string output,
        string type);

    string GetStreamUrl(
        string id,
        string type,
        string? username,
        string? password,
        string stream);
}
