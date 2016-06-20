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

namespace DiscordSharpTest
{
    class WarframeEventsContainer
    {
        public List<WarframeAlert> AlertsList { get; private set; }
        public List<WarframeInvasion> InvasionsList { get; private set; }
        public List<WarframeVoidTrader> VoidTraders { get; private set; }

        //private XDocument _rssFeed { get; set; }
        private JObject _worldState { get; set; }

        private Timer _eventUpdateInterval { get; set; }

        private bool isRunning;

        private WarframeDataMapper wfDataMapper;

        //When an alert is read more than once, it will be added to this list. Alerts in this list are no longer new.
        private List<WarframeAlert> NewAlerts;
        private List<WarframeInvasion> NewInvasions;

        #region Events
        public event EventHandler<WarframeAlertScrapedArgs> AlertScraped;
        public event EventHandler<WarframeInvasionScrapedArgs> InvasionScraped;
        public event EventHandler<WarframeVoidTraderScrapedArgs> VoidTraderScraped;
        public event EventHandler<WarframeAlertExpiredArgs> AlertExpired;
        public event EventHandler<ExistingAlertFoundArgs> ExistingAlertFound;
        #endregion

        public WarframeEventsContainer()
        {
            AlertsList = new List<WarframeAlert>();
            InvasionsList = new List<WarframeInvasion>();
            VoidTraders = new List<WarframeVoidTrader>();
            NewAlerts = new List<WarframeAlert>();
            NewInvasions = new List<WarframeInvasion>();
            wfDataMapper = new WarframeDataMapper();
        }

        public void Start()
        {
            if (!isRunning)
            {
                //Establish event update interval (every minute)
                _eventUpdateInterval = new Timer((e) =>
                {
                    ScrapeWorldState();
                    ParseJsonEvents();
                }, null, 0, (int)(TimeSpan.FromMinutes(1.0).TotalMilliseconds));

                isRunning = true;
            }
        }

        private void ScrapeWorldState()
        {
            using (WebClient wc = new WebClient())
            {
                //_rssFeed = XDocument.Parse(wc.DownloadString("http://content.warframe.com/dynamic/rss.php"));
                _worldState = JObject.Parse(wc.DownloadString("http://content.warframe.com/dynamic/worldState.php"));
            }
        }

        private void ParseAlerts()
        {
            NewAlerts.Clear();

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

                    string rewardStr = (countables != null ?
                        wfDataMapper.GetItemName(countables[0]["ItemType"].ToString()) :
                        (nonCountables != null ? wfDataMapper.GetItemName(nonCountables[0].ToString()) : ""));

                    double secondsUntilStart = double.Parse(jsonAlert["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    double secondsUntilExpire = double.Parse(jsonAlert["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    DateTime startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    DateTime expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);

                    int creditReward = int.Parse(jsonAlert["MissionInfo"]["missionReward"]["credits"].ToString());

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
                                int.Parse(jsonAlert["MissionInfo"]["maxEnemyLevel"].ToString()));

                            currentAlert = new WarframeAlert(alertInfo, id, wfDataMapper.GetNodeName(loc), startTime, expireTime);
                            AlertsList.Add(currentAlert);
                            NewAlerts.Add(currentAlert);
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
                    CreateAlertExpiredEvent(currentAlert, "");
            }
        }

        private void ParseInvasions()
        {
            NewInvasions.Clear();

            //Find Invasions
            foreach (var jsonInvasion in _worldState["Invasions"])
            {
                WarframeInvasion currentInvasion = InvasionsList.Find(x => x.GUID == jsonInvasion["_id"]["$id"].ToString());

                if (currentInvasion == null)
                {
                    string id = jsonInvasion["_id"]["$id"].ToString();
                    string loc = jsonInvasion["Node"].ToString();

                    JToken attackerCountables = (jsonInvasion["DefenderReward"]["countedItems"]),
                        attackerCredits = (jsonInvasion["DefenderReward"]["credits"]),
                        defenderCountables = (jsonInvasion["AttackerReward"]["countedItems"]),
                        defenderCredits = (jsonInvasion["AttackerReward"]["credits"]);

                    string attackerRewardStr = (attackerCountables != null ? wfDataMapper.GetItemName(attackerCountables[0]["ItemType"].ToString()) : ""),
                        defenderRewardStr = (defenderCountables != null ? wfDataMapper.GetItemName(defenderCountables[0]["ItemType"].ToString()) : "");

                    JToken attackerRewardParam = null;
                    if (attackerCountables != null) attackerRewardParam = attackerCountables[0]["ItemType"].ToString();
                    JToken defenderRewardParam = null;
                    if (defenderCountables != null) defenderRewardParam = defenderCountables[0]["ItemType"].ToString();

                    int attackerRewardQuantityParam = attackerCountables != null ? (attackerCountables[0]["ItemCount"] != null ? int.Parse(attackerCountables[0]["ItemCount"].ToString()) : 1) : 0;
                    int defenderRewardQuantityParam = defenderCountables != null ? (defenderCountables[0]["ItemCount"] != null ? int.Parse(defenderCountables[0]["ItemCount"].ToString()) : 1) : 0;

                    int goal = int.Parse(jsonInvasion["Goal"].ToString()), progress = int.Parse(jsonInvasion["Count"].ToString());

                    if ((System.Math.Abs(progress) < goal) /*&& (!String.IsNullOrEmpty(attackerRewardStr) || !String.IsNullOrEmpty(defenderRewardStr))*/)
                    {
                        if (RewardIsNotIgnored(int.Parse((attackerCredits ?? 0).ToString()), itemURI: (attackerRewardParam ?? "").ToString(), itemQuantity:attackerRewardQuantityParam) || RewardIsNotIgnored(int.Parse((defenderCredits ?? 0).ToString()), itemURI: (defenderRewardParam ?? "").ToString(), itemQuantity:defenderRewardQuantityParam))
                        {
                            //Mission Info corresponds to the faction to fight against.
                            MissionInfo attackerInfo = new MissionInfo(jsonInvasion["AttackerMissionInfo"]["faction"].ToString(),
                                jsonInvasion["DefenderMissionInfo"]["missionType"].ToString(),
                                defenderCredits != null ? int.Parse(defenderCredits.ToString()) : 0,
                                String.IsNullOrEmpty(defenderRewardStr) ? "" : defenderRewardStr,
                                defenderRewardQuantityParam,
                                int.Parse(jsonInvasion["DefenderMissionInfo"]["minEnemyLevel"].ToString()),
                                int.Parse(jsonInvasion["DefenderMissionInfo"]["maxEnemyLevel"].ToString()));

                            MissionInfo defenderInfo = new MissionInfo(jsonInvasion["DefenderMissionInfo"]["faction"].ToString(),
                                jsonInvasion["AttackerMissionInfo"]["missionType"].ToString(),
                                attackerCredits != null ? int.Parse(attackerCredits.ToString()) : 0,
                                String.IsNullOrEmpty(attackerRewardStr) ? "" : attackerRewardStr,
                                attackerRewardQuantityParam,
                                int.Parse(jsonInvasion["AttackerMissionInfo"]["minEnemyLevel"].ToString()),
                                int.Parse(jsonInvasion["AttackerMissionInfo"]["maxEnemyLevel"].ToString()));

                            double secondsUntilStart = double.Parse(jsonInvasion["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                            DateTime startTime = DateTime.Now.AddSeconds(secondsUntilStart);

                            currentInvasion = new WarframeInvasion(attackerInfo, defenderInfo, id, wfDataMapper.GetNodeName(loc), startTime, int.Parse(jsonInvasion["Goal"].ToString()));
                            InvasionsList.Add(currentInvasion);
                            NewInvasions.Add(currentInvasion);
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
                    int b = int.Parse(jsonInvasion["Count"].ToString());
                    currentInvasion.UpdateProgress(int.Parse(jsonInvasion["Count"].ToString()));
                    CreateNewInvasionReceivedEvent(currentInvasion);
                }
            }
        }

        private void ParseVoidTrader()
        {
            foreach (var jsonTrader in _worldState["VoidTraders"])
            {
                WarframeVoidTrader currentTrader = VoidTraders.Find(x => x.GUID == jsonTrader["_id"]["$id"].ToString());
                if (currentTrader == null)
                {
                    string id = jsonTrader["_id"]["$id"].ToString();
                    string loc = jsonTrader["Node"].ToString();

                    double secondsUntilStart = double.Parse(jsonTrader["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    double secondsUntilExpire = double.Parse(jsonTrader["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    DateTime startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    DateTime expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);

                    if (DateTime.Now < expireTime)
                    {
                        currentTrader = new WarframeVoidTrader(id, wfDataMapper.GetNodeName(loc), startTime, expireTime);
                        VoidTraders.Add(currentTrader);

                        JToken traderInventory = jsonTrader["Manifest"];
                        if (traderInventory != null)
                        {
                            foreach (var i in traderInventory)
                            {
                                currentTrader.AddTraderItem(wfDataMapper.GetItemName(i["ItemType"].ToString()), int.Parse(i["RegularPrice"].ToString()), int.Parse(i["PrimePrice"].ToString()));
                            }
                        }
                    }
                }
                else
                {
                    if (currentTrader.ExpireTime < DateTime.Now)
                        VoidTraders.Remove(currentTrader);
                }

                if (currentTrader.ExpireTime > DateTime.Now)
                    CreateNewVoidTraderReceivedEvent(currentTrader);
            }
        }

        private void ParseJsonEvents()
        {
            ParseAlerts();
            ParseInvasions();
            ParseVoidTrader();
        }

        private bool RewardIsNotIgnored(int credits = 0, string itemURI = "", int itemQuantity = 1)
        {
            bool result = false;

            //Check if the credit reward satisfies minimum
            if (wfDataMapper.GetMinimumCredits() <= credits)
                result = true;

            if (!String.IsNullOrEmpty(itemURI))
            {
                WarframeItem item = wfDataMapper.GetItem(itemURI);
                //Check if the category is ignored
                if (item != null)
                {
                    var categories = wfDataMapper.GetItemCategories(item);
                    foreach (var c in categories)
                    {
                        if (c.Ignore == 0)
                            result = true;
                    }
                    //Check if the item is being ignored
                    if ((item != null) && (item.Ignore == 0))
                    {
                        result = true;
                        //Check that the item quantity satisfies the minimum quantity
                        if (wfDataMapper.GetWarframeItemMinimumQuantity(item) <= itemQuantity)
                            result = true;
                        else result = false;
                    }
                    else
                        result = false;
                }
            }

            return result;
        }

        [Obsolete]
        private void RemoveExpiredAlerts()
        {
            List<WarframeAlert> AlertsListToIterate = new List<WarframeAlert>(AlertsList);

            foreach(var alert in AlertsListToIterate)
            {
                if (DateTime.Now > alert.ExpireTime)
                {
                    WarframeAlert targetAlert = AlertsList.Find(x => x.GUID == alert.GUID);
                    AlertsList.Remove(targetAlert);
                }
            }
        }

        public WarframeAlert GetAlert(string alertID)
        {
            return AlertsList.Find(x => x.GUID == alertID);
        }

        public WarframeInvasion GetInvasion(string invasionID)
        {
            return InvasionsList.Find(x => x.GUID == invasionID);
        }

        private void CreateNewAlertReceivedEvent(WarframeAlert newAlert)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<WarframeAlertScrapedArgs> handler = AlertScraped;

            WarframeAlertScrapedArgs e = new WarframeAlertScrapedArgs(newAlert);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }

        private void CreateNewInvasionReceivedEvent(WarframeInvasion newInvasion)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<WarframeInvasionScrapedArgs> handler = InvasionScraped;

            WarframeInvasionScrapedArgs e = new WarframeInvasionScrapedArgs(newInvasion);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }

        private void CreateNewVoidTraderReceivedEvent(WarframeVoidTrader newTrader)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<WarframeVoidTraderScrapedArgs> handler = VoidTraderScraped;

            WarframeVoidTraderScrapedArgs e = new WarframeVoidTraderScrapedArgs(newTrader);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }

        private void CreateAlertExpiredEvent(WarframeAlert expiredAlert, string messageID = "")
        {
            EventHandler<WarframeAlertExpiredArgs> handler = AlertExpired;

            WarframeAlertExpiredArgs e = new WarframeAlertExpiredArgs(expiredAlert, messageID);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }

        [Obsolete]
        private void CreateExistingAlertFoundEvent(WarframeAlert alert, string messageID = "")
        {
            EventHandler<ExistingAlertFoundArgs> handler = ExistingAlertFound;

            ExistingAlertFoundArgs e = new ExistingAlertFoundArgs(alert, messageID);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }

        [Obsolete]
        public bool AlertExists(WarframeAlert alert)
        {
            return !(AlertsList.Find(x => x.GUID == alert.GUID) == null);
        }

        [Obsolete]
        public void HandleOldAlerts(Dictionary<WarframeAlert, string> alerts)
        {
            //Iterate through the dictionary of events and identify any alerts which have expired.
            //WubbyBot will handle accordingly.
            foreach(KeyValuePair<WarframeAlert, string> entry in alerts)
            {
                Console.WriteLine("Checking for expire state");

                if (entry.Key.ExpireTime < DateTime.Now)
                    CreateAlertExpiredEvent(entry.Key, entry.Value);
                else
                    CreateExistingAlertFoundEvent(entry.Key, entry.Value);
            }
        }

        public bool IsAlertNew(WarframeAlert alert)
        {
            return (NewAlerts.Find(x => x.GUID == alert.GUID) != null);
        }

        public bool IsInvasionNew(WarframeInvasion invasion)
        {
            return (NewInvasions.Find(x => x.GUID == invasion.GUID) != null);
        }
    }
}
