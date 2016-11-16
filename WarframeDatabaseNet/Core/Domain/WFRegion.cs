using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFRegion
    {
        [Key]
        public int ID { get; set; }
        public string RegionName { get; set; }
    }
}
