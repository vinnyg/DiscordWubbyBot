using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net;
using DiscordSharpTest.Events;
using WarframeDatabaseNet;
using WarframeDatabaseNet.Persistence;
using DiscordSharpTest.WarframeEvents;
using System.Configuration;

namespace DiscordSharpTest
{
    //This class parses a JSON file and raises events regarding the contents
    public class WarframeJSONScraper
    {
        //TODO: Break this class down into two; a dedicated scraper class and JSON parser class
        //Consider responsibility of raising events
        private const int SECONDS_PER_DAY_CYCLE = 14400;
        public List<WarframeAlert> AlertsList { get; private set; } = new List<WarframeAlert>();
        public List<WarframeInvasion> InvasionsList { get; private set; } = new List<WarframeInvasion>();
        public List<WarframeInvasionConstruction> ConstructionProjectsList { get; private set; } = new List<WarframeInvasionConstruction>();
        public List<WarframeVoidTrader> VoidTraders { get; private set; } = new List<WarframeVoidTrader>();
        public List<WarframeVoidFissure> VoidFissures { get; private set; } = new List<WarframeVoidFissure>();
        public List<WarframeSortie> SortieList { get; private set; } = new List<WarframeSortie>();
        public bool IsRunning { get; set; }

        //Store the JSON file
        private JObject _worldState { get; set; }
        private Timer _eventUpdateTimer { get; set; }
        private List<WarframeAlert> _newAlerts { get; set; } = new List<WarframeAlert>();
        private List<WarframeInvasion> _newInvasions { get; set; } = new List<WarframeInvasion>();
        private List<WarframeInvasionConstruction> _newProjects { get; set; } = new List<WarframeInvasionConstruction>();
        private List<WarframeVoidFissure> _newVoidFissures { get; set; } = new List<WarframeVoidFissure>();
        private List<WarframeSortie> _newSorties { get; set; } = new List<WarframeSortie>();

        #region Events
        public event EventHandler<WarframeAlertScrapedArgs> AlertScraped;
        public event EventHandler<WarframeInvasionScrapedArgs> InvasionScraped;
        public event EventHandler<WarframeConstructionProjectsScrapedArgs> ConstructionProjectsScraped;
        public event EventHandler<WarframeVoidTraderScrapedArgs> VoidTraderScraped;
        public event EventHandler<WarframeVoidFissureScrapedArgs> VoidFissureScraped;
        public event EventHandler<WarframeSortieScrapedArgs> SortieScraped;
        public event EventHandler<DayCycleTimeScrapedArgs> DayCycleScraped;
        public event EventHandler<WarframeAlertExpiredArgs> AlertExpired;
        public event EventHandler<WarframeVoidFissureExpiredArgs> VoidFissureExpired;
        public event EventHandler<WarframeSortieExpiredArgs> SortieExpired;
        #endregion

        public WarframeJSONScraper()
        {
        }

        //Start scraping and processing data
        public void Start(int updateIntervalInMinutes = 1)
        {
            if (!IsRunning)
            {
                //Establish event update interval (every minute)
                _eventUpdateTimer = new Timer((e) =>
                {
                    ScrapeWorldState();
                    ParseJsonEvents();
                }, null, 0, (int)(TimeSpan.FromMinutes((double)updateIntervalInMinutes).TotalMilliseconds));

                IsRunning = true;
            }
        }

        //Stop the scraper
        public void Stop()
        {
            if (IsRunning)
            {
                _eventUpdateTimer.Dispose();
                IsRunning = false;
            }
        }

        //Download Warframe content information
        private void ScrapeWorldState()
        {
            const int NUMBER_OF_RETRIES = 3;
            const int RETRY_DELAY_IN_MILLISECONDS = 2000;

            using (WebClient wc = new WebClient())
            {
                //If a failure to download the content occurs, retry two more times
                for (int attempt = 0; attempt < NUMBER_OF_RETRIES; ++attempt)
                {
                    try
                    {
                        _worldState = JObject.Parse(wc.DownloadString(ConfigurationManager.AppSettings["WarframeContentURL"]));
                        attempt = NUMBER_OF_RETRIES;
                        return;
                    }
                    catch (WebException)
                    {
                        if (attempt >= NUMBER_OF_RETRIES)
                            throw;

                        Thread.Sleep(RETRY_DELAY_IN_MILLISECONDS);
                    }
                }
            }
        }

        private void ParseJsonEvents()
        {
            ParseAlerts();
            ParseInvasions();
            ParseInvasionConstruction();
            ParseVoidFissures();
            ParseSorties();
            ParseVoidTrader();
            ParseTimeCycle();
        }

        #region ParseJSONMethods
        private void ParseAlerts()
        {
            _newAlerts.Clear();

            //Find Alerts
            foreach (var jsonAlert in _worldState["Alerts"])
            {
                WarframeAlert currentAlert = AlertsList.Find(x => x.GUID == jsonAlert["_id"]["$id"].ToString());

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
                    
                    var secondsUntilStart = double.Parse(jsonAlert["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonAlert["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
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
                            AlertsList.Add(currentAlert);
                            _newAlerts.Add(currentAlert);
#if DEBUG
                            Console.WriteLine("New Alert Event");
#endif
                        }
                    }
                }
                else
                {
                    if (currentAlert.ExpireTime < DateTime.Now)
                        AlertsList.Remove(currentAlert);
                }

                if ((currentAlert != null) && (currentAlert.ExpireTime > DateTime.Now))
                    CreateNewAlertReceivedEvent(currentAlert);
                else
                    CreateAlertExpiredEvent(currentAlert);
            }
        }

        private void ParseInvasions()
        {
            _newInvasions.Clear();

            //Find Invasions
            foreach (var jsonInvasion in _worldState["Invasions"])
            {
                WarframeInvasion currentInvasion = InvasionsList.Find(x => x.GUID == jsonInvasion["_id"]["$id"].ToString());

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
                        if (RewardIsNotIgnored(attackerCredits, itemURI: (attackerRewardParam ?? string.Empty).ToString(),itemQuantity:attackerRewardQuantityParam)
                            //Check defender conditions
                            || RewardIsNotIgnored(defenderCredits, itemURI: (defenderRewardParam ?? string.Empty).ToString(), itemQuantity:defenderRewardQuantityParam))
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

                            var secondsUntilStart = double.Parse(jsonInvasion["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                            var startTime = DateTime.Now.AddSeconds(secondsUntilStart);

                            currentInvasion = new WarframeInvasion(attackerInfo, defenderInfo, id, nodeName, startTime, int.Parse(jsonInvasion["Goal"].ToString()));
                            InvasionsList.Add(currentInvasion);
                            _newInvasions.Add(currentInvasion);
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
                        InvasionsList.Remove(currentInvasion);
                }

                if (currentInvasion != null && !currentInvasion.IsExpired())
                {
                    currentInvasion.UpdateProgress(int.Parse(jsonInvasion["Count"].ToString()));
                    CreateNewInvasionReceivedEvent(currentInvasion);
                }
            }
        }

        //Parse information about the faction construction projects
        private void ParseInvasionConstruction()
        {
            const string IDENTIFIER_PREFIX = "ProjectPct";

            _newProjects.Clear();

            var currentIteration = 0;
            //Find Projects
            foreach (var jsonInvasionConstructionProject in _worldState["ProjectPct"])
            {
                var projectIdentifier = new StringBuilder(IDENTIFIER_PREFIX + currentIteration);
                WarframeInvasionConstruction currentConstructionProject = ConstructionProjectsList.Find(x => x.GUID == projectIdentifier.ToString());
                var progress = double.Parse(jsonInvasionConstructionProject.ToString());

                if (currentConstructionProject == null)
                {
                    currentConstructionProject = new WarframeInvasionConstruction(projectIdentifier.ToString(), currentIteration, progress);
                    ConstructionProjectsList.Add(currentConstructionProject);
                    _newProjects.Add(currentConstructionProject);
#if DEBUG
                    Console.WriteLine("New Construction Project Event");
#endif
                }
                else
                {
                    if (currentConstructionProject.IsExpired())
                        ConstructionProjectsList.Remove(currentConstructionProject);
                }

                if ((currentConstructionProject != null) && (!currentConstructionProject.IsExpired()))
                {
                    currentConstructionProject.UpdateProgress(progress);
                    CreateConstructionProjectReceivedEvent(currentConstructionProject);
                }

                ++currentIteration;
            }
        }

        private void ParseVoidTrader()
        {
            foreach (var jsonTrader in _worldState["VoidTraders"])
            {
                WarframeVoidTrader currentTrader = VoidTraders.Find(x => x.GUID == jsonTrader["_id"]["$id"].ToString());
                if (currentTrader == null)
                {
                    var id = jsonTrader["_id"]["$id"].ToString();
                    var loc = jsonTrader["Node"].ToString();

                    var secondsUntilStart = double.Parse(jsonTrader["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonTrader["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
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
                        VoidTraders.Add(currentTrader);

                        JToken traderInventory = jsonTrader["Manifest"];
                        if (traderInventory != null)
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
                        VoidTraders.Remove(currentTrader);
                }

                if ((currentTrader != null) && (currentTrader.ExpireTime > DateTime.Now))
                    CreateNewVoidTraderReceivedEvent(currentTrader);
            }
        }

        private void ParseVoidFissures()
        {
            _newVoidFissures.Clear();

            //Find Alerts
            foreach (var jsonFissure in _worldState["ActiveMissions"])
            {
                WarframeVoidFissure currentVoidFissure = VoidFissures.Find(x => x.GUID == jsonFissure["_id"]["$id"].ToString());

                if (currentVoidFissure == null)
                {
                    var id = jsonFissure["_id"]["$id"].ToString();
                    var loc = jsonFissure["Node"].ToString();

                    var secondsUntilStart = double.Parse(jsonFissure["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonFissure["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
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
                        VoidFissures.Add(currentVoidFissure);
                        _newVoidFissures.Add(currentVoidFissure);
#if DEBUG
                        Console.WriteLine("New Fissure Event");
#endif
                    }
                }
                else
                {
                    if (currentVoidFissure.ExpireTime < DateTime.Now)
                        VoidFissures.Remove(currentVoidFissure);
                }

                if ((currentVoidFissure != null) && (currentVoidFissure.ExpireTime > DateTime.Now))
                    CreateNewVoidFissureReceivedEvent(currentVoidFissure);
                else
                    CreateVoidFissureExpiredEvent(currentVoidFissure);
            }
        }

        private void ParseSorties()
        {
            _newSorties.Clear();

            //Find Sorties
            foreach (var jsonSortie in _worldState["Sorties"])
            {
                //Check if the sortie has already being tracked
                WarframeSortie currentSortie = SortieList.Find(x => x.GUID == jsonSortie["_id"]["$id"].ToString());

                if (currentSortie == null)
                {
                    var id = jsonSortie["_id"]["$id"].ToString();

                    //Variant details
                    var varDests = new List<string>();
                    var varMissions = new List<MissionInfo>();
                    var varConditions = new List<string>();

                    var secondsUntilStart = double.Parse(jsonSortie["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    var secondsUntilExpire = double.Parse(jsonSortie["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
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
                        SortieList.Add(currentSortie);
                        _newSorties.Add(currentSortie);
#if DEBUG
                        Console.WriteLine("New Sortie Event");
#endif
                    }
                }
                else
                {
                    if (currentSortie.ExpireTime < DateTime.Now)
                        SortieList.Remove(currentSortie);
                }

                if ((currentSortie != null) && (currentSortie.ExpireTime > DateTime.Now))
                    CreateNewSortieReceivedEvent(currentSortie);
                else
                    CreateSortieExpiredEvent(currentSortie);
            }
        }

        private void ParseTimeCycle()
        {
            var currentTime = int.Parse(_worldState["Time"].ToString());
            var cycleInfo = new WarframeTimeCycleInfo(currentTime);
            CreateDayCycleUpdateReceivedEvent(cycleInfo);
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

        private void CreateNewAlertReceivedEvent(WarframeAlert newAlert)
        {
            // Make a temporary copy of the event to potential
            // race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<WarframeAlertScrapedArgs> handler = AlertScraped;

            var e = new WarframeAlertScrapedArgs(newAlert);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }

        private void CreateNewInvasionReceivedEvent(WarframeInvasion newInvasion)
        {
            EventHandler<WarframeInvasionScrapedArgs> handler = InvasionScraped;

            var e = new WarframeInvasionScrapedArgs(newInvasion);

            if (handler != null)
                handler(this, e);
        }

        private void CreateConstructionProjectReceivedEvent(WarframeInvasionConstruction newConstruction)
        {
            EventHandler<WarframeConstructionProjectsScrapedArgs> handler = ConstructionProjectsScraped;

            var e = new WarframeConstructionProjectsScrapedArgs(newConstruction);

            if (handler != null)
                handler(this, e);
        }

        private void CreateNewVoidTraderReceivedEvent(WarframeVoidTrader newTrader)
        {
            EventHandler<WarframeVoidTraderScrapedArgs> handler = VoidTraderScraped;

            var e = new WarframeVoidTraderScrapedArgs(newTrader);

            if (handler != null)
                handler(this, e);
        }

        private void CreateNewVoidFissureReceivedEvent(WarframeVoidFissure newFissure)
        {
            EventHandler<WarframeVoidFissureScrapedArgs> handler = VoidFissureScraped;

            var e = new WarframeVoidFissureScrapedArgs(newFissure);

            if (handler != null)
                handler(this, e);
        }

        private void CreateNewSortieReceivedEvent(WarframeSortie newSortie)
        {
            EventHandler<WarframeSortieScrapedArgs> handler = SortieScraped;

            var e = new WarframeSortieScrapedArgs(newSortie);

            if (handler != null)
                handler(this, e);
        }

        private void CreateDayCycleUpdateReceivedEvent(WarframeTimeCycleInfo cycleInfo)
        {
            EventHandler<DayCycleTimeScrapedArgs> handler = DayCycleScraped;

            var e = new DayCycleTimeScrapedArgs(cycleInfo);

            if (handler != null)
                handler(this, e);
        }

        private void CreateAlertExpiredEvent(WarframeAlert expiredAlert, string messageID = "")
        {
            EventHandler<WarframeAlertExpiredArgs> handler = AlertExpired;

            var e = new WarframeAlertExpiredArgs(expiredAlert, messageID);

            if (handler != null)
                handler(this, e);
        }

        private void CreateVoidFissureExpiredEvent(WarframeVoidFissure expiredFissure, string messageID = "")
        {
            EventHandler<WarframeVoidFissureExpiredArgs> handler = VoidFissureExpired;

            var e = new WarframeVoidFissureExpiredArgs(expiredFissure, messageID);

            if (handler != null)
                handler(this, e);
        }

        private void CreateSortieExpiredEvent(WarframeSortie expiredSortie, string messageID = "")
        {
            EventHandler<WarframeSortieExpiredArgs> handler = SortieExpired;

            var e = new WarframeSortieExpiredArgs(expiredSortie, messageID);

            if (handler != null)
                handler(this, e);
        }

        public bool IsAlertNew(WarframeAlert alert)
        {
            return (_newAlerts.Exists( x => x.GUID == alert.GUID));
        }

        public bool IsInvasionNew(WarframeInvasion invasion)
        {
            return (_newInvasions.Exists(x => x.GUID == invasion.GUID));
        }
    }
}
