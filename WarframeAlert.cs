using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeAlert : WarframeEvent
    {
        public MissionInfo MissionDetails { get; private set; }

        public DateTime ExpireTime { get; internal set; }

        public WarframeAlert(MissionInfo info, string guid, string destinationName, DateTime startTime, DateTime expireTime) : base(guid, destinationName, startTime)
        {
            MissionDetails = info;
            ExpireTime = expireTime;
        }

        /*[Obsolete]
        public WarframeAlert(string alert) : base("", "")
        {
            string[] splitAlert = alert.Split('|');
            DestinationName = splitAlert[0];

            //Faction and mission type are held in the same substring
            Mission = (splitAlert[1].Split('('))[0].Trim();
            //Regex.
            Faction = Regex.Match(splitAlert[1], @"\((.*?)\)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();//s.Substring(0, s.Length - 2);
            //Time
            //var strTimeLeft = Regex.Match(splitAlert[4], @"(\d+m) \| (\d+m)", RegexOptions.None).Captures;
            int i, j;
            int.TryParse(Regex.Match(splitAlert[3], @"(\d+)", RegexOptions.None).Value, out i);
            int.TryParse(Regex.Match(splitAlert[4], @"(\d+)", RegexOptions.None).Value, out j);

            //May be used for a new status indicating when an alert begins
            TimeTilStart = i;

            MinutesRemaining = j;
            TimeSpan expireTimeSpan = new TimeSpan(0, i + j, 0);

            ExpireTime = DateTime.Now + expireTimeSpan;
            //_expireTime = String.Format($"{(ExpirationTime):HH:mm}");

            //Capture digits suffixed with 'cr'
            //Credits = Regex.Match(alert, @"((\d+)(,)?)+(cr)", RegexOptions.None).Value;
            //Credits = int.TryParse(Regex.Match(alert, @"([\d,])+(?=cr)", RegexOptions.None).Value, NumberStyles.AllowThousands);
            Loot = splitAlert[5].Split('-')[1];


            //GetMinutesRemaining();
        }*/

        public int GetMinutesRemaining(bool untilStart)
        {
            TimeSpan ts = untilStart ? StartTime.Subtract(DateTime.Now) : ExpireTime.Subtract(DateTime.Now);
            int days = ts.Days;
            int hours = ts.Hours;
            int mins = ts.Minutes;
            return (days * 1440) + (hours * 60) + ts.Minutes;

            //return string.Format($"Destination: **{_destinationName}\n**Mission: **{Mission} ({Faction})\n**Reward: **{_loot}, {Credits}\n**Status: **{_expireTime}**");
        }

        override public bool HasExpired()
        {
            return (GetMinutesRemaining(false) <= 0);
        }
    }
}
