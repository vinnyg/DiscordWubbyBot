using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    public class WarframeVoidFissure : WarframeEvent
    {
        private readonly Dictionary<string, int> fissureIndex = new Dictionary<string, int>() { { "Lith Fissure", 0 }, { "Meso Fissure", 1 }, { "Neo Fissure", 2}, { "Axi Fissure", 3 } };
        public MissionInfo MissionDetails { get; private set; }
        public DateTime ExpireTime { get; internal set; }
        
        public WarframeVoidFissure(MissionInfo info, string guid, string destinationName, DateTime startTime, DateTime expireTime) : base(guid, destinationName, startTime)
        {
            MissionDetails = info;
            ExpireTime = expireTime;
        }

        public int GetMinutesRemaining(bool untilStart)
        {
            TimeSpan ts = untilStart ? StartTime.Subtract(DateTime.Now) : ExpireTime.Subtract(DateTime.Now);
            int days = ts.Days, hours = ts.Hours, mins = ts.Minutes;
            return (days * 1440) + (hours * 60) + ts.Minutes;
        }

        public int GetFissureIndex()
        {
            return fissureIndex[MissionDetails.Reward];
        }

        override public bool IsExpired()
        {
            return (GetMinutesRemaining(false) <= 0);
        }
    }
}
