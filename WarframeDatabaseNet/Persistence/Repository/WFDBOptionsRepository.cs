using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Core.Repository.Interfaces;

namespace WarframeDatabaseNet.Persistence.Repository
{
    class WFDBOptionsRepository : Repository<WFMiscIgnoreSettings>, IWFDBOptionsRepository
    {
        public WFDBOptionsRepository(WarframeDataContext context) : base(context)
        {

        }

        public WarframeDataContext WFDataContext
        {
            get { return Context as WarframeDataContext; }
        }

        /*public int GetMinimumCredits()
        {
            //Get WarframeItem entry for credits
            var item = GetItem("/Lotus/Language/Menu/Monies");
            int minCred = GetWarframeItemMinimumQuantity(item);
            return minCred;
        }*/

        public int GetItemMinimum(WarframeItem item)
        {
            int result = 0;
            
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
