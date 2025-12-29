namespace IptvMan.Models;

public class CategoryRefreshResult
{
    public List<Category> NewCategories { get; set; } = new();
    public bool HasChanges => NewCategories.Count > 0;
}
