using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeDataMapper
    {
        private WarframeDataContext dbContext { get; set; }
        private Dictionary<string, string> itemList { get; set; }
        private Dictionary<string, string> mapList { get; set; }

        public WarframeDataMapper()
        {
            dbContext = new WarframeDataContext();
            dbContext.Database.EnsureCreated();

            //SetMinimumQuantity(GetItem("/Lotus/Types/Items/Research/EnergyComponent"), 2);
            //SetMinimumQuantity(GetItem("/Lotus/Types/Items/Research/EnergyFragment"), 2);
        }

        public WarframeItem GetItem(string itemURI)
        {
            WarframeItem result = null;
            if (!String.IsNullOrEmpty(itemURI))
            {
                try
                {
                    result = dbContext.WarframeItems.Where(s => s.ItemURI == itemURI).Single();
                }
                catch (Exception)
                {
                    result = null;
                }
            }
            return result;
        }

        public List<WarframeItemCategory> GetItemCategories(WarframeItem item)
        {
            List<WarframeItemCategory> categories = dbContext.ItemCategoryAssociations.Where(s => s.ItemID == item.ID).Select(s => s.Category).ToList();
            return categories;
        }

        public int GetItemID(string itemURI)
        {
            if (dbContext != null)
            {
                try
                {
                    return dbContext.WarframeItems.Where(x => x.ItemURI == itemURI).Single().ID;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
            return -1;
        }

        public string GetItemName(string itemURI)
        {
            string result;
            if (dbContext != null)
            {
                try
                {
                    result = dbContext.WarframeItems.Where(x => x.ItemURI == itemURI).Single().Name;
                }
                catch (Exception)
                {
                    result = itemURI;
                }
                return result;
            }
            return "";
        }

        public string GetNodeName(string node)
        {
            string result;
            if (dbContext != null)
            {
                try
                {
                    result = dbContext.SolarNodes.Where(x => x.NodeURI == node).Single().NodeName;
                }
                catch(Exception)
                {
                    result = node;
                }
                return result;
            }
            return "";
        }

        public int GetWarframeItemMinimumQuantity(WarframeItem item)
        {
            int res;
            try
            {
                res = dbContext.WFMiscIgnoreOptions.Where(x => x.ItemID == item.ID).Single().MinQuantity;
            }
            catch (Exception)
            {
                res = 0;
            }
            return res;
        }

        public int GetMinimumCredits()
        {
            //Get WarframeItem entry for credits
            var item = GetItem("/Lotus/Language/Menu/Monies");
            int minCred = GetWarframeItemMinimumQuantity(item);
            return minCred;
        }

        public void SetIgnoreOption(WarframeItem item, bool ignore)
        {
            item.Ignore = !ignore ? 0 : 1;
        }

        public void SetMinimumQuantity(WarframeItem item, int minimumQuantity)
        {
            try
            {
                WFMiscIgnoreSettings option = dbContext.WFMiscIgnoreOptions.Where(x => x.ItemID == item.ID).Single();
                if (option != null)
                {
                    if (minimumQuantity > 0)
                        option.MinQuantity = minimumQuantity;
                }
            }
            catch (Exception)
            {
                //Do nothing
                Console.WriteLine("Failed to set minimum quantity!");
            }
            
        }
    }
}
