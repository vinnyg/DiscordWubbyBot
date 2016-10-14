using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    public class WarframeDataMapper
    {
        //private WarframeDataContext dbContext { get; set; }

        public WarframeDataMapper(WarframeDataContext wdc = null)
        {
            /*if (wdc.Equals(null))
            {
                dbContext = new WarframeDataContext();
            }
            else
            {
                dbContext = wdc;
            }
            dbContext.Database.EnsureCreated();*/

        }

        public WarframeItem GetItem(string itemURI)
        {
            WarframeItem result = null;

            string altItemURI = GetAltItemURI(itemURI);

            if ((!String.IsNullOrEmpty(itemURI)) && (!String.IsNullOrEmpty(altItemURI)))
            {
                using (var dbCon = new WarframeDataContext())
                {
                    var iQ = dbCon.WarframeItems.Where(s => s.ItemURI == itemURI);
                    if (iQ.Count() == 0)
                    {
                        iQ = dbCon.WarframeItems.Where(s => s.ItemURI == altItemURI);
                    }

                    if (iQ.Count() > 0)
                        result = iQ.Single();
                }

                    
            }
            return result;
        }

        public List<WarframeItemCategory> GetItemCategories(WarframeItem item)
        {
            List<WarframeItemCategory> categories;

            using (var dbCon = new WarframeDataContext())
            {
                categories = dbCon.ItemCategoryAssociations.Where(s => s.ItemID == item.ID).Select(s => s.Category).ToList();
            }
            return categories;
        }

        public int GetItemID(string itemURI)
        {
            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    try
                    {
                        return dbCon.WarframeItems.Where(x => x.ItemURI == itemURI).Single().ID;
                    }
                    catch (Exception)
                    {
                        return -1;
                    }
                }
            }
                
            return -1;
        }

        public string GetItemName(string itemURI)
        {
            string result = itemURI;
            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var item = dbCon.WarframeItems.Where(x => x.ItemURI == itemURI);
                    if (item.Count() > 0)
                        result = item.Single().Name;
                    else
                        result = BandAidGetItemName(itemURI);
                }
            }
            
            return result;
        }

        //It appears that the URI for items has changed so use this method to check for existing items under the new URI format if no items can be found for the old format 
        private string BandAidGetItemName(string itemURI)
        {
            string altItemURI = GetAltItemURI(itemURI);
            string result = itemURI;
            using (var dbCon = new WarframeDataContext())
            {
                var item = dbCon.WarframeItems.Where(x => x.ItemURI == altItemURI);

                if (item.Count() > 0)
                    result = item.Single().Name;
            }

            return result;
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
            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    try
                    {
                        result = dbCon.SolarNodes.Where(x => x.NodeURI == node).Single().NodeName;
                    }
                    catch (Exception)
                    {
                        result = node;
                    }
                    return result;
                }
            }

            return "";
        }

        public int GetWarframeItemMinimumQuantity(WarframeItem item)
        {
            int res;
            using (var dbCon = new WarframeDataContext())
            {
                try
                {
                    res = dbCon.WFMiscIgnoreOptions.Where(x => x.ItemID == item.ID).Single().MinQuantity;
                }
                catch (Exception)
                {
                    res = 0;
                }
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
            using (var dbCon = new WarframeDataContext())
            {
                try
                {
                    WFMiscIgnoreSettings option = dbCon.WFMiscIgnoreOptions.Where(x => x.ItemID == item.ID).Single();
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

        public string GetFissureName(string fissureURI)
        {
            string result = fissureURI;

            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var item = dbCon.WFVoidFissures.Where(x => x.FissureURI == fissureURI);
                    if (item.Count() > 0)
                        result = item.Single().FissureName;
                    /*else
                        result = BandAidGetItemName(fissureURI);*/
                }
            }
            return result;
        }

        public int GetRegionMission(int regionIndex, int missionIndex)
        {
            //11 is an unidentified mission type
            var result = 11;

            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var index = dbCon.WFPlanetRegionMissions.Where(x => (x.RegionID == regionIndex) && (x.JSONIndexOrder == missionIndex));
                    if (index.Count() > 0)
                        result = index.Single().MissionID;
                }
            }
            return result;
        }

        public string GetSortieRegionName(int regionIndex)
        {
            var result = $"region{regionIndex}";

            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var item = dbCon.WFRegionNames.Where(x => x.ID == regionIndex);
                    if (item.Count() > 0)
                        result = item.Single().RegionName;
                }
            }
            return result;
        }

        public string GetSortieMissionName(int missionID)
        {
            var result = $"mission{missionID}";
            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var item = dbCon.WFSortieMissions.Where(x => x.ID == missionID);
                    if (item.Count() > 0)
                        result = item.Single().MissionType;
                }
            }
            return result;
        }

        public string GetSortieConditionName(int conditionIndex)
        {
            var result = $"ConditionIndex{conditionIndex}";
            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var item = dbCon.WFSortieConditions.Where(x => x.ConditionIndex == conditionIndex);
                    if (item.Count() > 0)
                        result = item.Single().ConditionName;
                }
            }
            return result;
        }

        public string GetBossName(int bossIndex)
        {
            var result = $"boss{bossIndex}";

            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var item = dbCon.WFBossInfo.Where(x => x.Index == bossIndex);
                    if (item.Count() > 0)
                        result = item.Single().Name;
                }
            }
            return result;
        }

        public string GetBossFaction(int bossIndex)
        {
            var result = $"faction{bossIndex}";

            using (var dbCon = new WarframeDataContext())
            {
                if (dbCon != null)
                {
                    var item = dbCon.WFBossInfo.Where(x => x.Index == bossIndex);
                    if (item.Count() > 0)
                        result = item.Single().FactionIndex;
                }
            }
            return result;
        }
    }
}
