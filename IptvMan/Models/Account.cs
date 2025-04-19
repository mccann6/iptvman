namespace IptvMan.Models;

public class Account(string id, string host)
{
    public string Id { get; } = id;
    public string Host { get; } = host;
}