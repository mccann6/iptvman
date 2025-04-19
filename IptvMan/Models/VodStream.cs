using System.Text.Json.Serialization;

namespace IptvMan.Models;

public class VodStream
{
    [JsonPropertyName("num")]
    public int Num { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("stream_type")]
    public string? StreamType { get; set; }
    [JsonPropertyName("stream_id")]
    public int StreamId { get; set; }
    [JsonPropertyName("stream_icon")]
    public string? StreamIcon { get; set; }
    [JsonPropertyName("rating")]
    public string? Rating { get; set; }
    [JsonPropertyName("rating_5based")]
    public double Rating5Based { get; set; }
    [JsonPropertyName("added")]
    public string? Added { get; set; }
    [JsonPropertyName("is_adult")]
    public string? IsAdult { get; set; }
    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }
    [JsonPropertyName("container_extension")]
    public string? ContainerExtension { get; set; }
    [JsonPropertyName("custom_sid")]
    public string? CustomSid { get; set; }
    [JsonPropertyName("direct_source")]
    public string? DirectSource { get; set; }
}