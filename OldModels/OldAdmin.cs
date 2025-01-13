namespace DbUpdater;
public class OldAdmin
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SteamId { get; set; }
    public string Flags { get; set; }
    public int Immunity { get; set; }
    public int End { get; set; }
    public string GroupName { get; set; } = "";
    public int GroupId { get; set; }
    public string ServerId { get; set; }

    public  OldAdmin(int id, string name, string steamId, string flags, int immunity, int end, int groupId, string serverId) // For set Admin
    {
        Id = id;
        Name = name;
        SteamId = steamId;
        Flags = flags;
        Immunity = immunity;
        End = end;
        GroupId = groupId;
        ServerId = serverId;
    }
}