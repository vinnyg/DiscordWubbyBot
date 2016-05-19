using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net;
using System.Data.SQLite;
using DiscordSharpTest.Events;

namespace DiscordSharpTest
{
    class WarframeEventsContainer
    {
        public List<WarframeAlert> AlertsList { get; private set; }
        public List<WarframeInvasion> InvasionsList { get; private set; }

        private XDocument _rssFeed { get; set; }
        private JObject _worldState { get; set; }

        private Timer _eventUpdateInterval { get; set; }

        private SQLiteConnection _dbConnection { get; set; }

        //Latest alerts and invasions from the most recent update
        //public List<string> NewestAlerts { get; private set; }
        //public List<string> NewestInvasions { get; private set; }

        #region Events
        public event EventHandler<WarframeAlertScrapedArgs> AlertScraped;
        public event EventHandler<WarframeInvasionScrapedArgs> InvasionScraped;
        #endregion

        public WarframeEventsContainer()
        {
            //Establish event update interval (every minute)
            _eventUpdateInterval = new Timer((e) => { ScrapeWorldState(); ParseJsonEvents(); }, null, 0, (int)(TimeSpan.FromMinutes(1.0).TotalMilliseconds));

            AlertsList = new List<WarframeAlert>();
            InvasionsList = new List<WarframeInvasion>();

            //NewestAlerts = new List<string>();
            //NewestInvasions = new List<string>();
        }

        void ScrapeWorldState()
        {
            using (WebClient wc = new WebClient())
            {
                _rssFeed = XDocument.Parse(wc.DownloadString("http://content.warframe.com/dynamic/rss.php"));
                _worldState = JObject.Parse(wc.DownloadString("http://content.warframe.com/dynamic/worldState.php"));
#if DEBUG
                Console.WriteLine("Scraping");
#endif
            }
        }

        void UpdateInvasionProgress()
        {
            throw new NotImplementedException();

            /*InvasionsList.Find()

            foreach (var invasion in InvasionsList)
            {
                invasion.UpdateInvasionProgress(_worldState["Invasions"][""])
            }*/
        }

        void ParseJsonEvents()
        {
            //Remove old entries
            //NewestAlerts.Clear();
            //NewestInvasions.Clear();

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

                    double secondsUntilStart = double.Parse(jsonAlert["Activation"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    double secondsUntilExpire = double.Parse(jsonAlert["Expiry"]["sec"].ToString()) - double.Parse(_worldState["Time"].ToString());
                    DateTime startTime = DateTime.Now.AddSeconds(secondsUntilStart);
                    DateTime expireTime = DateTime.Now.AddSeconds(secondsUntilExpire);
                    
                    MissionInfo alertInfo = new MissionInfo(jsonAlert["MissionInfo"]["faction"].ToString(),
                        jsonAlert["MissionInfo"]["missionType"].ToString(),
                        int.Parse(jsonAlert["MissionInfo"]["missionReward"]["credits"].ToString()),
                        //If for whatever reason, an alert returns both countables and non-countables, currently the countables will be returned.
                        //In addition, if an alert returns multiple different countables, only the first instance will be returned. This affects invasions as well!
                        (countables != null ? countables[0]["ItemType"].ToString() : (nonCountables != null ? nonCountables[0].ToString() : "")),
                        int.Parse((countables != null ? countables[0]["ItemCount"] : 1).ToString()),
                        int.Parse(jsonAlert["MissionInfo"]["minEnemyLevel"].ToString()),
                        int.Parse(jsonAlert["MissionInfo"]["maxEnemyLevel"].ToString()));

                    currentAlert = new WarframeAlert(alertInfo, id, loc, startTime, expireTime);
                    AlertsList.Add(currentAlert);
                    //NewestAlerts.Add(id);
                    //CreateAlertReceivedEvent(currentAlert);

#if DEBUG
                    Console.WriteLine("New Alert Event");
#endif
                }
                else
                {
#if DEBUG
                    Console.WriteLine("Update Alert Event");
#endif
                }
                CreateAlertReceivedEvent(currentAlert);
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

        WarframeAlert GetAlert(string alertID)
        {
            return AlertsList.Find(x => x.GUID == alertID);
        }

        WarframeInvasion GetInvasion(string invasionID)
        {
            return InvasionsList.Find(x => x.GUID == invasionID);
        }

        private void CreateAlertReceivedEvent(WarframeAlert newAlert)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<WarframeAlertScrapedArgs> handler = AlertScraped;

            WarframeAlertScrapedArgs e = new WarframeAlertScrapedArgs(newAlert);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }

        /*private void CreateAlertUpdatedEvent(WarframeAlert existingAlert)
        {
            EventHandler<WarframeAlertScrapedArgs> handler = AlertScraped;

            WarframeAlertScrapedArgs e = new WarframeAlertScrapedArgs(existingAlert);

            if (handler != null)    //Check if there are any subscribers
                handler(this, e);
        }*/
    }
}
