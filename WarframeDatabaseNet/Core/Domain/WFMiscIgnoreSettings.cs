using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFMiscIgnoreSettings
    {
        [Key]
        public int ItemID { get; set; }
        public int MinQuantity { get; set; }
    }
}
