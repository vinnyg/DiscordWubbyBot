using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DiscordSharpTest
{
    public static class Faction
    {
        public const string GRINEER = "Grineer";
        public const string CORPUS = "Corpus";
        public const string INFESTATION = "Infestation";
        public const string SENTINEL = "Sentinel";

        private static readonly Dictionary<string, string> FactionNames = new Dictionary<string, string>
        {
            { "FC_GRINEER", "Grineer" },
            { "FC_CORPUS", "Corpus" },
            { "FC_INFESTATION", "Infestation" },
            { "FC_OROKIN", "Orokin" },
            { "FC_DE", "What?" }
        };

        public static string GetName(string faction)
        {
            return FactionNames.ContainsKey(faction) ? FactionNames[faction] : "Unknown Faction";
        }
    };

    public static class MissionType
    {
        private static readonly Dictionary<string, string> MissionTypes = new Dictionary<string, string>
        {
            { "MT_ASSASSINATION", "Assassination" },
            { "MT_CAPTURE", "Capture" },
            { "MT_COUNTER_INTEL", "Deception" },
            { "MT_DEFENSE", "Defense" },
            { "MT_EXCAVATE", "Excavate" },
            { "MT_EXTERMINATION", "Extermination" },
            { "MT_RETRIEVAL", "Hijack" },
            { "MT_TERRITORY", "Interception" },
            { "MT_INFESTED_SABOTAGE", "Hive" },
            { "MT_MOBILE_DEFENSE", "Mobile Defense" },
            { "MT_RESCUE", "Rescue" },
            { "MT_SABOTAGE", "Sabotage" },
            { "MT_INTEL", "Spy" },
            { "MT_SURVIVAL", "Survival" }
        };

        public static string GetName(string missionType)
        {
            return MissionTypes.ContainsKey(missionType) ? MissionTypes[missionType] : "Unknown Mission";
        }
    };

    public static class Reward
    {
        private static readonly Dictionary<string, string> RewardNames = new Dictionary<string, string>
        {
            //Some manual work that I don't really want to do. Refer to that Warframe Alerts Info application.
            //GameLangStrings\GameStringsEnglish.txt
            { "/Lotus/Types/Items/MiscItems/Alertium", "Nitain Extract" },
            { "/Lotus/Types/Items/MiscItems/OrokinCell", "Orokin Cell" },
            { "/Lotus/Types/Items/MiscItems/InfestedAladCoordinate", "Mutalist Alad V Coordinate" },
            { "/Lotus/Types/Items/Research/EnergyComponent", "Fieldron" },
            { "/Lotus/Types/Items/Research/BioComponent", "Mutagen Mass" },
            { "/Lotus/StoreItems/Upgrades/Mods/Aura/PlayerHolsterSpeedAuraMod", "Speed Holster Aura" },
            { "/Lotus/StoreItems/Upgrades/Mods/Aura/PlayerLootRadarAuraMod", "Loot Radar Aura" }
            
        };

        public static string GetName(string rewardName)
        {
            return RewardNames.ContainsKey(rewardName) ? RewardNames[rewardName] : rewardName.Split('/').Last();
        }
    }

    public static class InvasionType
    {
        public const string INVASION = "Invasion";
        public const string OUTBREAK = "Outbreak";
    };
}



/*namespace Extension
{
    
}*/
