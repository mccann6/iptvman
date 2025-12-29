using LiteDB;

namespace IptvMan.Models;

public class FilterSettings
{
    public int Id { get; set; }
    public bool AdultFilter { get; set; }
    public List<string> AllowedLiveCategoryIds { get; set; } = new();
    public List<string> NotAllowedLiveCategoryIds { get; set; } = new();
    public List<string> AllowedVodCategoryIds { get; set; } = new();
    public List<string> NotAllowedVodCategoryIds { get; set; } = new();
    public List<string> AllowedSeriesCategoryIds { get; set; } = new();
    public List<string> NotAllowedSeriesCategoryIds { get; set; } = new();
}
