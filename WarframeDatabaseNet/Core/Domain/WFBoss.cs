using System.ComponentModel.DataAnnotations;

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
