using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeDatabaseNet.Core.Domain
{
    public class WarframeItemCategory
    {
        public int ID { get; set; }
        public int TypeID { get; set; }
        public string Name { get; set; }
        public int Ignore { get; set; }
        virtual public ICollection<ItemCategoryAssociation> Items { get; set; }
    }
}
