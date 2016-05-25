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

        //private XDocument _rssFeed { get; set; }
        private JObject _worldState { get; set; }

        private Timer _eventUpdateInterval { get; set; }

        private bool isRunning;

        private WarframeDataMapper mapper;

        //When an alert is read more than once, it will be added to this list. Alerts in this list are no longer new.
        private List<WarframeAlert> NewAlerts;

        #region Events
        public event EventHandler<WarframeAlertScrapedArgs> AlertScraped;
        public event EventHandler<WarframeInvasionScrapedArgs> InvasionScraped;
        public event EventHandler<WarframeAlertExpiredArgs> AlertExpired;
        public event EventHandler<ExistingAlertFoundArgs> ExistingAlertFound;
        #endregion

        public WarframeEventsContainer()
        {
            AlertsList = new List<WarframeAlert>();
            InvasionsList = new List<WarframeInvasion>();
            NewAlerts = new List<WarframeAlert>();
            mapper = new WarframeDataMapper();
        }

        public void Start(/*Dictionary<WarframeAlert, string> alertsToCheck*/)
        {
            if (!isRunning)
            {
                //HandleOldAlerts(alertsToCheck);

                //Establish event update interval (every minute)
                _eventUpdateInterval = new Timer((e) =>
                {
                    ScrapeWorldState();
                    ParseJsonEvents();
                }, null, 0, (int)(TimeSpan.FromMinutes(1.0).TotalMilliseconds));

                isRunning = true;
            }
        }

        void ScrapeWorldState()
        {
            using (WebClient wc = new WebClient())
            {
                //_rssFeed = XDocument.Parse(wc.DownloadString("http://content.warframe.com/dynamic/rss.php"));
                _worldState = JObject.Parse(wc.DownloadString("http://content.warframe.com/dynamic/worldState.php"));
            }
        }

        void UpdateInvasionProgress()
        {
            throw new NotImplementedException();
        }

        void ParseJsonEvents()
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
                        mapper.GetItemName(countables[0]["ItemType"].ToString()) :
                        (nonCountables != null ? mapper.GetItemName(nonCountables[0].ToString()) : ""));

                    double secondsUntilStart = double.Parse(jsonAlert["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    double secondsUntilExpire = double.Parse(jsonAlert["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    DateTime startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    DateTime expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);
                    
                    if (DateTime.Now < expireTime)
                    {
                        MissionInfo alertInfo = new MissionInfo(jsonAlert["MissionInfo"]["faction"].ToString(),
                            jsonAlert["MissionInfo"]["missionType"].ToString(),
                            int.Parse(jsonAlert["MissionInfo"]["missionReward"]["credits"].ToString()),
                            //If for whatever reason, an alert returns both countables and non-countables, currently the countables will be returned.
                            //In addition, if an alert returns multiple different countables, only the first instance will be returned. This affects invasions as well!
                            rewardStr,
                            int.Parse((countables != null ? countables[0]["ItemCount"] : 1).ToString()),
                            int.Parse(jsonAlert["MissionInfo"]["minEnemyLevel"].ToString()),
                            int.Parse(jsonAlert["MissionInfo"]["maxEnemyLevel"].ToString()));

                        currentAlert = new WarframeAlert(alertInfo, id, mapper.GetNodeName(loc), startTime, expireTime);
                        AlertsList.Add(currentAlert);
                        NewAlerts.Add(currentAlert);
#if DEBUG
                        Console.WriteLine("New Alert Event");
#endif
                    }
                }
                else
                {
#if DEBUG
                    Console.WriteLine("Update Alert Event");
#endif
                }

                if (currentAlert.ExpireTime > DateTime.Now)
                    CreateNewAlertReceivedEvent(currentAlert);
                else
                    CreateAlertExpiredEvent(currentAlert, "");
            }

            //Find Invasions
            foreach (var invasion in _worldState["Invasions"])
            {
                if (InvasionsList.Find(x => x.GUID == invasion["_id"]["$id"].ToString()) == null)
                {
                    string id = invasion["_id"]["$id"].ToString();
                    string loc = invasion["Node"].ToString();

                    JToken attackerLootResult = (invasion["DefenderReward"]["countedItems"]), 
                        defenderLootResult = (invasion["AttackerReward"]["countedItems"]);

                    //Mission Info corresponds to the faction to fight against.
                    MissionInfo attackerInfo = new MissionInfo(invasion["AttackerMissionInfo"]["faction"].ToString(),
                        invasion["DefenderMissionInfo"]["missionType"].ToString(), int.Parse(invasion["DefenderMissionInfo"]["missionReward"]["credits"].ToString()),
                        (attackerLootResult != null ? attackerLootResult[0]["ItemType"] : "").ToString(),
                        int.Parse((attackerLootResult != null ? attackerLootResult[0]["ItemCount"] ?? 1 : 0).ToString()),
                        int.Parse(invasion["DefenderMissionInfo"]["minEnemyLevel"].ToString()),
                        int.Parse(invasion["DefenderMissionInfo"]["maxEnemyLevel"].ToString()));

                    (attackerLootResult != null ? attackerLootResult[0]["ItemType"] : "").ToString();
                    int.Parse((attackerLootResult != null ? (attackerLootResult[0]["ItemCount"] ?? 1) : 0).ToString());

                    MissionInfo defenderInfo = new MissionInfo(invasion["DefenderMissionInfo"]["faction"].ToString(),
                        invasion["AttackerMissionInfo"]["missionType"].ToString(),
                        int.Parse(invasion["AttackerMissionInfo"]["missionReward"]["credits"].ToString()),
                        (defenderLootResult != null ? defenderLootResult[0]["ItemType"] : "").ToString(),
                        int.Parse((defenderLootResult != null ? defenderLootResult[0]["ItemCount"] ?? 1 : 0).ToString()),
                        int.Parse(invasion["AttackerMissionInfo"]["minEnemyLevel"].ToString()),
                        int.Parse(invasion["AttackerMissionInfo"]["maxEnemyLevel"].ToString()));

                    double secondsUntilStart = uint.Parse(invasion["Activation"]["sec"].ToString()) - uint.Parse(_worldState["Time"].ToString());
                    DateTime startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    
                    InvasionsList.Add(new WarframeInvasion(attackerInfo, defenderInfo, id, loc, startTime));
                    //NewestInvasions.Add(id);
                }
            }
        }

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
    }
}
