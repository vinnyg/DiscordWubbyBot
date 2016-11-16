using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
