using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class SolarMapMission
    {
        [Key]
        public int NodeID { get; set; }
        public string MissionType { get; set; }
        public string Faction { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public int RequiresArchwing { get; set; }
        public string NodeType { get; set; }
    }
}
