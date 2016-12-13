using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using WubbyBot.Events.Extensions;
using DiscordWrapper;
using System.Reflection;
using System.Configuration;
using DSharpPlus.Objects;
using WarframeWorldStateAPI.Components;
using WarframeWorldStateAPI.WarframeEvents;

namespace DiscordSharpTest
{
    //This is an extension of DiscordBot implementing specific features for Warframe
    public class WubbyBot : DiscordBot
    {
#if DEBUG
        private long _alertsChannelID = long.Parse(ConfigurationManager.AppSettings["WubbyBotAlertsChannelDebug"]);
#else
        private long _alertsChannelID = long.Parse(ConfigurationManager.AppSettings["WubbyBotAlertsChannel"]);
#endif
        private const string ALERTS_ARCHIVE_CHANNEL = "wf-alert-archive";

        private const int EVENT_UPDATE_TIMER_DUE_TIME_MILLISECONDS = 3000;
        private const int EVENT_UPDATE_INTERVAL_MILLISECONDS = 60000;

        private readonly Random _randomNumGen;

        //This is how often we will  update our Discord messages and scrape for new event information
        private Timer _eventUpdateTimer;
        private WarframeEventInformationParser _eventsParser = new WarframeEventInformationParser();

        //These lists store containers which hold information such as message content and additional property information
        private List<MessageQueueElement> _alertMessagePostQueue = new List<MessageQueueElement>();
        private List<MessageQueueElement> _invasionMessagePostQueue = new List<MessageQueueElement>();
        private List<MessageQueueElement> _invasionConstructionMessagePostQueue = new List<MessageQueueElement>();
        private List<MessageQueueElement> _voidTraderMessagePostQueue = new List<MessageQueueElement>();
        private List<MessageQueueElement> _voidFissureMessagePostQueue = new List<MessageQueueElement>();
        private List<MessageQueueElement> _sortieMessagePostQueue = new List<MessageQueueElement>();
        private List<MessageQueueElement> _timeCycleMessagePostQueue = new List<MessageQueueElement>();

        //These are the Discord message representations of all Warframe events
        private DiscordMessage _alertMessage;
        private DiscordMessage _traderMessage;
        private DiscordMessage _fissureMessage;
        private DiscordMessage _sortieMessage;
        private DiscordMessage _timeCycleMessage;

        //Store invasion discord messages in a list, as the number of invasions can sometimes cause the Discord message to exceed the maximum character limit
        private List<DiscordMessage> _invasionMessages =  new List<DiscordMessage>();
        
        public WubbyBot(string name, string devLogName = "") : base(name, devLogName)
        {
            _randomNumGen = new Random((int)DateTime.Now.Ticks);
        }

        //Start the task
        override public void Init()
        {
#if DEBUG
            Log("DEBUG MODE");
#endif
            SetupEvents();
        }

        //This task is responsible for ensuring that a connection to Discord has been made, as well as handling the lifetime of the application
        private void SetupEvents()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Client.MessageReceived += (sender, e) =>
            {
                //Don't log messages posted in the log channel
                if (e.Channel.Name != LogChannelName)
                    Log($"Message from {e.Author.Username} in #{e.Channel.Name} on {e.Channel.Parent.Name}: {e.Message.ID}");
            };

            Client.Connected += (sender, e) =>
            {
                Log($"Connected as {e.User.Username}");
                SetupWarframeEventsTask();

                SetCurrentGame(false);
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
            Connect();
        }

        /*private void InitSystems()
        {
            //We want to wait until the bot has connected to the Discord server before we can do anything
            Log("Initialising Warframe JSON Parser...");
            _eventsScraper = new WarframeJSONScraper();

            Log("Initialisation complete.");
        }*/
        
        private void SetupWarframeEventsTask()
        {
            /*_eventsScraper.AlertScraped += (sender, e) =>
            {
#if DEBUG
                Log("Alert Scraped!");
#endif
                bool alertIsNew = _eventsScraper.IsAlertNew(e.Alert);
                AddToAlertPostQueue(e.Alert, alertIsNew, e.Alert.IsExpired());
            };

            _eventsScraper.InvasionScraped += (sender, e) =>
            {
#if DEBUG
                Log("Invasion Scraped!");
#endif
                bool invasionIsNew = _eventsScraper.IsInvasionNew(e.Invasion);
                AddToInvasionPostQueue(e.Invasion, invasionIsNew, e.Invasion.IsExpired());
            };

            _eventsScraper.ConstructionProjectsScraped += (sender, e) =>
            {
                //bool invasionIsNew = _eventsScraper.IsInvasionNew(e.Invasion);
                AddToInvasionConstructionPostQueue(e.ConstructionProject, false);
            };

            _eventsScraper.VoidTraderScraped += (sender, e) =>
            {
#if DEBUG
                Log("Void Trader Scraped!");
#endif
                AddToVoidTraderPostQueue(e.Trader, false, e.Trader.IsExpired());
            };

            _eventsScraper.VoidFissureScraped += (sender, e) =>
            {
#if DEBUG
                Log("Fissure Scraped!");
#endif
                AddToVoidFissurePostQueue(e.Fissure, false, e.Fissure.IsExpired());
            };

            _eventsScraper.SortieScraped += (sender, e) =>
            {
#if DEBUG
                Log("Sortie Scraped!");
#endif
                AddToSortiePostQueue(e.Sortie, false, e.Sortie.IsExpired());
            };

            _eventsScraper.DayCycleScraped += (sender, e) =>
            {
#if DEBUG
                Log("Day Cycle Scraped!");
#endif
                AddToTimeCyclePostQueue(e.cycleInfo, false);
            };

            _eventsScraper.Start();*/
            StartPostTimer();
        }

        private void CheckForWarframeEvents()
        {
            foreach (var alert in _eventsParser.GetAlerts())
            {
#if DEBUG
                Log("Alert Scraped!");
#endif
                bool alertIsNew = _eventsParser.IsAlertNew(alert);
                AddToAlertPostQueue(alert, alertIsNew, alert.IsExpired());
            }

            foreach (var invasion in _eventsParser.GetInvasions())
            {
#if DEBUG
                Log("Invasion Scraped!");
#endif
                bool invasionIsNew = _eventsParser.IsInvasionNew(invasion);
                AddToInvasionPostQueue(invasion, invasionIsNew, invasion.IsExpired());
            }
            
            foreach (var project in _eventsParser.GetInvasionConstruction())
            {
                AddToInvasionConstructionPostQueue(project, false);
            }

            foreach (var fissure in _eventsParser.GetVoidFissures())
            {
#if DEBUG
                Log("Fissure Scraped!");
#endif
                AddToVoidFissurePostQueue(fissure, false, fissure.IsExpired());
            }
            
            foreach (var sortie in _eventsParser.GetSorties())
            {
#if DEBUG
                Log("Sortie Scraped!");
#endif
                AddToSortiePostQueue(sortie, false, sortie.IsExpired());
            }

            foreach (var trader in _eventsParser.GetVoidTrader())
            {
#if DEBUG
                Log("Void Trader Scraped!");
#endif
                AddToVoidTraderPostQueue(trader, false, trader.IsExpired());
            }

            AddToTimeCyclePostQueue(_eventsParser.GetTimeCycle(), false);
        }

        //Start application operation cycle
        private void StartPostTimer()
        {
            _eventUpdateTimer = new Timer((e) =>
            {
                CheckForWarframeEvents();
                PostAlertMessage();
                PostInvasionMessage();
                PostSortieMessage();
                PostVoidFissureMessage();
                PostVoidTraderMessage();
                PostTimeCycleMessage();
            },
            null, EVENT_UPDATE_TIMER_DUE_TIME_MILLISECONDS, EVENT_UPDATE_INTERVAL_MILLISECONDS);
        }

        //Build and post the Discord message for alerts
        private void PostAlertMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            finalMessage.Append(WarframeEventExtensions.FormatMessage($"{(_alertMessagePostQueue.Count > 0 ? string.Empty : "NO ")}ACTIVE ALERTS", formatType: MessageFormat.Bold));

            foreach (var message in _alertMessagePostQueue)
            {
                //Provides code block formatting for Discord Messages
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                if (message.NotifyClient)
                {
                    coreMessageContent.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, true));
                }
                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), formatType: MessageFormat.CodeBlocks));
            }

            if (_alertMessage == null)
                _alertMessage = SendMessageToAlertsChannel(finalMessage.ToString());
            else
            {
                EditEventMessage(finalMessage.ToString(), _alertMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item);
                }
            }
            _alertMessagePostQueue.Clear();
        }

        //Build and post the Discord message for invasions
        private void PostInvasionMessage()
        {
            //Build all invasion strings into a single message
            var finalMessagesToPost = new List<StringBuilder>();
            var messagesToNotify = new List<string>();

            //Messages will append to this builder until the length reaches the MESSAGE_CHAR_LIMIT value
            //Due to the potential length of an invasion message, it may need to be broken down into smaller messages. Hence - entryForFinalMessage
            var entryForFinalMessage = new StringBuilder();
            finalMessagesToPost.Add(entryForFinalMessage);

            //Sometimes new invasions are added while the loop is still iterating, which causes problems.
            //Creating a copy to reduce the risk of concurrency problems.
            var invasionMessageQueue = new List<MessageQueueElement>(_invasionMessagePostQueue);
            var invasionConstructionMessageQueue = new List<MessageQueueElement>(_invasionConstructionMessagePostQueue);
            
            entryForFinalMessage.Append(WarframeEventExtensions.FormatMessage($"{(invasionMessageQueue.Count > 0 ? string.Empty : "NO ")}ACTIVE INVASIONS", formatType: MessageFormat.Bold));

            //Project Construction Information
            var constructionMessage = new StringBuilder();
            if (invasionConstructionMessageQueue.Count > 0)
            {
                foreach (var message in invasionConstructionMessageQueue)
                {
                    constructionMessage.Append(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                }

                entryForFinalMessage.Append(WarframeEventExtensions.FormatMessage(constructionMessage.ToString(), formatType: MessageFormat.CodeBlocks));
            }

            foreach (var message in invasionMessageQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContentEntry = new StringBuilder();
                coreMessageContentEntry.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                if (message.NotifyClient)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, true));
                    coreMessageContentEntry.Append("( new )");
                }

                //Create a new entry in the post queue if the character length of the current message hits the character limit
                if (entryForFinalMessage.Length + coreMessageContentEntry.Length < MESSAGE_CHAR_LIMIT)
                    entryForFinalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContentEntry.ToString(), formatType: MessageFormat.CodeBlocks));
                else
                {
                    entryForFinalMessage.Append(coreMessageContentEntry.ToString());
                    finalMessagesToPost.Add(entryForFinalMessage);
                }
            }
            
            if (_invasionMessages.Count > 0)
            {
                foreach (var item in messagesToNotify)
                    NotifyClient(item);
            }

            for (var i = 0; i < finalMessagesToPost.Count; ++i)
            {
                //If invasion messages already exist
                if (i < _invasionMessages.Count)
                    EditEventMessage(finalMessagesToPost.ElementAt(i).ToString(), _invasionMessages.ElementAt(i));
                else //When we run out of available invasion messages to edit
                    _invasionMessages.Add(SendMessageToAlertsChannel(finalMessagesToPost.ElementAt(i).ToString()));
            }

            //Get rid of any extra messages which have been created as a result of long character counts in Discord messages
            if (_invasionMessages.Count > finalMessagesToPost.Count)
            {
                var range = _invasionMessages.GetRange(finalMessagesToPost.Count, _invasionMessages.Count - finalMessagesToPost.Count);
                range.ForEach(msg => DeleteMessage(msg));

                _invasionMessages.RemoveRange(finalMessagesToPost.Count, _invasionMessages.Count - finalMessagesToPost.Count);
            }
#if DEBUG
            foreach(var i in finalMessagesToPost)
            {
                Log(i.Length + " characters long");
            }
#endif
            _invasionMessagePostQueue.Clear();
            _invasionConstructionMessagePostQueue.Clear();
        }

        //Build and post the Discord message for Void Traders
        private void PostVoidTraderMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();
            finalMessage.Append(WarframeEventExtensions.FormatMessage($"VOID TRADER{(_voidTraderMessagePostQueue.Count == 0 ? " HAS LEFT" : string.Empty)}", string.Empty, MessageFormat.Bold));

            //Core content of the Discord message without any formatting
            var coreMessageContent = new StringBuilder();

            foreach (var message in _voidTraderMessagePostQueue)
            {
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false) + Environment.NewLine);
            }
            finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), formatType: MessageFormat.CodeBlocks));

            if (_traderMessage == null)
                _traderMessage = SendMessageToAlertsChannel(finalMessage.ToString());
            else
            {
                EditEventMessage(finalMessage.ToString(), _traderMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item);
                }
            }

            _voidTraderMessagePostQueue.Clear();
        }

        //Build and post the Discord message for Void Fissures
        private void PostVoidFissureMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            finalMessage.Append(WarframeEventExtensions.FormatMessage($"{(_voidFissureMessagePostQueue.Count > 0 ? string.Empty : "NO ")}VOID FISSURES", formatType: MessageFormat.Bold));

            _voidFissureMessagePostQueue.OrderBy(s => (s.WarframeEvent as WarframeVoidFissure).GetFissureIndex());

            foreach (var message in _voidFissureMessagePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                if (message.NotifyClient)
                {
                    coreMessageContent.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                }
                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), formatType: MessageFormat.CodeBlocks));
            }

            if (_fissureMessage == null)
                _fissureMessage = SendMessageToAlertsChannel(finalMessage.ToString());
            else
            {
                EditEventMessage(finalMessage.ToString(), _fissureMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item);
                }
            }

            _voidFissureMessagePostQueue.Clear();
        }

        //Build and post the Discord message for sorties
        private void PostSortieMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            finalMessage.Append(WarframeEventExtensions.FormatMessage($"{(_sortieMessagePostQueue.Count > 0 ? string.Empty : "NO ")}SORTIES", formatType: MessageFormat.Bold));

            foreach (var message in _sortieMessagePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                if (message.NotifyClient)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, true));
                }
                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), formatType: MessageFormat.CodeBlocks));
            }

            if (_sortieMessage == null)
                _sortieMessage = SendMessageToAlertsChannel(finalMessage.ToString());
            else
            {
                EditEventMessage(finalMessage.ToString(), _sortieMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item);
                }
            }

            _sortieMessagePostQueue.Clear();
        }

        //Build and post the Discord message for day cycle information
        private void PostTimeCycleMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();
            
            finalMessage.Append(WarframeEventExtensions.FormatMessage("DAY CYCLE", formatType: MessageFormat.Bold));

            foreach (var message in _timeCycleMessagePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                if (message.NotifyClient)
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), formatType: MessageFormat.CodeBlocks));
            }

            if (_timeCycleMessage == null)
                _timeCycleMessage = SendMessageToAlertsChannel(finalMessage.ToString());
            else
            {
                EditEventMessage(finalMessage.ToString(), _timeCycleMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item);
                }
            }

            _timeCycleMessagePostQueue.Clear();
        }

        private void AddToAlertPostQueue(WarframeAlert alert, bool notifyClient, bool alertHasExpired)
        {
            _alertMessagePostQueue.Add(new MessageQueueElement(alert, notifyClient, alertHasExpired));
        }

        private void AddToInvasionPostQueue(WarframeInvasion invasion, bool notifyClient, bool invasionHasExpired)
        {
            _invasionMessagePostQueue.Add(new MessageQueueElement(invasion, notifyClient, invasionHasExpired));
        }

        private void AddToInvasionConstructionPostQueue(WarframeInvasionConstruction construction, bool invasionHasExpired)
        {
            _invasionConstructionMessagePostQueue.Add(new MessageQueueElement(construction, false, invasionHasExpired));
        }

        private void AddToVoidTraderPostQueue(WarframeVoidTrader trader, bool notifyClient, bool traderHasExpired)
        {
            _voidTraderMessagePostQueue.Add(new MessageQueueElement(trader, notifyClient, traderHasExpired));
        }

        private void AddToVoidFissurePostQueue(WarframeVoidFissure fissure, bool notifyClient, bool fissureHasExpired)
        {
            _voidFissureMessagePostQueue.Add(new MessageQueueElement(fissure, notifyClient, fissureHasExpired));
        }

        private void AddToSortiePostQueue(WarframeSortie sortie, bool notifyClient, bool sortieHasExpired)
        {
            _sortieMessagePostQueue.Add(new MessageQueueElement(sortie, notifyClient, sortieHasExpired));
        }

        private void AddToTimeCyclePostQueue(WarframeTimeCycleInfo cycle, bool notifyClient)
        {
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
            foreach (var i in _invasionMessages)
                DeleteMessage(i);

            Logout();
        }

        //Set the currently playing label in Discord
        public void SetCurrentGame(bool isStreaming, string gameName = "", string url = "")
        {
            //This method is not an override as the signature differs from the base method
            //Update the "Playing" message with a random game from the list if gameName is not provided
            if ((File.Exists("gameslist.json")) && (string.IsNullOrEmpty(gameName)))
            {
                var gamesList = JsonConvert.DeserializeObject<string[]>(File.ReadAllText("gameslist.json"));
                gameName = gamesList != null ? gamesList[_randomNumGen.Next(0, gamesList.Length)] : "null!";
            }

            base.SetCurrentGame(gameName, isStreaming, url);
#if DEBUG
            var assemblyName = Assembly.GetEntryAssembly().GetName();
            var assemblyVersion = assemblyName.Version;

            base.SetCurrentGame($"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}", false);
#endif
        }

        private int RollDice(int min, int max)
        {
            throw new NotImplementedException();

            //return _randomNumGen.Next(min, max);
        }

        private DiscordMessage SendMessageToAlertsChannel(string content)
        {
            return SendMessage(content, Client.GetChannelByID(_alertsChannelID));
        }

        private DiscordMessage EditEventMessage(string newContent, DiscordMessage targetMessage)
        {
            return EditMessage(newContent, targetMessage, Client.GetChannelByID(_alertsChannelID));
        }

        //Creates a new message which is automatically deleted shortly after to force a DiscordApp notification
        private void NotifyClient(string content)
        {
            DiscordMessage message = SendMessageToAlertsChannel(content);
            DeleteMessage(message);
        }

        //Contains information about the message as well as its content
        public class MessageQueueElement
        {
            public WarframeEvent WarframeEvent { get; set; }
            public bool NotifyClient { get; set; }
            public bool EventHasExpired { get; set; }

            public MessageQueueElement(WarframeEvent warframeEvent, bool notify, bool eventHasExpired)
            {
                NotifyClient = notify;
                WarframeEvent = warframeEvent;
                EventHasExpired = eventHasExpired;
            }

            public MessageQueueElement(MessageQueueElement msg)
            {
                NotifyClient = msg.NotifyClient;
                WarframeEvent = msg.WarframeEvent;
                EventHasExpired = msg.EventHasExpired;
            }
        };
    }
}
