using LiteDB;

namespace IptvMan.Models;

public class Account
{
    [BsonId]
    public string Id { get; set; }
    public string Host { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public FilterSettings FilterSettings { get; set; } = new();

    public Account() { }

    public Account(string id, string host)
    {
        Id = id;
        Host = host;
    }
}