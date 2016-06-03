using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeVoidTrader : WarframeEvent
    {
        public DateTime ExpireTime { get; internal set; }

        public WarframeVoidTrader(string guid, string destinationName, DateTime startTime, DateTime expireTime) : base(guid, destinationName, startTime)
        {
            ExpireTime = expireTime;
        }

        public int GetMinutesRemaining(bool untilStart)
        {
            TimeSpan ts = untilStart ? StartTime.Subtract(DateTime.Now) : ExpireTime.Subtract(DateTime.Now);
            int days = ts.Days, hours = ts.Hours, mins = ts.Minutes;
            return (days * 1440) + (hours * 60) + ts.Minutes;
        }

        public TimeSpan GetTimeRemaining(bool untilStart)
        {
            return untilStart ? StartTime.Subtract(DateTime.Now) : ExpireTime.Subtract(DateTime.Now);
        }

        override public bool IsExpired()
        {
            return (GetMinutesRemaining(false) <= 0);
        }
    }
}
