using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeVoidFissure : WarframeEvent
    {
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

        override public bool IsExpired()
        {
            return (GetMinutesRemaining(false) <= 0);
        }
    }
}
