using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Dapper;
using MySqlConnector;

namespace DbUpdater;

public class Config
{
    public  string Host { get; set; } = "host";
    public  string Database { get; set; } = "Database";
    public  string User { get; set; } = "User";
    public  string Password { get; set; } = "Password";
    public  string Port { get; set; } = "3306";
    // Сопоставление ServerID со старых на новые численные
    public  Dictionary<string, ServerModel> CompareServerIds {get; set;} = new ()
    {
        {"A", new ServerModel(1, "127.0.0.1:27015", "MIRAGE #1", "12345")}
    };
    // IP которые будут автоматически заменятся на null - нужно для зеркалов
    public  List<string> IpBlackList {get; set;} = new() {"0.0.0.0"};

}

public static class Program
{
    public static string DBConnString = "";

    public static Config Config = null!;
    static List<Admin> newAdmins = new();
    static List<AdminToServer> adminToServers = new();

    public static void ReadOrCreateConfig()
    {
        var filePath = $"./config.json";
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(new Config(), options: new JsonSerializerOptions() { WriteIndented = true, AllowTrailingCommas = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.Cyrillic), ReadCommentHandling = JsonCommentHandling.Skip }));
        }
        using var streamReader = new StreamReader(filePath);
        var json = streamReader.ReadToEnd();
        var config = JsonSerializer.Deserialize<Config>(json, options: new JsonSerializerOptions() { WriteIndented = true, AllowTrailingCommas = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.Cyrillic), ReadCommentHandling = JsonCommentHandling.Skip });
        Config = config!;
    }

    private static void Main()
    {
        try
        {
            ReadOrCreateConfig();
            OnConfigParsed();
            Task.Run(async () => {
                await Update();
            });
            Console.ReadKey();
        }
        catch (System.Exception e)
        {
            Console.WriteLine(e.Message);
            Console.ReadKey();
            
            throw;
        }
        
    }

    public  static  void OnConfigParsed()
    {
        var builder = new MySqlConnectionStringBuilder();
        builder.Password = Config.Password;
        builder.Server = Config.Host;
        builder.Database = Config.Database;
        builder.UserID = Config.User;
        builder.Port = uint.Parse(Config.Port);
        builder.ConnectionTimeout = 99999;
        DBConnString = builder.ConnectionString;
    }
    public  static  int? ServerId(string key) 
    {
        if (!Config.CompareServerIds.ContainsKey(key))
        {
            return null;
        }
        return key == "" ? null : Config.CompareServerIds[key].Id;
    }
    public  static int AdminId(string steamId, int? serverId)
    {
        if (steamId.ToLower() == "console")
        {
            return 1;
        }
        if (serverId == null)
        {
            return 1;
        }
        var admins = newAdmins.Where(x => x.SteamId == steamId).ToList();
        foreach (var admin in admins)
        {
            if (adminToServers.Any(x => x.AdminId == admin.Id && x.ServerId == serverId))
            {
                return admin.Id;
            }
        }
        return 1;
    }
    public  static async Task Update()
    {
        try
        {
        Console.WriteLine("Start updating");
        Console.WriteLine(DBConnString);
        await using var conn = new MySqlConnection(DBConnString);
        await conn.OpenAsync();
        Console.WriteLine("Rename old tables...");
        // await conn.QueryAsync(@"
        // rename table old_iks_admins to iks_admins;
        // rename table old_iks_gags to iks_gags;
        // rename table old_iks_mutes to iks_mutes;
        // rename table old_iks_groups to iks_groups;
        // rename table old_iks_bans to iks_bans;
        // ");
        await conn.QueryAsync(@"
        rename table iks_admins to old_iks_admins;
        rename table iks_gags to old_iks_gags;
        rename table iks_mutes to old_iks_mutes;
        rename table iks_groups to old_iks_groups;
        rename table iks_bans to old_iks_bans;
        ");
        Console.WriteLine("Init new tables...");
        await Init();
        Console.WriteLine("Add serevers to base");
        foreach (var server in Config.CompareServerIds)
        {
            await CreateServer(server.Value);
        }
        Console.WriteLine("Getting old admins");
        var oldAdmins = await GetOldAdmins();
        Console.WriteLine("Calculate admins id");
        foreach (var admin in oldAdmins)
        {
            admin.Id++;
        }
        if (oldAdmins.Any(x => x.Id == 1))
        {
            foreach (var admin in oldAdmins)
            {
                admin.Id++;
            }
        }
        Console.WriteLine("Getting old bans");
        var oldBans = await GetOldBans();
        Console.WriteLine("Getting old mutes");
        var oldMutes = await GetOldMutes();
        Console.WriteLine("Getting old gags");
        var oldGags = await GetOldGags();
        Console.WriteLine("Getting old groups");
        var oldGroups = await GetOldGroups();
        Console.WriteLine("Calculate new groups");
        List<Group> newGroups = new();
        foreach (var oldGroup in oldGroups)
        {
            newGroups.Add(new (oldGroup.Id, oldGroup.Name, oldGroup.Flags, oldGroup.Immunity, null));
        }
        newAdmins = new();
        adminToServers = new();
        // NEW ADMINS SET
        var biggerId = 2;
        Console.WriteLine("Calculate new admins");
        foreach (var oldAdmin in oldAdmins)
        {
            var newAdmin = new Admin(
                oldAdmin.SteamId,
                oldAdmin.Name,
                oldAdmin.Flags == "" ? null : oldAdmin.Flags,
                oldAdmin.Immunity == -1 ? null : oldAdmin.Immunity
                );
            newAdmins.Add(newAdmin);
            newAdmin.EndAt = oldAdmin.End;
            newAdmin.Id = oldAdmin.Id;
            if (newAdmin.Id > biggerId)
            {
                biggerId = newAdmin.Id+1;
            }
            newAdmin.GroupId = oldAdmin.GroupId == -1 ? null : oldAdmin.GroupId;
            if (oldAdmin.ServerId == "")
            {
                foreach (var s in Config.CompareServerIds)
                {
                    adminToServers.Add(new AdminToServer(newAdmin.Id, s.Value.Id));
                }
                continue;
            }
            var serverIds = oldAdmin.ServerId.Split(";");
            foreach (var oldSid in serverIds)
            {
                if (Config.CompareServerIds.TryGetValue(oldSid, out var s))
                {
                    adminToServers.Add(new AdminToServer(newAdmin.Id, s.Id));
                }
            }
        }
        // ========
        List<PlayerBan> newBans = new List<PlayerBan>();
        Console.WriteLine("Calculate new bans");
        foreach (var oldBan in oldBans)
        {
            if (oldBan.AdminSid.ToLower() != "console" && newAdmins.All(x => x.SteamId != oldBan.AdminSid))
            {
                var deletedAdmin = new Admin(oldBan.AdminSid, "DELETED", null, null, null, null, null, null);
                deletedAdmin.DeletedAt = AdminUtils.CurrentTimestamp();
                deletedAdmin.Id = biggerId++;
                newAdmins.Add(deletedAdmin);
            }
            int adminId = AdminId(oldBan.AdminSid, ServerId(oldBan.ServerId));
            int? unbannedBy = oldBan.UnbannedBy == null ? null : AdminId(oldBan.UnbannedBy, ServerId(oldBan.ServerId));
            var ip = oldBan.Ip.ToLower().Trim() is "undefined" or "" ? null : oldBan.Ip.Split(":")[0];
            var ban = new PlayerBan(
                oldBan.Id,
                oldBan.Sid.ToLower() == "undefined" ? null : long.TryParse(oldBan.Sid, out var steamID) ? steamID : null,
                Config.IpBlackList.Contains(ip!) ? null : ip,
                oldBan.Name,
                oldBan.Time,
                oldBan.Reason,
                (sbyte)oldBan.BanType,
                oldBan.ServerId == "" ? null : ServerId(oldBan.ServerId),
                adminId,
                unbannedBy,
                unbannedBy == null ? null : "NOT SETTED BY DbUpdater",
                oldBan.Created,
                oldBan.End,
                oldBan.Created,
                null
            );
            newBans.Add(ban);
        }
        // NEW COMMS SET
        List<PlayerComm> newComms = new List<PlayerComm>();
        Console.WriteLine("Calculate new comms");
        foreach (var oldGag in oldGags)
        {
            if (oldGag.AdminSid.ToLower() != "console" && newAdmins.All(x => x.SteamId != oldGag.AdminSid))
            {
                var deletedAdmin = new Admin(oldGag.AdminSid, "DELETED", null, null, null, null, null, null);
                deletedAdmin.DeletedAt = AdminUtils.CurrentTimestamp();
                deletedAdmin.Id = biggerId++;
                newAdmins.Add(deletedAdmin);
            }
            int adminId = AdminId(oldGag.AdminSid, ServerId(oldGag.ServerId));
            int? unbannedBy = oldGag.UnbannedBy == null ? null : AdminId(oldGag.UnbannedBy, ServerId(oldGag.ServerId));
            if (!long.TryParse(oldGag.Sid, out var steamID))
            {
                continue;
            }
            var ban = new PlayerComm(
                oldGag.Id,
                steamID,
                null,
                oldGag.Name,
                1,
                oldGag.Time,
                oldGag.Reason,
                oldGag.ServerId == "" ? null : ServerId(oldGag.ServerId),
                adminId,
                unbannedBy,
                unbannedBy == null ? null : "NOT SETTED BY DbUpdater",
                oldGag.Created,
                oldGag.End,
                oldGag.Created,
                null
            );
            newComms.Add(ban);
        }
        foreach (var oldMute in oldMutes)
        {
            if (oldMute.AdminSid.ToLower() != "console" && newAdmins.All(x => x.SteamId != oldMute.AdminSid))
            {
                var deletedAdmin = new Admin(oldMute.AdminSid, "DELETED", null, null, null, null, null, null);
                deletedAdmin.DeletedAt = AdminUtils.CurrentTimestamp();
                deletedAdmin.Id = biggerId++;
                newAdmins.Add(deletedAdmin);
            }
            int adminId = AdminId(oldMute.AdminSid, ServerId(oldMute.ServerId));
            int? unbannedBy = oldMute.UnbannedBy == null ? null : AdminId(oldMute.UnbannedBy, ServerId(oldMute.ServerId));
            if (!long.TryParse(oldMute.Sid, out var steamID))
            {
                continue;
            }
            var ban = new PlayerComm(
                oldMute.Id,
                steamID,
                null,
                oldMute.Name,
                0,
                oldMute.Time,
                oldMute.Reason,
                oldMute.ServerId == "" ? null : ServerId(oldMute.ServerId),
                adminId,
                unbannedBy,
                unbannedBy == null ? null : "NOT SETTED BY DbUpdater",
                oldMute.Created,
                oldMute.End,
                oldMute.Created,
                null
            );
            newComms.Add(ban);
        }
        Console.WriteLine("Sort new comms");
        var sortedComms = newComms.OrderBy(si => si.CreatedAt).ToList();
        var counter = 0;
        foreach (var comm in sortedComms)
        {
            comm.Id = counter;
            counter++;
        }

        var importSqlString = "";
        // Добавление всего этого в базу данных
        Console.WriteLine("Construct groups sql");
        foreach (var group in newGroups)
        {
            var commandDefinition = new CommandDefinition(@"
            insert into iks_groups
            (id, name, flags, immunity, comment)
            values
            (@id, @name, @flags, @immunity, null);
            ");
            var sql = commandDefinition.CommandText;
            importSqlString +=  GetSqlWithParameters(sql, new {
                id = group.Id,
                name = group.Name,
                flags = group.Flags,
                immunity = group.Immunity
            }) + "\n";
        }
        Console.WriteLine("Construct adminsSql sql");
        Console.WriteLine("Admins count: " + newAdmins.Count);
        foreach (var admin in newAdmins)
        {
            var commandDefinition = new CommandDefinition(@"
            insert into iks_admins
            (id, steam_id, name, flags, immunity, group_id, discord, vk, end_at, created_at, updated_at, deleted_at)
            values
            (@id, @steamId, @name, @flags, @immunity, @groupId, null, null, @endAt, unix_timestamp(), unix_timestamp(), @deleted_at);
            ");
            importSqlString += GetSqlWithParameters(commandDefinition.CommandText, new {
                id = admin.Id,
                steamId = admin.SteamId,
                name = admin.Name,
                flags = admin.Flags,
                immunity = admin.Immunity,
                groupId = admin.GroupId,
                endAt = admin.EndAt,
                deleted_at = admin.DeletedAt,
            }) + "\n";
        }
        Console.WriteLine("Construct AdminToServer SQL");
        foreach (var ats in adminToServers)
        {
            importSqlString += GetSqlWithParameters(@"
            insert into iks_admin_to_server(admin_id, server_id)
            values
            (@adminId, @serverId)
            ", new {adminId = ats.AdminId, serverId = ats.ServerId}) + "\n";
        }

        Console.WriteLine("BansSql construct...");
        foreach (var ban in newBans)
        {
            ban.SetEndAt();
            importSqlString += GetSqlWithParameters(@"insert into iks_bans
            (id, steam_id, ip, name, duration, reason, ban_type, server_id, admin_id, unbanned_by, unban_reason, created_at, end_at, updated_at, deleted_at)
            values
            (@id, @steamId, @ip, @name, @duration, @reason, @banType, @serverId, @adminId, @unbannedBy, @unbanReason, @createdAt, @endAt, @updatedAt, @deletedAt);", new {
                    id = ban.Id,
                    steamId = ban.SteamId,
                    ip = ban.Ip,
                    name = ban.Name,
                    duration = ban.Duration,
                    reason = ban.Reason,
                    banType = ban.BanType,
                    serverId = ban.ServerId,
                    adminId = ban.AdminId,
                    unbannedBy = ban.UnbannedBy,
                    unbanReason = ban.UnbanReason,
                    createdAt = ban.CreatedAt,
                    endAt = ban.EndAt,
                    updatedAt = ban.UpdatedAt,
                    deletedAt = ban.DeletedAt,
                });
        }
        Console.WriteLine("CommsSql construct...");
        foreach (var comm in sortedComms)
        {
            comm.SetEndAt();
            importSqlString += GetSqlWithParameters(@"
            insert into iks_comms
            (id, steam_id, ip, name, mute_type, duration, reason, server_id, admin_id, unbanned_by, unban_reason, created_at, end_at, updated_at, deleted_at)
            values
            (@id, @steamId, @ip, @name, @muteType, @duration, @reason, @serverId, @adminId, @unbannedBy, @unbanReason, @createdAt, @endAt, @updatedAt, @deletedAt);
            ", new {
                id = comm.Id,
                steamId = comm.SteamId,
                ip = comm.Ip,
                name = comm.Name,
                duration = comm.Duration,
                reason = comm.Reason,
                muteType = comm.MuteType,
                serverId = comm.ServerId,
                adminId = comm.AdminId,
                unbannedBy = comm.UnbannedBy,
                unbanReason = comm.UnbanReason,
                createdAt = comm.CreatedAt,
                endAt = comm.EndAt,
                updatedAt = comm.UpdatedAt,
                deletedAt = comm.DeletedAt,
            }) + "\n";
        }
        Console.WriteLine("Save into file...");
        File.WriteAllText("./import.sql", importSqlString);
        Console.WriteLine("READY!");
        }
        catch (MySqlException e)
        {
            throw new Exception(e.ToString());
        }
    }

    private static async Task CreateServer(ServerModel server)
    {
        try
        {
            await using var conn = new MySqlConnection(DBConnString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
                insert into iks_servers(id, ip, name, rcon, created_at, updated_at)
                values(@serverId, @ip, @name, @rcon, unix_timestamp(), unix_timestamp())
            ", new {
                serverId = server.Id,
                ip = server.Ip,
                name = server.Name,
                rcon = server.Rcon
            });
        }
        catch (MySqlException e)
        {
            throw new Exception(e.Message);
        }
    }

    public  static async Task<List<OldAdmin>> GetOldAdmins()
    {
        var admins = new List<OldAdmin>();
        try
        {
            await using var conn = new MySqlConnection(DBConnString);
            await conn.OpenAsync();
            admins = (await conn.QueryAsync<OldAdmin>(@"
            select 
            id as id,
            name as name,
            sid as steamId,
            flags as flags,
            immunity as immunity,
            end as end,
            group_id as groupId,
            server_id as serverId
            from old_iks_admins
            ")).ToList();
        }
        catch (MySqlException e)
        {
            throw new Exception(e.Message);
        }

        return admins;
    }
    public  static async Task<List<OldGroup>> GetOldGroups()
    {
        var groups = new List<OldGroup>();
        try
        {
            await using var conn = new MySqlConnection(DBConnString);
            await conn.OpenAsync();
            groups = (await conn.QueryAsync<OldGroup>(@"
            select 
            name as name,
            flags as flags,
            immunity as immunity,
            id as id
            from old_iks_groups
            ")).ToList();
        }
        catch (MySqlException e)
        {
            throw new Exception(e.Message);
        }

        return groups;
    }

    public  static async Task<List<OldBan>> GetOldBans()
    {
        try
        {
            await using var conn = new MySqlConnection(DBConnString);
            await conn.OpenAsync();
            var bans = await conn.QueryAsync<OldBan>(@"
            select 
            name as name,
            sid as sid,
            ip as ip,
            adminsid as adminSid,
            adminName as adminName,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            BanType as banType,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from old_iks_bans
            ", new {timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
            
            return bans.ToList();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    public  static async Task<List<OldComm>> GetOldMutes()
    {
        try
        {
            await using var conn = new MySqlConnection(DBConnString);
            await conn.OpenAsync();
            var mutes = await conn.QueryAsync<OldComm>(@"
            select 
            name as name,
            sid as sid,
            adminsid as adminSid,
            adminName as adminName,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from old_iks_mutes
            ", new {timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
            
            return mutes.ToList();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    public  static async Task<List<OldComm>> GetOldGags()
    {
        try
        {
            await using var conn = new MySqlConnection(DBConnString);
            await conn.OpenAsync();
            var gags = await conn.QueryAsync<OldComm>(@"
            select 
            name as name,
            sid as sid,
            adminsid as adminSid,
            adminName as adminName,
            created as created,
            time as time,
            end as end,
            reason as reason,
            server_id as serverId,
            Unbanned as unbanned,
            UnbannedBy as unbannedBy,
            id as id
            from old_iks_gags
            ", new {timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
            
            return gags.ToList();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public  static  async Task Init()
    {
        try
        {
            using var conn = new MySqlConnection(DBConnString);
            await conn.OpenAsync();
            await conn.QueryAsync(@"
    create table if not exists iks_servers(
        id int not null unique,
        ip varchar(32) not null comment 'ip:port',
        name varchar(64) not null,
        rcon varchar(128) default null,
        created_at int not null,
        updated_at int not null,
        deleted_at int default null
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
    create table if not exists iks_groups(
        id int not null auto_increment primary key,
        name varchar(64) not null unique,
        flags varchar(32) not null,
        immunity int not null,
        comment varchar(255) default null
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
    create table if not exists iks_admins(
        id int not null auto_increment primary key,
        steam_id varchar(17) not null,
        name varchar(64) not null,
        flags varchar(32) default null,
        immunity int default null,
        group_id int default null,
        discord varchar(64) default null,
        vk varchar(64) default null,
        is_disabled int(1) not null default 0,
        end_at int null,
        created_at int not null,
        updated_at int not null,
        deleted_at int default null,
        foreign key (group_id) references iks_groups(id)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;

    insert into iks_admins(steam_id, name, flags, immunity, created_at, updated_at)
    select 'CONSOLE', 'CONSOLE', null, 0, unix_timestamp(), unix_timestamp();

    create table if not exists iks_admin_to_server(
        id int not null auto_increment primary key,
        admin_id int not null,
        server_id int not null,
        foreign key (admin_id) references iks_admins(id),
        foreign key (server_id) references iks_servers(id)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;

    create table if not exists iks_comms(
        id int not null auto_increment primary key,
        steam_id bigint not null,
        ip varchar(32),
        name varchar(128),
        mute_type int not null comment '0 - voice(mute), 1 - chat(gag), 2 - both(silence)', 
        duration int not null,
        reason varchar(128) not null,
        server_id int default null,
        admin_id int not null,
        unbanned_by int default null,
        unban_reason varchar(128) default null,
        created_at int not null,
        end_at int not null,
        updated_at int not null,
        deleted_at int default null,
        foreign key (admin_id) references iks_admins(id),
        foreign key (unbanned_by) references iks_admins(id),
        foreign key (server_id) references iks_servers(id),
        index `idx_steam_id` (`steam_id`)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;

    create table if not exists iks_bans(
        id int not null auto_increment primary key,
        steam_id bigint,
        ip varchar(32),
        name varchar(128),
        duration int not null,
        reason varchar(128) not null,
        ban_type tinyint not null default 0 comment '0 - SteamId, 1 - Ip, 2 - Both',
        server_id int default null,
        admin_id int not null,
        unbanned_by int default null,
        unban_reason varchar(128) default null,
        created_at int not null,
        end_at int not null,
        updated_at int not null,
        deleted_at int default null,
        foreign key (admin_id) references iks_admins(id),
        foreign key (unbanned_by) references iks_admins(id),
        foreign key (server_id) references iks_servers(id),
        index `idx_steam_id` (`steam_id`)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
    create table if not exists iks_admins_warns(
        id int not null auto_increment primary key,
        admin_id int not null,
        target_id int not null,
        duration int not null,
        reason varchar(128) not null,
        created_at int not null,
        end_at int not null,
        updated_at int not null,
        deleted_at int default null,
        deleted_by int default null,
        foreign key (admin_id) references iks_admins(id),
        foreign key (target_id) references iks_admins(id),
        foreign key (deleted_by) references iks_admins(id)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
    create table if not exists iks_groups_limitations( 
        id int not null auto_increment primary key,
        group_id int not null,
        limitation_key varchar(64) not null,
        limitation_value varchar(32) not null,
        foreign key (group_id) references iks_groups(id)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE utf8mb4_unicode_ci;
                ");
            }
            catch (MySqlException e)
            {
                throw new Exception(e.Message);
            }
        }
    public static  string GetSqlWithParameters(string sql, object parameters)
    {
        var query = sql;
        
        // Получаем все свойства объекта параметров
        var parameterDictionary = parameters.GetType()
                                             .GetProperties()
                                             .ToDictionary(prop => "@" + prop.Name, prop => prop.GetValue(parameters));

        foreach (var param in parameterDictionary)
        {
            string parameterValue = param.Value?.ToString() ?? "NULL";  // Если значение null, подставляем NULL
            if (param.Value is string || param.Value is DateTime) // Для строк и DateTime подставляем в одинарные кавычки
            {
                parameterValue = $"'{parameterValue.Replace(@"\", "").Replace("'", "").Replace("\"", "")}'";  // Экранируем одиночные кавычки
            }

            // Заменяем параметр на его значение в строке запроса
            query = query.Replace(param.Key, parameterValue);
        }

        return query;
    }
}
