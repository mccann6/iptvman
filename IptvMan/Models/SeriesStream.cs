using System.Text.Json.Serialization;

namespace IptvMan.Models;

public class SeriesStream
{
    [JsonPropertyName("num")]
    public int Num { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("series_id")]
    public int SeriesId { get; set; }
    [JsonPropertyName("cover")]
    public string? Cover { get; set; }
    [JsonPropertyName("plot")]
    public string? Plot { get; set; }
    [JsonPropertyName("cast")]
    public string? Cast { get; set; }
    [JsonPropertyName("director")]
    public string? Director { get; set; }
    [JsonPropertyName("genre")]
    public string? Genre { get; set; }
    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }
    [JsonPropertyName("last_modified")]
    public string? LastModified { get; set; }
    [JsonPropertyName("rating")]
    public string? Rating { get; set; }
    [JsonPropertyName("rating_5based")]
    public dynamic? Rating5Based { get; set; }
    [JsonPropertyName("backdrop_path")]
    public dynamic? BackdropPath { get; set; }
    [JsonPropertyName("youtube_trailer")]
    public string? YoutubeTrailer { get; set; }
    [JsonPropertyName("episode_run_time")]
    public string? EpisodeRunTime { get; set; }
    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }
}
