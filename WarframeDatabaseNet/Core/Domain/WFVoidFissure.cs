using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFVoidFissure
    {
        [Key]
        public string FissureURI { get; set; }
        public string FissureName { get; set; }
    }
}
