using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFVoidFissure
    {
        [Key]
        public string FissureURI { get; set; }
        public string FissureName { get; set; }
    }
}
