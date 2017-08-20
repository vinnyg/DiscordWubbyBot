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
    //Parses the contents of a JSON file for Warframe events
    public class WarframeEventInformationParser
    {
        public WarframeEventInformationParser() : this(new WarframeJSONScraper())
        {

        }

        public WarframeEventInformationParser(IWarframeJSONScraper scraper)
        {
            _scraper = scraper;
        }

        private const int SECONDS_PER_DAY_CYCLE = 14400;
        private const int TIME_TO_LONG_MULTIPLIER = 1000;

        private List<WarframeAlert> _alertsList = new List<WarframeAlert>();
        private List<WarframeInvasion> _invasionsList = new List<WarframeInvasion>();
        private List<WarframeInvasionConstruction> _constructionProjectsList = new List<WarframeInvasionConstruction>();
        private List<WarframeVoidTrader> _voidTraders = new List<WarframeVoidTrader>();
        private List<WarframeVoidFissure> _voidFissures = new List<WarframeVoidFissure>();
        private List<WarframeSortie> _sortieList = new List<WarframeSortie>();
        private List<WarframeAcolyte> _acolytesList = new List<WarframeAcolyte>();
        private IWarframeJSONScraper _scraper;

        #region ParseJSONMethods
        public IEnumerable<WarframeAlert> GetAlerts()
        {
            JObject worldState = _scraper.ScrapeWorldState();
            var resultAlerts = new List<WarframeAlert>();

            //Find Alerts
            foreach (var jsonAlert in worldState["Alerts"])
            {
                WarframeAlert currentAlert = _alertsList.Find(x => x.GUID == jsonAlert["_id"]["$oid"].ToString());

                if (currentAlert == null)
                {
                    string id = jsonAlert["_id"]["$oid"].ToString();
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

                    var millisecondsUntilStart = long.Parse(jsonAlert["Activation"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    var millisecondsUntilExpire = long.Parse(jsonAlert["Expiry"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    var startTime = DateTime.Now.AddMilliseconds(millisecondsUntilStart);
                    var expireTime = DateTime.Now.AddMilliseconds(millisecondsUntilExpire);

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

                _alertsList.RemoveAll(x => x.ExpireTime < DateTime.Now);

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
                WarframeInvasion currentInvasion = _invasionsList.Find(x => x.GUID == jsonInvasion["_id"]["$oid"].ToString());

                if (currentInvasion == null)
                {
                    var id = jsonInvasion["_id"]["$oid"].ToString();
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

                            var secondsUntilStart = long.Parse(jsonInvasion["Activation"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                            var startTime = DateTime.Now.AddMilliseconds(secondsUntilStart);
                            
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

                _invasionsList.RemoveAll(x => x.IsExpired());

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
                WarframeVoidTrader currentTrader = _voidTraders.Find(x => x.GUID == jsonTrader["_id"]["$oid"].ToString());
                if (currentTrader == null)
                {
                    var id = jsonTrader["_id"]["$oid"].ToString();
                    var loc = jsonTrader["Node"].ToString();

                    var millisecondsUntilStart = long.Parse(jsonTrader["Activation"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    var millisecondsUntilExpire = long.Parse(jsonTrader["Expiry"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    var startTime = DateTime.Now.AddMilliseconds(millisecondsUntilStart);
                    var expireTime = DateTime.Now.AddMilliseconds(millisecondsUntilExpire);

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

                _voidTraders.RemoveAll(x => x.ExpireTime < DateTime.Now);

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
                WarframeVoidFissure currentVoidFissure = _voidFissures.Find(x => x.GUID == jsonFissure["_id"]["$oid"].ToString());

                if (currentVoidFissure == null)
                {
                    var id = jsonFissure["_id"]["$oid"].ToString();
                    var loc = jsonFissure["Node"].ToString();

                    var millisecondsUntilStart = long.Parse(jsonFissure["Activation"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    var millisecondsUntilExpire = long.Parse(jsonFissure["Expiry"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    var startTime = DateTime.Now.AddMilliseconds(millisecondsUntilStart);
                    var expireTime = DateTime.Now.AddMilliseconds(millisecondsUntilExpire);

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

                _voidFissures.RemoveAll(x => x.ExpireTime < DateTime.Now);

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
                WarframeSortie currentSortie = _sortieList.Find(x => x.GUID == jsonSortie["_id"]["$oid"].ToString());

                if (currentSortie == null)
                {
                    var id = jsonSortie["_id"]["$oid"].ToString();

                    //Variant details
                    var varDests = new List<string>();
                    var varMissions = new List<MissionInfo>();
                    var varConditions = new List<string>();

                    var millisecondsUntilStart = long.Parse(jsonSortie["Activation"]["$date"]["$numberLong"].ToString()) - long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER;
                    var millisecondsUntilExpire = long.Parse(jsonSortie["Expiry"]["$date"]["$numberLong"].ToString()) - long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER;
                    var startTime = DateTime.Now.AddMilliseconds(millisecondsUntilStart);
                    var expireTime = DateTime.Now.AddMilliseconds(millisecondsUntilExpire);

                    //If this sortie doesn't exist in the current list, then loop through the variant node to get mission info for all variants
                    foreach (var variant in jsonSortie["Variants"])
                    {
                        using (var unit = new UnitOfWork(new WarframeDataContext()))
                        {
                            var loc = variant["node"].ToString();
                            varDests.Add(unit.WFSolarNodes.GetNodeName(loc));
                            varConditions.Add(unit.WFSorties.GetCondition(variant["modifierType"].ToString()));

                            //Mission type varies depending on the region
                            var sortieBossID = jsonSortie["Boss"].ToString();
                            string missionName = variant["missionType"].ToString();

                            var varMission = new MissionInfo(unit.WFSorties.GetFaction(sortieBossID), missionName,
                                    0, unit.WFSorties.GetBoss(sortieBossID), 0, 0, 0, false);

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

                _sortieList.RemoveAll(x => x.ExpireTime < DateTime.Now);

                if ((currentSortie != null) && (currentSortie.ExpireTime > DateTime.Now))
                    resultSorties.Add(currentSortie);
            }
            return _sortieList;
        }

        public WarframeTimeCycleInfo GetTimeCycle()
        {
            JObject worldState = _scraper.ScrapeWorldState();

            var currentTime = long.Parse(worldState["Time"].ToString());
            var cycleInfo = new WarframeTimeCycleInfo(currentTime);
            return cycleInfo;
        }

        public IEnumerable<WarframeAcolyte> GetAcolytes()
        {
            JObject worldState = _scraper.ScrapeWorldState();
            var resultAcolytes = new List<WarframeAcolyte>();

            //Find Alerts
            foreach (var jsonAcolyte in worldState["PersistentEnemies"])
            {
                WarframeAcolyte currentAcolyte = _acolytesList.Find(x => x.GUID == jsonAcolyte["_id"]["$oid"].ToString());

                //We want to know this information regardless of whether or not the acolyte has been previously discovered
                var jsonAcolyteName = jsonAcolyte["AgentType"].ToString();
                var loc = jsonAcolyte["LastDiscoveredLocation"].ToString();

                var nodeName = loc;
                var acolyteName = jsonAcolyteName;
                var acolyteHealth = float.Parse(jsonAcolyte["HealthPercent"].ToString());
                var isDiscovered = bool.Parse(jsonAcolyte["Discovered"].ToString());

                using (var unit = new UnitOfWork(new WarframeDataContext()))
                {
                    acolyteName = unit.WFEnemies.GetNameByURI(jsonAcolyteName);
                    nodeName = unit.WFSolarNodes.GetNodeName(loc);
                }

                if (currentAcolyte == null)
                {
                    var id = jsonAcolyte["_id"]["$oid"].ToString();
                    var lastDiscoveredTime = jsonAcolyte["LastDiscoveredTime"]["$date"]["$numberLong"].ToString();
                    var regionIndex = isDiscovered ? int.Parse(jsonAcolyte["Region"].ToString()) : -1;


                    //Loot - Can be countable (Alertium etc.) or single (Blueprints) items
                    //JToken countables = (jsonAcolyte["MissionInfo"]["missionReward"]["countedItems"]),
                    //    nonCountables = (jsonAcolyte["MissionInfo"]["missionReward"]["items"]);

                    //var rewardStr = string.Empty;
                    

                    //var secondsUntilStart = long.Parse(jsonAcolyte["Activation"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    //var secondsUntilExpire = long.Parse(jsonAcolyte["Expiry"]["$date"]["$numberLong"].ToString()) - (long.Parse(worldState["Time"].ToString()) * TIME_TO_LONG_MULTIPLIER);
                    //var startTime = DateTime.Now.AddSeconds(secondsUntilStart / TIME_TO_LONG_MULTIPLIER);
                    //var expireTime = DateTime.Now.AddSeconds(secondsUntilExpire / TIME_TO_LONG_MULTIPLIER);

                    //var creditReward = int.Parse(jsonAcolyte["MissionInfo"]["missionReward"]["credits"].ToString());
                    //var reqArchwingData = jsonAcolyte["MissionInfo"]["archwingRequired"];
                    //bool requiresArchwing = reqArchwingData != null ? bool.Parse(reqArchwingData.ToString()) : false;

                    //JToken rewardParam = null;
                    //if (countables != null) rewardParam = countables[0]["ItemType"].ToString();
                    //else if (nonCountables != null) rewardParam = nonCountables[0].ToString();

                    //if (RewardIsNotIgnored(creditReward, (rewardParam != null) ? rewardParam.ToString() : null))
                    if (acolyteHealth > .0f)
                    {
                        /*MissionInfo alertInfo = new MissionInfo(jsonAcolyte["MissionInfo"]["faction"].ToString(),
                            jsonAcolyte["MissionInfo"]["missionType"].ToString(),
                            creditReward,
                            //If for whatever reason, an alert returns both countables and non-countables, currently only the countables will be returned.
                            //In addition, if an alert returns multiple different countables, only the first instance will be returned. This affects invasions as well!
                            rewardStr,
                            int.Parse((countables != null ? countables[0]["ItemCount"] : 1).ToString()),
                            int.Parse(jsonAcolyte["MissionInfo"]["minEnemyLevel"].ToString()),
                            int.Parse(jsonAcolyte["MissionInfo"]["maxEnemyLevel"].ToString()),
                            requiresArchwing);*/

                        currentAcolyte = new WarframeAcolyte(id, acolyteName, nodeName, acolyteHealth, regionIndex, isDiscovered);
                        _acolytesList.Add(currentAcolyte);
#if DEBUG
                        Console.WriteLine("New Acolyte Event");
#endif
                    }
                }

                _acolytesList.RemoveAll(x => x.Health <= .0f);

                if ((currentAcolyte != null) && (currentAcolyte.Health > .0f))
                {
                    currentAcolyte.IsDiscovered = isDiscovered;
                    currentAcolyte.UpdateLocation(nodeName);
                    currentAcolyte.UpdateHealth(acolyteHealth);
                    resultAcolytes.Add(currentAcolyte);
                }
            }

            return _acolytesList;
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
    }
}
