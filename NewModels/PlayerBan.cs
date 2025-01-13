using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdater;

public class PlayerBan
{
    public int Id {get; set;}
    public string? SteamId {get; set;}
    public string? Ip {get; set;}
    public string? Name {get; set;}
    public string Reason {get; set;}
    public int Duration {get; set;}
    public sbyte BanType {get; set;} = 0;
    public int? ServerId {get; set;} = null;
    public int AdminId {get; set;}
    public int EndAt {get; set;}
    public int? UnbannedBy {get; set;}
    public string? UnbanReason {get; set;}
    public int CreatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int UpdatedAt {get; set;} = AdminUtils.CurrentTimestamp();
    public int? DeletedAt {get; set;} = null;

    public bool IsExpired => EndAt != 0 && EndAt < AdminUtils.CurrentTimestamp();
    public bool IsUnbanned => UnbannedBy != null;
    public PlayerBan(int id, long? steamId, string? ip, string? name, 
    int duration, string reason, sbyte banType, int? serverId, int adminId, 
    int? unbannedBy, string? unbanReason, int createdAt, int endAt, int 
    updatedAt, int? deletedAt)
    {
        Id = id;
        SteamId = steamId?.ToString();
        Ip = ip;
        Name = name;
        Duration = duration;
        BanType = banType;
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