using WarframeWorldStateAPI.WarframeEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeWorldStateAPI.WarframeEvents
{
    public class WarframeOstronBounty : WarframeEvent
    {
        public MissionInfo MissionDetails { get; private set; }
        public DateTime ExpireTime { get; internal set; }

        public string JobType { get; private set; }
        public List<int> OstronStanding { get; private set; }
        public List<string> RewardTable { get; private set; }

        public WarframeOstronBounty(MissionInfo info, string guid, string destinationName, DateTime startTime, DateTime expireTime, string jobType, List<int> ostronStanding, List<string> rewardTable) : base(guid, destinationName, startTime)
        {
            MissionDetails = info;
            ExpireTime = expireTime;
            JobType = jobType;
            OstronStanding = ostronStanding;
            RewardTable = rewardTable;
        }

        public WarframeOstronBounty(WarframeOstronBounty bounty) : base(bounty.GUID, bounty.DestinationName, bounty.StartTime)
        {
            MissionDetails = new MissionInfo(bounty.MissionDetails);
            ExpireTime = bounty.ExpireTime;
        }

        public int GetMinutesRemaining(bool untilStart)
        {
            TimeSpan ts = untilStart ? StartTime.Subtract(DateTime.Now) : ExpireTime.Subtract(DateTime.Now);
            int days = ts.Days, hours = ts.Hours, mins = ts.Minutes;
            return (days * 1440) + (hours * 60) + ts.Minutes;
        }

        override public bool IsExpired()
        {
            return (GetMinutesRemaining(false) <= 0);
        }
    }
}
