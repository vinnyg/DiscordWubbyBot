using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Enums.MissionType;
using WarframeDatabaseNet.Enums.NodeType;

namespace WarframeDatabaseNet.Core.Repository.Interfaces
{
    public interface IWFSortieRepository : IRepository<WFSortieMission>
    {
        string GetMissionType(int missionID, int regionID);
        string GetBoss(int bossIndex);
        string GetFaction(int bossIndex);
        string GetRegion(int regionID);
        string GetCondition(int conditionIndex);
    }
}
