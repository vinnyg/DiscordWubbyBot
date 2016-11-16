using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;

namespace WarframeDatabaseNet.Core.Repository.Interfaces
{
    public interface IWarframeItemRepository : IRepository<WarframeItem>
    {
        WarframeItem GetItemByURI(string itemURI);
        IEnumerable<WarframeItemCategory> GetCategories(string itemURI);
        int GetItemID(string itemURI);
        string GetItemName(string itemURI);
        int GetMinimumQuantity(string itemURI);
    }
}
