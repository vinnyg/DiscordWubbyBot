using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Core.Repository.Interfaces;

namespace WarframeDatabaseNet.Persistence.Repository
{
    class WFSortieRepository : Repository<WFSortieMission>, IWFSortieRepository
    {
        public WFSortieRepository(WarframeDataContext context) : base(context)
        {

        }

        public WarframeDataContext WFDataContext
        {
            get { return Context as WarframeDataContext; }
        }

        public string GetBoss(int bossIndex)
        {
            var result = $"boss{bossIndex}";

            var item = WFDataContext.WFBossInfo.Where(x => x.Index == bossIndex);
            if (item.Count() > 0)
                result = item.Single().Name;

            return result;
        }

        public string GetCondition(int conditionIndex)
        {
            var result = $"ConditionIndex{conditionIndex}";
            
            var item = WFDataContext.WFSortieConditions.Where(x => x.ConditionIndex == conditionIndex);
            if (item.Count() > 0)
                result = item.Single().ConditionName;
                
            return result;
        }

        public string GetFaction(int bossIndex)
        {
            var result = $"faction{bossIndex}";

            var item = WFDataContext.WFBossInfo.Where(x => x.Index == bossIndex);
            if (item.Count() > 0)
                result = item.Single().FactionIndex;
                
            return result;
        }

        public string GetMissionType(int missionID, int regionID)
        {
            //TODO: 11 is MT_GENERIC - please change this
            var missionIndex = 11;
            //This first part gets the mission index based on region
            var index = WFDataContext.WFPlanetRegionMissions.Where(x => (x.RegionID == regionID) && (x.JSONIndexOrder == missionID));
            if (index.Count() > 0)
                missionIndex = index.Single().MissionID;
            //Then we get the mission name from the corresponding region
            var result = $"mission{missionID}";

            var item = WFDataContext.WFSortieMissions.Where(x => x.ID == missionID);
            if (item.Count() > 0)
                result = item.Single().MissionType;
            
            return result;
        }

        public string GetRegion(int regionID)
        {
            var result = $"region{regionID}";
            
            var item = WFDataContext.WFRegionNames.Where(x => x.ID == regionID);
            if (item.Count() > 0)
                result = item.Single().RegionName;
                
            return result;
        }
    }
}
