using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFNewSortieCondition
    {
        [Key]
        public int ID { get; set; }
        public string SortieConditionID { get; set; }
        public string ConditionName { get; set; }
    }
}
