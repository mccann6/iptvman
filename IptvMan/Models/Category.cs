using System.Text.Json.Serialization;

namespace IptvMan.Models;

public class Category
{
    [JsonPropertyName("category_id")]
    public string? Id { get; set; }
    [JsonPropertyName("category_name")]
    public string? Name { get; set; }
    [JsonPropertyName("parent_id")]
    public int ParentId { get; set; }
}