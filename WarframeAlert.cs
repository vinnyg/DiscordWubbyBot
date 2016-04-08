using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DiscordSharpTest
{
    class WarframeAlert
    {
        public string DestinationName { get; internal set; }
        public string Faction { get; internal set; }
        public string Mission { get; internal set; }
        public int Credits { get; internal set; }
        public string Loot { get; internal set; }

        [Obsolete]
        public int TimeTilStart { get; internal set; }

        //Remaining time in minutes until the alert expires
        public int MinutesRemaining { get; internal set; }

        /*--*///public string _expireTime { get; internal set; }

        public DateTime ExpirationTime { get; internal set; }
        public string AssociatedMessageID { get; set; }

        public WarframeAlert(string destinationName, string factionName, string missionTypeName, int credits, string lootName, int minutesToExpire)
        {
            DestinationName = destinationName;
            Faction = factionName;
            Mission = missionTypeName;
            Credits = credits;
            Loot = lootName;
            MinutesRemaining = minutesToExpire;

            TimeSpan timeToExpire = new TimeSpan(0, minutesToExpire, 0);
            ExpirationTime = DateTime.Now + timeToExpire;
        }

        [Obsolete]
        public WarframeAlert(string alert)
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

            ExpirationTime = DateTime.Now + expireTimeSpan;
            //_expireTime = String.Format($"{(ExpirationTime):HH:mm}");

            //Capture digits suffixed with 'cr'
            //Credits = Regex.Match(alert, @"((\d+)(,)?)+(cr)", RegexOptions.None).Value;
            //Credits = int.TryParse(Regex.Match(alert, @"([\d,])+(?=cr)", RegexOptions.None).Value, NumberStyles.AllowThousands);
            Loot = splitAlert[5].Split('-')[1];


            UpdateStatus();
        }

        public void UpdateStatus()
        {
            TimeSpan ts = ExpirationTime.Subtract(DateTime.Now);
            int days = ts.Days;
            int hours = ts.Hours;
            int mins = ts.Minutes;
            MinutesRemaining = (days * 1440) + (hours * 60) + ts.Minutes;

            //return string.Format($"Destination: **{_destinationName}\n**Mission: **{Mission} ({Faction})\n**Reward: **{_loot}, {Credits}\n**Status: **{_expireTime}**");
        }
    }
}
