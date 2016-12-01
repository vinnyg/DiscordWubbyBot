using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DiscordSharp;
using DiscordSharp.Objects;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using WubbyBot.Events.Extensions;
using DiscordWrapper;
using DiscordSharpTest.WarframeEvents;

namespace DiscordSharpTest
{
    public class WubbyBot : DiscordBot
    {
#if DEBUG
        const string ALERTS_CHANNEL = "wf-dev";
#else
        const string ALERTS_CHANNEL = "warframe-events";
#endif
        const string ALERTS_ARCHIVE_CHANNEL = "wf-alert-archive";

        private Random _randomNumGen;

        //Events
        private Timer _eventUpdateInterval { get; set; }

        //Miscellaneous
        private string _currentGame { get; set; }
        
        private WarframeEventsContainer _eventsContainer { get; set; }
        private Dictionary<WarframeAlert, DiscordMessage> _alertMessageAssociations { get; set; }
        private List<MessageQueueElement> _alertMessagePostQueue { get; set; }
        private List<MessageQueueElement> _invasionMessagePostQueue { get; set; }
        private List<MessageQueueElement> _voidTraderMessagePostQueue { get; set; }
        private List<MessageQueueElement> _voidFissureMessagePostQueue { get; set; }
        private List<MessageQueueElement> _sortieMessagePostQueue { get; set; }
        private List<MessageQueueElement> _timeCycleMessagePostQueue { get; set; }

        private DiscordMessage _alertMessage { get; set; } = null;
        private DiscordMessage _traderMessage { get; set; } = null;
        private DiscordMessage _fissureMessage { get; set; } = null;
        private DiscordMessage _sortieMessage { get; set; } = null;
        private DiscordMessage _timeCycleMessage { get; set; } = null;

        //List just in case the invasion message exceeds the 2000 character limit
        private List<DiscordMessage> invasionMessages = null;

        //Give the bot a name
        public WubbyBot(string name, string devLogName = "") : base(name, devLogName)
        {
            _randomNumGen = new Random((int)DateTime.Now.Ticks);
        }

        //Initialise the bot
        override public void Init()
        {
#if DEBUG
            Log("DEBUG MODE");
#endif
            if (File.Exists("gameslist.json"))
            {
                var gamesList = JsonConvert.DeserializeObject<string[]>(File.ReadAllText("gameslist.json"));
                _currentGame = gamesList != null ? gamesList[_randomNumGen.Next(0, gamesList.Length)] : "null!";
            }
            SetupEvents();
        }

        private void InitSystems()
        {
            Log("Initialising Warframe JSON Parser...");
            _eventsContainer = new WarframeEventsContainer();
            _alertMessageAssociations = new Dictionary<WarframeAlert, DiscordMessage>();

            _alertMessagePostQueue = new List<MessageQueueElement>();
            _invasionMessagePostQueue = new List<MessageQueueElement>();
            _voidTraderMessagePostQueue = new List<MessageQueueElement>();
            _voidFissureMessagePostQueue = new List<MessageQueueElement>();
            _sortieMessagePostQueue = new List<MessageQueueElement>();
            _timeCycleMessagePostQueue = new List<MessageQueueElement>();

            invasionMessages = new List<DiscordMessage>();

            Log("Initialisation complete.");
        }

        //Start application operation cycle
        private void StartPostTimer()
        {
            _eventUpdateInterval = new Timer((e) => { PostAlertMessage(); PostInvasionMessage(); PostSortieMessage(); PostVoidFissureMessage(); PostVoidTraderMessage(); PostTimeCycleMessage(); }, null, 3000, (int)(TimeSpan.FromMinutes(1).TotalMilliseconds));
        }

        private void PostAlertMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            if (_alertMessagePostQueue.Count > 0) finalMsg.Append("```ACTIVE ALERTS```" + Environment.NewLine);
            else finalMsg.Append("```NO ACTIVE ALERTS```" + Environment.NewLine);

            foreach (var m in _alertMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";
                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                {
                    finalMsg.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, true));
                }
                finalMsg.Append("```" + Environment.NewLine);
            }

            if (_alertMessage == null)
                _alertMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), _alertMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach(var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }
            
            _alertMessagePostQueue.Clear();
        }

        private void PostInvasionMessage()
        {
            //Build all invasion strings into a single message
            List<StringBuilder> finalMessagesToPost = new List<StringBuilder>();
            List<string> messagesToNotify = new List<string>();
            //Messages will append to this builder until the length reaches the MESSAGE_CHAR_LIMIT value
            StringBuilder entryForFinalMsg = new StringBuilder();
            finalMessagesToPost.Add(entryForFinalMsg);

            //Sometimes new invasions are added while the loop is still iterating, which causes problems.
            List<MessageQueueElement> invasionMessageQueue = new List<MessageQueueElement>(_invasionMessagePostQueue);

            //Append this before the loop so that it only appears once in the message
            if (invasionMessageQueue.Count > 0) entryForFinalMsg.Append("```ACTIVE INVASIONS```" + Environment.NewLine);
            else entryForFinalMsg.Append("```NO ACTIVE INVASIONS```" + Environment.NewLine);

            foreach (var m in invasionMessageQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";
                StringBuilder invasionMsgToAppend = new StringBuilder(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false) + Environment.NewLine);
                
                if (m.NotifyClient)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, true));
                    invasionMsgToAppend.Append("( new )");
                }
                invasionMsgToAppend.Append("```" + Environment.NewLine);

                //Create a new entry in the post queue if the character length of the current message hits the character limit
                if (entryForFinalMsg.Length + invasionMsgToAppend.Length < MESSAGE_CHAR_LIMIT) { entryForFinalMsg.Append(invasionMsgToAppend); } else
                {
                    entryForFinalMsg.Append(invasionMsgToAppend.ToString());
                    finalMessagesToPost.Add(entryForFinalMsg);
                }
            }

            //Notify client for relevant messages
            if (invasionMessages.Count > 0)
            {
                foreach (var item in messagesToNotify)
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
            }

            for (var i = 0; i < finalMessagesToPost.Count; ++i)
            {
                //If invasion messages already exist
                if (i < invasionMessages.Count)
                    EditMessage(finalMessagesToPost.ElementAt(i).ToString(), invasionMessages.ElementAt(i), Client.GetChannelByName(ALERTS_CHANNEL));
                else //When we run out of available invasion messages to edit
                    invasionMessages.Add(SendMessage(finalMessagesToPost.ElementAt(i).ToString(), Client.GetChannelByName(ALERTS_CHANNEL)));
            }

            //Get rid of any extra messages which have been created to deal with long character counts.
            int totalInvasionMessages = invasionMessages.Count;
            if (totalInvasionMessages > finalMessagesToPost.Count)
            {
                for (var i = finalMessagesToPost.Count; i < totalInvasionMessages; ++i)
                {
                    if (i > 0)
                    {
                        DeleteMessage(invasionMessages.ElementAt(i));
                        invasionMessages.RemoveAt(i);
                    }
                }
            }
#if DEBUG
            foreach(var i in finalMessagesToPost)
            {
                Log(i.Length + " characters long");
            }
#endif
            _invasionMessagePostQueue.Clear();
        }

        private void PostVoidTraderMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            finalMsg.Append($"```VOID TRADER{(_voidTraderMessagePostQueue.Count() == 0 ? " HAS LEFT" : String.Empty)}```" + Environment.NewLine);

            foreach (var m in _voidTraderMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";
                
                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                finalMsg.Append("```" + Environment.NewLine);
            }

            if (_traderMessage == null)
                _traderMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), _traderMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            _voidTraderMessagePostQueue.Clear();
        }

        private void PostVoidFissureMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            if (_voidFissureMessagePostQueue.Count > 0) finalMsg.Append("```VOID FISSURES```" + Environment.NewLine);
            else finalMsg.Append("```NO VOID FISSURES```" + Environment.NewLine);

            _voidFissureMessagePostQueue.OrderBy(s => (s.WFEvent as WarframeVoidFissure).GetFissureIndex());

            foreach (var m in _voidFissureMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";

                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                {
                    finalMsg.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false));
                }
                finalMsg.Append("```" + Environment.NewLine);
            }

            if (_fissureMessage == null)
                _fissureMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), _fissureMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            _voidFissureMessagePostQueue.Clear();
        }

        private void PostSortieMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            if (_sortieMessagePostQueue.Count > 0) finalMsg.Append("```ACTIVE SORTIES```" + Environment.NewLine);
            else finalMsg.Append("```NO SORTIES```" + Environment.NewLine);

            foreach (var m in _sortieMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";

                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, true));
                }
                finalMsg.Append("```" + Environment.NewLine);
            }

            if (_sortieMessage == null)
                _sortieMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), _sortieMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            _sortieMessagePostQueue.Clear();
        }

        private void PostTimeCycleMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();

            foreach (var m in _timeCycleMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```";

                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.WFEvent as dynamic, false));

                finalMsg.Append("```" + Environment.NewLine);
            }

            if (_timeCycleMessage == null)
                _timeCycleMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), _timeCycleMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            _timeCycleMessagePostQueue.Clear();
        }

        private void AddToAlertPostQueue(WarframeAlert alert, bool notifyClient, bool alertHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            _alertMessagePostQueue.Add(new MessageQueueElement(alert, notifyClient, alertHasExpired));
        }

        private void AddToInvasionPostQueue(WarframeInvasion invasion, bool notifyClient, bool invasionHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            _invasionMessagePostQueue.Add(new MessageQueueElement(invasion, notifyClient, invasionHasExpired));
        }

        private void AddToVoidTraderPostQueue(WarframeVoidTrader trader, bool notifyClient, bool traderHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            _voidTraderMessagePostQueue.Add(new MessageQueueElement(trader, notifyClient, traderHasExpired));
        }

        private void AddToVoidFissurePostQueue(WarframeVoidFissure fissure, bool notifyClient, bool fissureHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            _voidFissureMessagePostQueue.Add(new MessageQueueElement(fissure, notifyClient, fissureHasExpired));
        }

        private void AddToSortiePostQueue(WarframeSortie sortie, bool notifyClient, bool sortieHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            _sortieMessagePostQueue.Add(new MessageQueueElement(sortie, notifyClient, sortieHasExpired));
        }

        private void AddToTimeCyclePostQueue(WarframeTimeCycleInfo cycle, bool notifyClient, bool eventHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            _timeCycleMessagePostQueue.Add(new MessageQueueElement(cycle, notifyClient, false));
        }

        public void Shutdown()
        {
            Log("Shutting down...");
            DeleteMessage(_alertMessage);
            DeleteMessage(_fissureMessage);
            DeleteMessage(_sortieMessage);
            DeleteMessage(_traderMessage);
            DeleteMessage(_timeCycleMessage);

            //Sometimes the invasions message may be split up over multiple Discord messages so each one needs to be deleted.
            foreach (var i in invasionMessages)
                DeleteMessage(i);
        }

        private Task SetupEvents()
        {
            Console.ForegroundColor = ConsoleColor.White;
            return Task.Run(() =>
            {
                Client.MessageReceived += (sender, e) =>
                {
                    //Don't log messages posted in the log channel
                    if (e.Channel.Name != LogChannelName)
                        Log($"Message from {e.Author.Username} in #{e.Channel.Name} on {e.Channel.Parent.Name}: {e.Message.ID}");
                };
                /*_client.GuildCreated += (sender, e) =>
                {
                    owner.SlideIntoDMs($"Joined server {e.server.name} ({e.server.id})");
                };*/
                /*_client.SocketClosed += (sender, e) =>
                {
                    WriteError($"Socket Closed! Code: {e.Code}. Reason: {e.Reason}. Clear: {e.WasClean}.");
                    Console.WriteLine("Waiting 6 seconds to reconnect..");
                    Thread.Sleep(6 * 1000);
                    client.Connect();
                };
                _client.TextClientDebugMessageReceived += (sender, e) =>
                {
                    if (e.message.Level == MessageLevel.Error || e.message.Level == MessageLevel.Critical)
                    {
                        WriteError($"(Logger Error) {e.message.Message}");
                        try
                        {
                            owner.SlideIntoDMs($"Bot error ocurred: ({e.message.Level.ToString()})```\n{e.message.Message}\n```");
                        }
                        catch { }
                    }
                    if (e.message.Level == MessageLevel.Warning)
                        WriteWarning($"(Logger Warning) {e.message.Message}");
                };*/
                Client.Connected += (sender, e) =>
                {
                    Log($"Connected as {e.User.Username}");

                    InitSystems();

                    SetupWarframeEventsTask();
                };

#if DEBUG
                Client.SocketOpened += (sender, e) =>
                {
                    Log("Socket was opened!");
                };

                Client.SocketClosed += (sender, e) =>
                {
                    Log("Socket was closed!");
                };
#endif

                /*Client.MessageDeleted += (sender, e) =>
                {
                    JToken token;
                    string messageID = "";
                    if (e.RawJson.TryGetValue("id", out token))
                    {
                        messageID = (string)token;
                    }

                    foreach (var alert in _activeAlerts)
                    {
                        if (alert.Item1.AssociatedMessageID == messageID)
                        {
                            Log("An associated message for an alert has been deleted.") ;
                            _activeAlerts.Remove(alert);
                        }
                    }
                };*/

                if (Client.SendLoginRequest() != null)
                {
                    Client.Connect();
                }

                Thread.Sleep(3000);
                Client.UpdateCurrentGame(_currentGame);
            }
            );
        }

        private Task SetupWarframeEventsTask()
        {
            return Task.Run(() =>
            {
                _eventsContainer.AlertScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Alert Scraped!");
#endif
                    bool alertIsNew = _eventsContainer.IsAlertNew(e.Alert);
                    AddToAlertPostQueue(e.Alert, alertIsNew, e.Alert.IsExpired());
                };

                _eventsContainer.InvasionScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Invasion Scraped!");
#endif
                    bool invasionIsNew = _eventsContainer.IsInvasionNew(e.Invasion);
                    AddToInvasionPostQueue(e.Invasion, invasionIsNew, e.Invasion.IsExpired());
                };

                _eventsContainer.VoidTraderScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Void Trader Scraped!");
#endif
                    AddToVoidTraderPostQueue(e.Trader, false, e.Trader.IsExpired());
                };

                _eventsContainer.VoidFissureScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Fissure Scraped!");
#endif
                    AddToVoidFissurePostQueue(e.Fissure, false, e.Fissure.IsExpired());
                };

                _eventsContainer.SortieScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Sortie Scraped!");
#endif
                    AddToSortiePostQueue(e.Sortie, false, e.Sortie.IsExpired());
                };

                _eventsContainer.DayCycleScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Day Cycle Scraped!");
#endif
                    AddToTimeCyclePostQueue(e.cycleInfo, false, false);
                };

                _eventsContainer.ExistingAlertFound += (sender, e) =>
                {
                    if (Client.ReadyComplete == true)
                    {
                        DiscordMessage targetMessage = null;
                        targetMessage = GetMessageByID(e.MessageID, Client.GetChannelByName(ALERTS_CHANNEL));

                        if (targetMessage == null)
                            Log($"Message {e.MessageID} could not be found.");
                        else
                        {
#if DEBUG
                            Log($"Message {e.MessageID} was found.");
#endif
                            _alertMessageAssociations.Add(e.Alert, targetMessage);
                        }
                    }
                };
                
                _eventsContainer.Start();
                StartPostTimer();
            });
        }

        private int RollDice(int min, int max)
        {
            throw new NotImplementedException();

            //return _randomNumGen.Next(min, max);
        }

        public class MessageQueueElement
        {
            public WarframeEvent WFEvent;
            public bool NotifyClient;
            public bool EventHasExpired;

            public MessageQueueElement(WarframeEvent wfEvent, bool notify, bool eventHasExpired)
            {
                NotifyClient = notify;
                WFEvent = wfEvent;
                EventHasExpired = eventHasExpired;
            }

            public MessageQueueElement(MessageQueueElement msg)
            {
                NotifyClient = msg.NotifyClient;
                WFEvent = msg.WFEvent;
                EventHasExpired = msg.EventHasExpired;
            }
        };
    }
}
