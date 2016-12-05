using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFPlanetRegionMission
    {
        [Key]
        public int ID { get; set; }
        public int RegionID { get; set; }
        virtual public WFRegion Region { get; set; }

        //The order that mission indices appear in the Warframe source per region.
        //e.g. Each region has a different set of available missions, therefore the index of each mission will be different depending on the region.
        public int JSONIndexOrder { get; set; }

        virtual public WFSortieMission Mission { get; set; }

        public int MissionID { get; set; }
    }
}
