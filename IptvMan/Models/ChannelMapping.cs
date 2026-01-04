using LiteDB;

namespace IptvMan.Models;

public class ChannelMapping
{
    [BsonId]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AccountId { get; set; } = string.Empty;
    public string OriginalStreamId { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string OriginalGroupName { get; set; } = string.Empty;
    public string? CustomName { get; set; }
    public int? ChannelNumber { get; set; }
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
