using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    public class WarframeSortie : WarframeEvent
    {
        public List<MissionInfo> VariantDetails { get; private set; }
        public List<string> VariantDestinations { get; private set; }
        public List<string> VariantConditions { get; private set; }
        public DateTime ExpireTime { get; internal set; }

        public WarframeSortie(List<MissionInfo> varDetails, string guid, List<string> varDest, List<string> varCond, DateTime startTime, DateTime expireTime) : base(guid, "", startTime)
        {
            VariantDetails = new List<MissionInfo>();
            VariantDestinations = new List<string>();
            VariantConditions = new List<string>();

            //Add respective details and destination names to sortie variants info lists.
            varDetails.ForEach(s => VariantDetails.Add(s));
            varDest.ForEach(s => VariantDestinations.Add(s));
            varCond.ForEach(s => VariantConditions.Add(s));

            ExpireTime = expireTime;
        }

        public WarframeSortie(WarframeSortie sortie) : base(sortie.GUID, sortie.DestinationName, sortie.StartTime)
        {
            VariantDetails = new List<MissionInfo>(sortie.VariantDetails);
            VariantDestinations = new List<string>(sortie.VariantDestinations);
            ExpireTime = sortie.ExpireTime;
        }

        public int GetMinutesRemaining(bool untilStart)
        {
            TimeSpan ts = untilStart ? StartTime.Subtract(DateTime.Now) : ExpireTime.Subtract(DateTime.Now);
            int days = ts.Days, hours = ts.Hours, mins = ts.Minutes;
            return (days * 1440) + (hours * 60) + ts.Minutes;

            //return string.Format($"Destination: **{_destinationName}\n**Mission: **{Mission} ({Faction})\n**Reward: **{_loot}, {Credits}\n**Status: **{_expireTime}**");
        }

        override public bool IsExpired()
        {
            return (GetMinutesRemaining(false) <= 0);
        }
    }
}
