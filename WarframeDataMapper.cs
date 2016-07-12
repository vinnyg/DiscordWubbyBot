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

        public WarframeDataMapper()
        {
            dbContext = new WarframeDataContext();
            dbContext.Database.EnsureCreated();
        }

        public WarframeItem GetItem(string itemURI)
        {
            WarframeItem result = null;

            string altItemURI = GetAltItemURI(itemURI);

            if ((!String.IsNullOrEmpty(itemURI)) && (!String.IsNullOrEmpty(altItemURI)))
            {
                var iQ = dbContext.WarframeItems.Where(s => s.ItemURI == itemURI);
                if (iQ.Count() == 0)
                {
                    iQ = dbContext.WarframeItems.Where(s => s.ItemURI == altItemURI);
                }

                if (iQ.Count() > 0)
                    result = iQ.Single();
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
            string result = itemURI;
            if (dbContext != null)
            {
                var item = dbContext.WarframeItems.Where(x => x.ItemURI == itemURI);
                if (item.Count() > 0)
                    result = item.Single().Name;
                else
                    result = BandAidGetItemName(itemURI);
            }
            return result;
        }

        private string BandAidGetItemName(string itemURI)
        {
            string altItemURI = GetAltItemURI(itemURI);

            var item = dbContext.WarframeItems.Where(x => x.ItemURI == altItemURI);

            if (item.Count() > 0)
                return item.Single().Name;
            else
                return itemURI;
        }

        private string GetAltItemURI(string URI)
        {
            var splitString = URI.Split('/');
            StringBuilder altItemURI = new StringBuilder();

            //Rebuild the itemURI string, omitting the substring which contains StoreItems
            foreach (var i in splitString)
            {
                if ((i != "StoreItems") && (!String.IsNullOrEmpty(i)))
                {
                    altItemURI.Append('/' + i);
                }
            }

            return altItemURI.ToString();
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

        public string GetFissureName(string fissureURI)
        {
            string result = fissureURI;
            if (dbContext != null)
            {
                var item = dbContext.WFVoidFissures.Where(x => x.FissureURI == fissureURI);
                if (item.Count() > 0)
                    result = item.Single().FissureName;
                else
                    result = BandAidGetItemName(fissureURI);
            }
            return result;
        }
    }
}
