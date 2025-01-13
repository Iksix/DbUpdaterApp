namespace DbUpdater;
public class OldComm
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Sid { get; set; }
    public string AdminSid { get; set; }
    public string AdminName { get; set; }
    public int Created { get; set; }
    public int Time { get; set; }
    public int End { get; set; }
    public string Reason { get; set; }
    public int Unbanned { get; set; }
    public string? UnbannedBy { get; set; }
    public string ServerId { get; set; }

    public OldComm(
        string name, string sid, string adminSid, string adminName,
        int created, int time, int end, 
        string reason, string serverId, int unbanned = 0, string? unbannedBy = null, int id = 0)
    {
        Name = name;
        Sid = sid;
        AdminSid = adminSid;
        AdminName = adminName;
        Created = created;
        Time = time;
        End = end;
        Reason = reason;
        ServerId = serverId;
        Unbanned = unbanned;
        UnbannedBy = unbannedBy;
        Id = id;
    }
}