using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest.WarframeEvents
{
    public class VoidTraderItem
    {
        public string Name { get; private set; }
        public int Credits { get; private set; }
        public int Ducats { get; private set; }

        public VoidTraderItem(string name, int credits, int ducats)
        {
            Name = name;
            Credits = credits;
            Ducats = ducats;
        }
    }

    public class WarframeVoidTrader : WarframeEvent
    {
        public DateTime ExpireTime { get; internal set; }
        public List<VoidTraderItem> Inventory { get; private set; }

        public WarframeVoidTrader(string guid, string destinationName, DateTime startTime, DateTime expireTime) : base(guid, destinationName, startTime)
        {
            ExpireTime = expireTime;
            Inventory = new List<VoidTraderItem>();
        }

        public void AddTraderItem(string name, int credits, int ducats)
        {
            Inventory.Add(new VoidTraderItem(name, credits, ducats));
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
