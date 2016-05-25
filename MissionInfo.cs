using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class MissionInfo
    {
        public string Faction { get; private set; }
        public string MissionType { get; private set; }
        public int Credits { get; private set; }
        public string Reward { get; private set; }
        public int RewardQuantity { get; private set; }
        public int MinimumLevel { get; private set; }
        public int MaximumLevel { get; private set; }

        public MissionInfo(string factionName, string missionType, int credits, string reward, int rewardQuantity, int minLevel, int maxLevel)
        {
            Faction = DiscordSharpTest.Faction.GetName(factionName);
            MissionType = DiscordSharpTest.MissionType.GetName(missionType);
            Credits = credits;
            Reward = reward; //DiscordSharpTest.Reward.GetName(reward);
            RewardQuantity = rewardQuantity;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
        }

        public MissionInfo(MissionInfo info)
        {
            Faction = info.Faction;
            MissionType = info.MissionType;
            Credits = info.Credits;
            Reward = info.Reward;
            RewardQuantity = info.RewardQuantity;
            MinimumLevel = info.MinimumLevel;
            MaximumLevel = info.MaximumLevel;
        }
    }
}
