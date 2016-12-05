using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFRegion
    {
        [Key]
        public int ID { get; set; }
        public string RegionName { get; set; }
    }
}
