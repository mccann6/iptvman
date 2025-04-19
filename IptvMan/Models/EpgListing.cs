using System.Text.Json.Serialization;

namespace IptvMan.Models;

public class EpgListing
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("epg_id")]
    public string? EpgId { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }
    [JsonPropertyName("start")]
    public string? Start { get; set; }
    [JsonPropertyName("end")]
    public string? End { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }
    [JsonPropertyName("start_timestamp")]
    public string? StartTimestamp { get; set; }
    [JsonPropertyName("stop_timestamp")]
    public string? StopTimestamp { get; set; }
    [JsonPropertyName("now_playing")]
    public int NowPlaying { get; set; }
    [JsonPropertyName("has_archive")]
    public int HasArchive { get; set; }
}

public class EpgListings
{
    [JsonPropertyName("epg_listings")]
    public List<EpgListing> Listings { get; set; }
}