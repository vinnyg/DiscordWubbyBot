using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using WarframeDatabaseNet;
using WarframeDatabaseNet.Persistence;
using WarframeWorldStateAPI.Extensions;
using WarframeWorldStateAPI.WarframeEvents;

namespace WarframeWorldStateAPI.Components
{
    public class WarframeEventInformationParser
    {
        //Consider responsibility of raising events
        private const int SECONDS_PER_DAY_CYCLE = 14400;
        private const int SECONDS_UNTIL_EVENT_NOT_NEW = 60;

        private List<WarframeAlert> _alertsList { get; set; } = new List<WarframeAlert>();
        private List<WarframeInvasion> _invasionsList { get; set; } = new List<WarframeInvasion>();
        private List<WarframeInvasionConstruction> _constructionProjectsList { get; set; } = new List<WarframeInvasionConstruction>();
        private List<WarframeVoidTrader> _voidTraders { get; set; } = new List<WarframeVoidTrader>();
        private List<WarframeVoidFissure> _voidFissures { get; set; } = new List<WarframeVoidFissure>();
        private List<WarframeSortie> _sortieList { get; set; } = new List<WarframeSortie>();
        private WarframeJSONScraper _scraper = new WarframeJSONScraper();

        #region ParseJSONMethods
        public IEnumerable<WarframeAlert> GetAlerts()
        {
            JObject worldState = _scraper.ScrapeWorldState();
            var resultAlerts = new List<WarframeAlert>();

            //Find Alerts
            foreach (var jsonAlert in worldState["Alerts"])
            {
                WarframeAlert currentAlert = _alertsList.Find(x => x.GUID == jsonAlert["_id"]["$id"].ToString());

                if (currentAlert == null)
                {
                    string id = jsonAlert["_id"]["$id"].ToString();
                    string loc = jsonAlert["MissionInfo"]["location"].ToString();

                    //Loot - Can be countable (Alertium etc.) or single (Blueprints) items
                    JToken countables = (jsonAlert["MissionInfo"]["missionReward"]["countedItems"]),
                        nonCountables = (jsonAlert["MissionInfo"]["missionReward"]["items"]);

                    var rewardStr = string.Empty;
                    var nodeName = loc;

                    using (var unit = new UnitOfWork(new WarframeDataContext()))
                    {
                        rewardStr = (countables != null ?
                            unit.WarframeItems.GetItemName(countables[0]["ItemType"].ToString()) :
                            (nonCountables != null ? unit.WarframeItems.GetItemName(nonCountables[0].ToString()) : ""));

                        nodeName = unit.WFSolarNodes.GetNodeName(loc);
                    }

                    var secondsUntilStart = double.Parse(jsonAlert["Activation"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonAlert["Expiry"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    DateTime startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    DateTime expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);

                    var creditReward = int.Parse(jsonAlert["MissionInfo"]["missionReward"]["credits"].ToString());
                    var reqArchwingData = jsonAlert["MissionInfo"]["archwingRequired"];
                    bool requiresArchwing = reqArchwingData != null ? bool.Parse(reqArchwingData.ToString()) : false;

                    JToken rewardParam = null;
                    if (countables != null) rewardParam = countables[0]["ItemType"].ToString();
                    else if (nonCountables != null) rewardParam = nonCountables[0].ToString();

                    if (RewardIsNotIgnored(creditReward, (rewardParam != null) ? rewardParam.ToString() : null))
                    {
                        if (DateTime.Now < expireTime)
                        {
                            MissionInfo alertInfo = new MissionInfo(jsonAlert["MissionInfo"]["faction"].ToString(),
                                jsonAlert["MissionInfo"]["missionType"].ToString(),
                                creditReward,
                                //If for whatever reason, an alert returns both countables and non-countables, currently only the countables will be returned.
                                //In addition, if an alert returns multiple different countables, only the first instance will be returned. This affects invasions as well!
                                rewardStr,
                                int.Parse((countables != null ? countables[0]["ItemCount"] : 1).ToString()),
                                int.Parse(jsonAlert["MissionInfo"]["minEnemyLevel"].ToString()),
                                int.Parse(jsonAlert["MissionInfo"]["maxEnemyLevel"].ToString()),
                                requiresArchwing);

                            currentAlert = new WarframeAlert(alertInfo, id, nodeName, startTime, expireTime);
                            _alertsList.Add(currentAlert);
#if DEBUG
                            Console.WriteLine("New Alert Event");
#endif
                        }
                    }
                }
                else
                {
                    if (currentAlert.ExpireTime < DateTime.Now)
                        _alertsList.Remove(currentAlert);
                }

                if ((currentAlert != null) && (currentAlert.ExpireTime > DateTime.Now))
                    resultAlerts.Add(currentAlert);
            }
            
            return _alertsList;
        }

        public IEnumerable<WarframeInvasion> GetInvasions()
        {
            JObject worldState = _scraper.ScrapeWorldState();
            var resultInvasions = new List<WarframeInvasion>();

            //Find Invasions
            foreach (var jsonInvasion in worldState["Invasions"])
            {
                WarframeInvasion currentInvasion = _invasionsList.Find(x => x.GUID == jsonInvasion["_id"]["$id"].ToString());

                if (currentInvasion == null)
                {
                    var id = jsonInvasion["_id"]["$id"].ToString();
                    var loc = jsonInvasion["Node"].ToString();

                    var attackerCountables = new JArray();
                    var defenderCountables = new JArray();

                    JToken attackerCountablesInfo = jsonInvasion["AttackerReward"];
                    JToken defenderCountablesInfo = jsonInvasion["DefenderReward"];

                    var attackerCredits = 0;
                    var defenderCredits = 0;

                    var attackersGiveReward = !attackerCountablesInfo.IsNullOrEmpty();
                    var defendersGiveReward = !defenderCountablesInfo.IsNullOrEmpty();

                    if (defendersGiveReward)
                    {
                        if (!defenderCountablesInfo["countedItems"].IsNullOrEmpty())
                            defenderCountables = (JArray)(jsonInvasion["DefenderReward"]["countedItems"]);
                        if (!defenderCountablesInfo["credits"].IsNullOrEmpty())
                            defenderCredits = int.Parse((jsonInvasion["DefenderReward"]["credits"]).ToString());
                    }

                    if (attackersGiveReward)
                    {
                        if (!attackerCountablesInfo["countedItems"].IsNullOrEmpty())
                            attackerCountables = (JArray)(jsonInvasion["AttackerReward"]["countedItems"]);
                        if (!attackerCountablesInfo["credits"].IsNullOrEmpty())
                            attackerCredits = int.Parse((jsonInvasion["AttackerReward"]["credits"]).ToString());
                    }

                    var attackerRewardStr = string.Empty;
                    var defenderRewardStr = string.Empty;
                    var nodeName = string.Empty;

                    using (var unit = new UnitOfWork(new WarframeDataContext()))
                    {
                        attackerRewardStr = (attackersGiveReward ? unit.WarframeItems.GetItemName(attackerCountables[0]["ItemType"].ToString()) : "");
                        defenderRewardStr = (defendersGiveReward ? unit.WarframeItems.GetItemName(defenderCountables[0]["ItemType"].ToString()) : "");

                        nodeName = unit.WFSolarNodes.GetNodeName(loc);
                    }

                    //Store mission information in variables so that we don't have to keep parsing the JSON
                    var attackerRewardParam = string.Empty;
                    if (attackersGiveReward)
                        attackerRewardParam = attackerCountables[0]["ItemType"].ToString();
                    var defenderRewardParam = string.Empty;
                    if (defendersGiveReward)
                        defenderRewardParam = defenderCountables[0]["ItemType"].ToString();

                    var attackerRewardQuantityParam = attackersGiveReward ? (attackerCountables[0]["ItemCount"] != null ? int.Parse(attackerCountables[0]["ItemCount"].ToString()) : 1) : 0;
                    var defenderRewardQuantityParam = defendersGiveReward ? (defenderCountables[0]["ItemCount"] != null ? int.Parse(defenderCountables[0]["ItemCount"].ToString()) : 1) : 0;

                    var goal = int.Parse(jsonInvasion["Goal"].ToString());
                    var progress = int.Parse(jsonInvasion["Count"].ToString());

                    if (System.Math.Abs(progress) < goal)
                    {
                        //Check attacker conditions
                        if (RewardIsNotIgnored(attackerCredits, itemURI: (attackerRewardParam ?? string.Empty).ToString(), itemQuantity: attackerRewardQuantityParam)
                            //Check defender conditions
                            || RewardIsNotIgnored(defenderCredits, itemURI: (defenderRewardParam ?? string.Empty).ToString(), itemQuantity: defenderRewardQuantityParam))
                        {
                            //Mission Info corresponds to the faction to fight against.
                            //JSON file has currently removed mission levels and mission types from the JSON file.
                            var attackerInfo = new MissionInfo(jsonInvasion["AttackerMissionInfo"]["faction"].ToString(),
                                string.Empty,
                                attackerCredits,
                                string.IsNullOrEmpty(attackerRewardStr) ? "" : attackerRewardStr,
                                attackerRewardQuantityParam,
                                0, 0,
                                false);

                            var defenderInfo = new MissionInfo(jsonInvasion["DefenderMissionInfo"]["faction"].ToString(),
                                string.Empty,
                                defenderCredits,
                                string.IsNullOrEmpty(defenderRewardStr) ? "" : defenderRewardStr,
                                defenderRewardQuantityParam,
                                0, 0,
                                false);

                            var secondsUntilStart = double.Parse(jsonInvasion["Activation"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                            var startTime = DateTime.Now.AddSeconds(secondsUntilStart);

                            currentInvasion = new WarframeInvasion(attackerInfo, defenderInfo, id, nodeName, startTime, int.Parse(jsonInvasion["Goal"].ToString()));
                            _invasionsList.Add(currentInvasion);
                        }
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("An Invasion was discarded due to its lack of rewards");
#endif
                    }
                }
                else
                {
                    if (currentInvasion.IsExpired())
                        _invasionsList.Remove(currentInvasion);
                }

                if (currentInvasion != null && !currentInvasion.IsExpired())
                {
                    currentInvasion.UpdateProgress(int.Parse(jsonInvasion["Count"].ToString()));
                    resultInvasions.Add(currentInvasion);
                }
            }
            
            return _invasionsList;
        }

        //Parse information about the faction construction projects
        public IEnumerable<WarframeInvasionConstruction> GetInvasionConstruction()
        {
            const string IDENTIFIER_PREFIX = "ProjectPct";
            JObject worldState = _scraper.ScrapeWorldState();
            var resultConstructionProjects = new List<WarframeInvasionConstruction>();

            var currentIteration = 0;
            //Find Projects
            foreach (var jsonInvasionConstructionProject in worldState["ProjectPct"])
            {
                var projectIdentifier = new StringBuilder(IDENTIFIER_PREFIX + currentIteration);
                WarframeInvasionConstruction currentConstructionProject = _constructionProjectsList.Find(x => x.GUID == projectIdentifier.ToString());
                var progress = double.Parse(jsonInvasionConstructionProject.ToString());

                if (currentConstructionProject == null)
                {
                    currentConstructionProject = new WarframeInvasionConstruction(projectIdentifier.ToString(), currentIteration, progress);
                    _constructionProjectsList.Add(currentConstructionProject);
#if DEBUG
                    Console.WriteLine("New Construction Project Event");
#endif
                }
                else
                {
                    if (currentConstructionProject.IsExpired())
                        _constructionProjectsList.Remove(currentConstructionProject);
                }

                if ((currentConstructionProject != null) && (!currentConstructionProject.IsExpired()))
                {
                    currentConstructionProject.UpdateProgress(progress);
                    resultConstructionProjects.Add(currentConstructionProject);
                }

                ++currentIteration;
            }
            
            return _constructionProjectsList;
        }

        public IEnumerable<WarframeVoidTrader> GetVoidTrader()
        {
            JObject worldState = _scraper.ScrapeWorldState();
            var resultVoidTraders = new List<WarframeVoidTrader>();

            foreach (var jsonTrader in worldState["VoidTraders"])
            {
                WarframeVoidTrader currentTrader = _voidTraders.Find(x => x.GUID == jsonTrader["_id"]["$id"].ToString());
                if (currentTrader == null)
                {
                    var id = jsonTrader["_id"]["$id"].ToString();
                    var loc = jsonTrader["Node"].ToString();

                    var secondsUntilStart = double.Parse(jsonTrader["Activation"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonTrader["Expiry"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    var startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    var expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);

                    if (DateTime.Now < expireTime)
                    {
                        var nodeName = loc;
                        var itemName = string.Empty;

                        using (var unit = new UnitOfWork(new WarframeDataContext()))
                        {
                            nodeName = unit.WFSolarNodes.GetNodeName(loc);
                        }

                        currentTrader = new WarframeVoidTrader(id, nodeName, startTime, expireTime);
                        _voidTraders.Add(currentTrader);
                    }

                    if (currentTrader != null)
                    {
                        JToken traderInventory = jsonTrader["Manifest"];
                        if (!traderInventory.IsNullOrEmpty())
                        {
                            foreach (var i in traderInventory)
                            {
                                using (var unit = new UnitOfWork(new WarframeDataContext()))
                                {
                                    currentTrader.AddTraderItem(unit.WarframeItems.GetItemName(i["ItemType"].ToString()), int.Parse(i["RegularPrice"].ToString()), int.Parse(i["PrimePrice"].ToString()));
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (currentTrader.ExpireTime < DateTime.Now)
                        _voidTraders.Remove(currentTrader);
                }

                if ((currentTrader != null) && (currentTrader.ExpireTime > DateTime.Now))
                    resultVoidTraders.Add(currentTrader);
            }
            return _voidTraders;
        }

        public IEnumerable<WarframeVoidFissure> GetVoidFissures()
        {
            JObject worldState = _scraper.ScrapeWorldState();
            var resultVoidFissures = new List<WarframeVoidFissure>();

            //Find Alerts
            foreach (var jsonFissure in worldState["ActiveMissions"])
            {
                WarframeVoidFissure currentVoidFissure = _voidFissures.Find(x => x.GUID == jsonFissure["_id"]["$id"].ToString());

                if (currentVoidFissure == null)
                {
                    var id = jsonFissure["_id"]["$id"].ToString();
                    var loc = jsonFissure["Node"].ToString();

                    var secondsUntilStart = double.Parse(jsonFissure["Activation"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonFissure["Expiry"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    var startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    var expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);

                    var nodeName = loc;
                    var faction = string.Empty;
                    var missionType = string.Empty;
                    var fissure = string.Empty;
                    var minLevel = 0;
                    var maxLevel = 0;
                    var archwingRequired = false;

                    using (var unit = new UnitOfWork(new WarframeDataContext()))
                    {
                        nodeName = unit.WFSolarNodes.GetNodeName(loc);
                        faction = unit.WFSolarNodes.GetFaction(loc);
                        missionType = unit.WFSolarNodes.GetMissionType(loc);
                        minLevel = unit.WFSolarNodes.GetMinLevel(loc);
                        maxLevel = unit.WFSolarNodes.GetMaxLevel(loc);
                        fissure = unit.WFVoidFissures.GetFissureName(jsonFissure["Modifier"].ToString());
                        archwingRequired = unit.WFSolarNodes.ArchwingRequired(loc);
                    }

                    if (DateTime.Now < expireTime)
                    {
                        var fissureInfo = new MissionInfo(faction, missionType, 0, fissure, 0, minLevel, maxLevel, archwingRequired);

                        currentVoidFissure = new WarframeVoidFissure(fissureInfo, id, nodeName, startTime, expireTime);
                        _voidFissures.Add(currentVoidFissure);
#if DEBUG
                        Console.WriteLine("New Fissure Event");
#endif
                    }
                }
                else
                {
                    if (currentVoidFissure.ExpireTime < DateTime.Now)
                        _voidFissures.Remove(currentVoidFissure);
                }

                if ((currentVoidFissure != null) && (currentVoidFissure.ExpireTime > DateTime.Now))
                    resultVoidFissures.Add(currentVoidFissure);
            }
            return _voidFissures;
        }

        public IEnumerable<WarframeSortie> GetSorties()
        {
            JObject worldState = _scraper.ScrapeWorldState();
            var resultSorties = new List<WarframeSortie>();

            //Find Sorties
            foreach (var jsonSortie in worldState["Sorties"])
            {
                //Check if the sortie has already being tracked
                WarframeSortie currentSortie = _sortieList.Find(x => x.GUID == jsonSortie["_id"]["$id"].ToString());

                if (currentSortie == null)
                {
                    var id = jsonSortie["_id"]["$id"].ToString();

                    //Variant details
                    var varDests = new List<string>();
                    var varMissions = new List<MissionInfo>();
                    var varConditions = new List<string>();

                    var secondsUntilStart = double.Parse(jsonSortie["Activation"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonSortie["Expiry"]["sec"].ToString()) - double.Parse(worldState["Time"].ToString());
                    var startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    var expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);

                    //If this sortie doesn't exist in the current list, then loop through the variant node to get mission info for all variants
                    foreach (var variant in jsonSortie["Variants"])
                    {
                        using (var unit = new UnitOfWork(new WarframeDataContext()))
                        {
                            var loc = variant["node"].ToString();
                            varDests.Add(unit.WFSolarNodes.GetNodeName(loc));
                            varConditions.Add(unit.WFSorties.GetCondition(int.Parse(variant["modifierIndex"].ToString())));

                            //Mission type varies depending on the region
                            var regionIndex = int.Parse(variant["regionIndex"].ToString());
                            var missionIndex = int.Parse(variant["missionIndex"].ToString());
                            var bossIndex = int.Parse(variant["bossIndex"].ToString());

                            string regionName = unit.WFSorties.GetRegion(regionIndex);
                            string missionName = unit.WFSorties.GetMissionType(missionIndex, regionIndex);

                            var varMission = new MissionInfo(unit.WFSorties.GetFaction(bossIndex), missionName,
                                    0, unit.WFSorties.GetBoss(bossIndex), 0, 0, 0, false);

                            varMissions.Add(varMission);
                        }
                    }

                    if (DateTime.Now < expireTime)
                    {
                        currentSortie = new WarframeSortie(varMissions, id, varDests, varConditions, startTime, expireTime);
                        _sortieList.Add(currentSortie);
#if DEBUG
                        Console.WriteLine("New Sortie Event");
#endif
                    }
                }
                else
                {
                    if (currentSortie.ExpireTime < DateTime.Now)
                        _sortieList.Remove(currentSortie);
                }

                if ((currentSortie != null) && (currentSortie.ExpireTime > DateTime.Now))
                    resultSorties.Add(currentSortie);
            }
            return _sortieList;
        }

        public WarframeTimeCycleInfo GetTimeCycle()
        {
            JObject worldState = _scraper.ScrapeWorldState();

            var currentTime = int.Parse(worldState["Time"].ToString());
            var cycleInfo = new WarframeTimeCycleInfo(currentTime);
            return cycleInfo;
        }

        #endregion

        private bool RewardIsNotIgnored(int credits = 0, string itemURI = "", int itemQuantity = 1)
        {
            const string CREDITS_URI = "/Lotus/Language/Menu/Monies";
            var result = true;
            using (var unit = new UnitOfWork(new WarframeDataContext()))
            {
                if (string.IsNullOrEmpty(itemURI))
                {
                    //If there is no item reward, we check for credit value
                    var creds = unit.WarframeItems.GetItemByURI(CREDITS_URI);
                    int min = unit.WFDatabaseOptions.GetItemMinimum(creds);

                    result = !(credits < min);
                }
                else
                {
                    //Check for item min in the same way we check for credits
                    var item = unit.WarframeItems.GetItemByURI(itemURI);
                    int min = unit.WFDatabaseOptions.GetItemMinimum(item);

                    result = !(itemQuantity < min);
                }
            }

            return result;
        }

        //Check if the event started recently
        public bool IsEventNew(WarframeEvent warframeEvent)
        {
            var timeEventIsNotNew = warframeEvent.StartTime.AddSeconds(SECONDS_UNTIL_EVENT_NOT_NEW);
            return ((DateTime.Now >= warframeEvent.StartTime) && (DateTime.Now < timeEventIsNotNew));
        }
    }
}
