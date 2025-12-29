namespace IptvMan.Models;

public class UpdateCategoriesRequest
{
    public List<string> AllowedCategoryIds { get; set; } = new();
    public List<string> NotAllowedCategoryIds { get; set; } = new();
}
