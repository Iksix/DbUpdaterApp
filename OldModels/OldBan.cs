namespace DbUpdater;
public class OldBan
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Sid { get; set; }
    public string? Ip { get; set; }
    public string AdminSid { get; set; }
    public string AdminName { get; set; }
    public int Created { get; set; }
    public int Time { get; set; }
    public int End { get; set; }
    public string Reason { get; set; }
    public int BanType { get; set; }
    public int Unbanned { get; set; }
    public string? UnbannedBy { get; set; }
    public string ServerId { get; set; }

    public OldBan(
        string name, string sid, string? ip, 
        string adminSid, string adminName, int created, int time, int end, 
        string reason, string serverId, int banType = 0, int unbanned = 0, string? unbannedBy = null, int id = 1)
    {
        Name = name;
        Sid = sid;
        Ip = ip;
        AdminSid = adminSid;
        AdminName = adminName;
        Created = created;
        Time = time;
        End = end;
        Reason = reason;
        ServerId = serverId;
        BanType = banType;
        Unbanned = unbanned;
        UnbannedBy = unbannedBy;
        Id = id;
    }
}