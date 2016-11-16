using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
