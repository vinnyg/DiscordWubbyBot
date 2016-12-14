using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Core.Repository.Interfaces;

namespace WarframeDatabaseNet.Persistence.Repository
{
    public class WarframeItemRepository : Repository<WarframeItem>, IWarframeItemRepository
    {
        public WarframeItemRepository(WarframeDataContext context) : base(context)
        {
        }

        public WarframeDataContext WFDataContext
        {
            get { return Context as WarframeDataContext; }
        }

        public WarframeItem GetItemByURI(string itemURI)
        {
            WarframeItem result = null;
            string altItemURI = GetAltItemURI(itemURI);

            if ((!string.IsNullOrEmpty(itemURI)) && (!string.IsNullOrEmpty(altItemURI)))
            {
                var iQ = WFDataContext.WarframeItems.Where(s => s.ItemURI == itemURI);
                if (iQ.Count() == 0)
                {
                    iQ = WFDataContext.WarframeItems.Where(s => s.ItemURI == altItemURI);
                }

                if (iQ.Count() > 0)
                    result = iQ.Single();
            }
            return result;
        }

        public IEnumerable<WarframeItemCategory> GetCategories(string itemURI)
        {
            List<WarframeItemCategory> categories;
            WarframeItem item = GetItemByURI(itemURI);
            categories = WFDataContext.ItemCategoryAssociations.Where(s => s.ItemID == item.ID).Select(s => s.Category).ToList();

            return categories;
        }

        //For legacy reasons we are including an overloaded signature matching the previous accessor.
        public IEnumerable<WarframeItemCategory> GetCategories(WarframeItem item)
        {
            List<WarframeItemCategory> categories;
            categories = WFDataContext.ItemCategoryAssociations.Where(s => s.ItemID == item.ID).Select(s => s.Category).ToList();

            return categories;
        }

        public int GetItemID(string itemURI)
        {
            const int NON_EXISTANT_ITEM = -1;
            try
            {
                return WFDataContext.WarframeItems.Where(x => x.ItemURI == itemURI).Single().ID;
            }
            catch (InvalidOperationException)
            {
                return NON_EXISTANT_ITEM;
            }
        }

        public string GetItemName(string itemURI)
        {
            var result = itemURI;
            var altItemURI = GetAltItemURI(itemURI);
            var item = WFDataContext.WarframeItems.Where(x => x.ItemURI == itemURI);
            //To save us calling another method, we just do the check for the alternative URI structure in here
            var altItem = WFDataContext.WarframeItems.Where(x => x.ItemURI == altItemURI);
            if (item.Count() > 0)
                result = item.Single().Name;
            else if (altItem.Count() > 0)
                result = altItem.Single().Name;

            return result;
        }

        public bool IsIgnored(string itemURI)
        {
            const int TRUE = 1;

            if (string.IsNullOrEmpty(itemURI))
                return true;

            var altItemURI = GetAltItemURI(itemURI);
            var item = WFDataContext.WarframeItems.Where(x => x.ItemURI == itemURI);
            var altItem = WFDataContext.WarframeItems.Where(x => x.ItemURI == altItemURI);

            if (item.Count() > 0) return
                    (item.Single().Ignore == TRUE);
            else if (altItem.Count() > 0)
                return (altItem.Single().Ignore == TRUE);
            else return false;
        }

        //Due to a change in itemURI structure we have to do a check for the new itemURI structure as well as a check for the legacy structure
        private string GetAltItemURI(string URI)
        {
            if (string.IsNullOrEmpty(URI))
                return string.Empty;

            var splitString = URI.Split('/');
            StringBuilder altItemURI = new StringBuilder();

            //Rebuild the itemURI string, omitting the substring which contains StoreItems
            foreach (var i in splitString)
            {
                if ((i != "StoreItems") && (!string.IsNullOrEmpty(i)))
                    altItemURI.Append('/' + i);
            }

            return altItemURI.ToString();
        }

        public int GetMinimumQuantity(string itemURI)
        {
            if (string.IsNullOrEmpty(itemURI))
                return 0;

            var item = GetItemByURI(itemURI);
            int result;

            try
            {
                result = WFDataContext.WFMiscIgnoreOptions.Where(x => x.ItemID == item.ID).Single().MinQuantity;
            }
            catch (Exception)
            {
                result = 0;
            }

            return result;
        }
    }
}
