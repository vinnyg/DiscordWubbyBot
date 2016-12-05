using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFSortieCondition
    {
        [Key]
        public int ID { get; set; }
        public int ConditionIndex { get; set; }
        public string ConditionName { get; set; }
    }
}
