using LiteDB;

namespace IptvMan.Models;

public class FilterSettings
{
    public int Id { get; set; }
    public bool AdultFilter { get; set; }
    public List<string> CategoryFilters { get; set; } = new();
}
