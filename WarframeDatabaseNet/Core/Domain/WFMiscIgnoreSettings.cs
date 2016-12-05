using System.ComponentModel.DataAnnotations;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WFMiscIgnoreSettings
    {
        [Key]
        public int ItemID { get; set; }
        public int MinQuantity { get; set; }
    }
}
