using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdater;

public class AdminToServer
{
    public int AdminId {get; set;}
    public int ServerId {get; set;}
    public AdminToServer(int adminId, int serverId) {
        AdminId = adminId;
        ServerId = serverId;
    }
}