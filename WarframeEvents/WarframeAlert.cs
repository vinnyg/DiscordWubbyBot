﻿using DiscordSharpTest.WarframeEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest.WarframeEvents
{
    public class WarframeAlert : WarframeEvent
    {
        public MissionInfo MissionDetails { get; private set; }
        public DateTime ExpireTime { get; internal set; }

        public WarframeAlert(MissionInfo info, string guid, string destinationName, DateTime startTime, DateTime expireTime) : base(guid, destinationName, startTime)
        {
            MissionDetails = info;
            ExpireTime = expireTime;
        }

        public WarframeAlert(WarframeAlert alert) : base(alert.GUID, alert.DestinationName, alert.StartTime)
        {
            MissionDetails = new MissionInfo(alert.MissionDetails);
            ExpireTime = alert.ExpireTime;
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
