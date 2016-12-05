using System.Collections.Generic;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WarframeItem
    {
        public int ID { get; set; }
        public string ItemURI { get; set; }
        public string Name { get; set; }
        public int Ignore { get; set; }
        virtual public ICollection<ItemCategoryAssociation> Categories { get; set; }
    }
}
