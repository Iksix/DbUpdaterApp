using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdater;

public class PlayerComm
{
    public enum MuteTypes
    {
        Mute = 0,
        Gag = 1,
        Silence = 2
    }
    public int Id {get; set;}
    public string SteamId {get; set;}
    public string? Ip {get; set;}
    public string? Name {get; set;}
    public int MuteType {get; set;}
    public string Reason {get; set;}
    public int Duration {get; set;}
    public int? ServerId {get; set;} = null;
    public int AdminId {get; set;}
    public int EndAt {get; set;}
    public int? UnbannedBy {get; set;}
    public string? UnbanReason {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;
    
    public PlayerComm(int id, long steamId, string? ip, string? name, int muteType, int duration, string reason, int? serverId, int adminId, int? unbannedBy, string? unbanReason, int createdAt, int endAt, int updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId.ToString();
        Ip = ip;
        Name = name;
        MuteType = muteType;
        Duration = duration;
        Reason = reason; 
        ServerId = serverId;
        AdminId = adminId;
        UnbannedBy = unbannedBy;
        CreatedAt = createdAt;
        EndAt = endAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
        UnbanReason = unbanReason;
    }
    public void SetEndAt()
    {
        EndAt = Duration == 0 ? 0 : AdminUtils.CurrentTimestamp() + Duration;
    }
}