using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFBoss
    {
        [Key]
        public int ID { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string FactionIndex { get; set; }
    }
}
