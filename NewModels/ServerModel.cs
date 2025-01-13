using System.Text.Json.Serialization;

namespace DbUpdater;
public class ServerModel
{
    public int Id { get; set; }
    public string Ip { get; set; }
    public string Name { get; set; }
    public string? Rcon { get; set; }
    [JsonIgnore]
    public int CreatedAt { get; set; } = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    [JsonIgnore]
    public int UpdatedAt { get; set; } = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    [JsonIgnore]
    public int? DeletedAt { get; set; }
    public ServerModel(int id, string ip, string name, string rcon)
    {
        Id = id;
        Ip = ip;
        Name = name;
        Rcon = rcon;
    }
}