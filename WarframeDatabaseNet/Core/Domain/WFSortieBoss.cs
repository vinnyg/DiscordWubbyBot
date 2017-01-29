using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFSortieBoss
    {
        [Key]
        public int ID { get; set; }
        public string SortieBossID { get; set; }
        public string BossName { get; set; }
        public string FactionIndex { get; set; }
    }
}
