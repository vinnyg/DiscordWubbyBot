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
        [Obsolete]
        string GetMissionType(int missionID, int regionID);
        //string GetMissionType(string missionID);
        [Obsolete]
        string GetBoss(int bossIndex);
        string GetBoss(string sortieBossID);
        [Obsolete]
        string GetFaction(int bossIndex);
        string GetFaction(string sortieBossID);
        string GetRegion(int regionID);
        [Obsolete]
        string GetCondition(int conditionIndex);
        string GetCondition(string sortieConditionID);
    }
}
