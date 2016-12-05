using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DiscordSharpTest.WarframeEvents.Properties
{
    public static class Faction
    {
        public const string GRINEER = "Grineer";
        public const string CORPUS = "Corpus";
        public const string INFESTATION = "Infestation";
        public const string OROKIN = "Orokin";

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
            return FactionNames.ContainsKey(faction) ? FactionNames[faction] : faction;
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
            { "MT_EXCAVATE", "Excavation" },
            { "MT_EXTERMINATION", "Extermination" },
            { "MT_RETRIEVAL", "Hijack" },
            { "MT_TERRITORY", "Interception" },
            { "MT_HIVE", "Hive" },
            { "MT_MOBILE_DEFENSE", "Mobile Defense" },
            { "MT_RESCUE", "Rescue" },
            { "MT_SABOTAGE", "Sabotage" },
            { "MT_INTEL", "Spy" },
            { "MT_SURVIVAL", "Survival" }
        };

        public static string GetName(string missionType)
        {
            return MissionTypes.ContainsKey(missionType) ? MissionTypes[missionType] : missionType;
        }
    };

    public static class InvasionType
    {
        public const string INVASION = "Invasion";
        public const string OUTBREAK = "Outbreak";
    };
}
