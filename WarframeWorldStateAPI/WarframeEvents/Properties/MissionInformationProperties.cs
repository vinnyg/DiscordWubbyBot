using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace WarframeWorldStateAPI.WarframeEvents.Properties
{
    public static class Faction
    {
        public const string GRINEER = "Grineer";
        public const string CORPUS = "Corpus";
        public const string INFESTATION = "Infestation";
        public const string OROKIN = "Orokin";

        private static readonly Dictionary<string, string> _factionNames = new Dictionary<string, string>
        {
            { "FC_GRINEER", "Grineer" },
            { "FC_CORPUS", "Corpus" },
            { "FC_INFESTATION", "Infestation" },
            { "FC_OROKIN", "Orokin" },
            { "FC_DE", "What?" }
        };

        private static readonly string[] _projectName = {
            "Balor Fomorian",
            "Razorback",
            "Juggernaut Behemoth",
            "Orokin",
            "Nerf Hammer"    
        };

        //Return the name of a faction using a string identifier
        public static string GetName(string faction)
        {
            return _factionNames.ContainsKey(faction) ? _factionNames[faction] : faction;
        }

        //Return the name of the faction using just an ID if no string identifier is available
        public static string GetNameByID(int id)
        {
            try
            {
                return _factionNames.ElementAt(id).Value;
            }
            catch (ArgumentOutOfRangeException)
            {
                return GetName("FC_DE");
            }
            catch (ArgumentNullException)
            {
                return id.ToString();
            }
        }

        //Return the name of a project using an ID
        public static string GetProjectNameByID(int id)
        {
            try
            {
                return _projectName.ElementAt(id);
            }
            catch (ArgumentOutOfRangeException)
            {
                return id.ToString();
            }
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
