using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFSortieMission
    {
        [Key]
        public int ID { get; set; }
        public string MissionType { get; set; }
    }
}
