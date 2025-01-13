
namespace DbUpdater;

public class Admin 
{
    public int Id {get; set;}
    public string SteamId {get; set;} = "";
    public string Name {get; set;}
    public string? Flags {get; set;}
    public int? Immunity {get; set;}
    public int? GroupId {get; set;} = null;
    public string? Discord {get; set;}
    public string? Vk {get; set;}
    public int Disabled {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;
    public int? EndAt {get; set;}
    /// <summary>
    /// For creating new admin
    /// </summary>
    public Admin(string steamId, string name, string? flags = null, int? immunity = null, int? groupId = null, string? discord = null, string? vk = null, int? endAt = null)
    {
        SteamId = steamId;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        GroupId = groupId;
        Discord = discord;
        Vk = vk;
        EndAt = endAt;
        CreatedAt = AdminUtils.CurrentTimestamp();
        UpdatedAt = AdminUtils.CurrentTimestamp();
    }
}