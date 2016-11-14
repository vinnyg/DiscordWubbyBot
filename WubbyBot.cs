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
using WubbyBot.Extensions;

namespace DiscordSharpTest
{
    class MessageQueueEntry
    {
        public WarframeEvent wfEvent;
        public bool NotifyClient;
        public bool EventHasExpired;

        public MessageQueueEntry(WarframeEvent wfEvent, bool notify, bool eventHasExpired)
        {
            NotifyClient = notify;
            this.wfEvent = wfEvent;
            EventHasExpired = eventHasExpired;
        }

        public MessageQueueEntry(MessageQueueEntry msg)
        {
            NotifyClient = msg.NotifyClient;
            this.wfEvent = msg.wfEvent;
            EventHasExpired = msg.EventHasExpired;
        }
    };

    class WubbyBot : DiscordBot
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

        //Cut out the middle-man "helper" class.
        private WarframeEventsContainer eventsContainer;
        //private WarframeEventMessageBuilder messageBuilder;
        private Dictionary<WarframeAlert, DiscordMessage> alertMessageAssociations;
        //private List<MessageQueueEntry> alertMessagePostQueue;
        private List<MessageQueueEntry> alertMessagePostQueue;
        private List<MessageQueueEntry> invasionMessagePostQueue;
        private List<MessageQueueEntry> voidTraderMessagePostQueue;
        private List<MessageQueueEntry> voidFissureMessagePostQueue;
        private List<MessageQueueEntry> sortieMessagePostQueue;
        private List<MessageQueueEntry> timeCycleMessagePostQueue;

        private DiscordMessage alertMessage = null;
        private DiscordMessage traderMessage = null;
        private DiscordMessage fissureMessage = null;
        private DiscordMessage sortieMessage = null;
        private DiscordMessage timeCycleMessage = null;

        //List just in case the invasion message exceeds the 2000 character limit
        private List<DiscordMessage> invasionMessages = null;

        //Give the bot a name
        public WubbyBot(string name, string devLogName = "") : base(name, devLogName)
        {
            _randomNumGen = new Random((int)DateTime.Now.Ticks);

            /*string credentialsFile = string.Format("{0}.txt", _name);
            if (File.Exists(credentialsFile))
            {
                StreamReader sr = new StreamReader(credentialsFile);
                _client.ClientPrivateInformation.email = sr.ReadLine();
                _client.ClientPrivateInformation.password = sr.ReadLine();
                sr.Close();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Error: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(string.Format("Credentials could not be found.\nPlease provide credentials in {0}.txt in the following format: \nemailaddress@domainname.com\npassword", _name));
                Console.ReadLine();
            }*/
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

        private void InitSubsystems()
        {
            Log("Initialising sub-systems");
            eventsContainer = new WarframeEventsContainer();
            //messageBuilder = new WarframeEventMessageBuilder();
            alertMessageAssociations = new Dictionary<WarframeAlert, DiscordMessage>();
            //database = new EventsDatabase();

            alertMessagePostQueue = new List<MessageQueueEntry>();
            invasionMessagePostQueue = new List<MessageQueueEntry>();
            voidTraderMessagePostQueue = new List<MessageQueueEntry>();
            voidFissureMessagePostQueue = new List<MessageQueueEntry>();
            sortieMessagePostQueue = new List<MessageQueueEntry>();
            timeCycleMessagePostQueue = new List<MessageQueueEntry>();

            invasionMessages = new List<DiscordMessage>();

            Log("Sub-systems initialised");
        }

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
            if (alertMessagePostQueue.Count > 0) finalMsg.Append("```ACTIVE ALERTS```" + Environment.NewLine);
            else finalMsg.Append("```NO ACTIVE ALERTS```" + Environment.NewLine);

            foreach (var m in alertMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";
                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false) + Environment.NewLine);
                //Invoking extension method as static method. This will require some rearchitecturing.
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                {
                    finalMsg.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, true));
                }
                finalMsg.Append("```" + Environment.NewLine);
            }

            if (alertMessage == null)
                alertMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), alertMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach(var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }
            
            alertMessagePostQueue.Clear();
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
            List<MessageQueueEntry> invasionMessageQueue = new List<MessageQueueEntry>(invasionMessagePostQueue);

            //Append this before the loop so that it only appears once in the message
            if (invasionMessageQueue.Count > 0) entryForFinalMsg.Append("```ACTIVE INVASIONS```" + Environment.NewLine);
            else entryForFinalMsg.Append("```NO ACTIVE INVASIONS```" + Environment.NewLine);

            foreach (var m in invasionMessageQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";
                StringBuilder invasionMsgToAppend = new StringBuilder(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false) + Environment.NewLine);
                
                if (m.NotifyClient)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, true));
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
            invasionMessagePostQueue.Clear();
        }

        private void PostVoidTraderMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            finalMsg.Append($"```VOID TRADER{(voidTraderMessagePostQueue.Count() == 0 ? " HAS LEFT" : String.Empty)}```" + Environment.NewLine);

            foreach (var m in voidTraderMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";
                
                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                finalMsg.Append("```" + Environment.NewLine);
            }

            if (traderMessage == null)
                traderMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), traderMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            voidTraderMessagePostQueue.Clear();
        }

        private void PostVoidFissureMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            if (voidFissureMessagePostQueue.Count > 0) finalMsg.Append("```VOID FISSURES```" + Environment.NewLine);
            else finalMsg.Append("```NO VOID FISSURES```" + Environment.NewLine);

            voidFissureMessagePostQueue.OrderBy(s => (s.wfEvent as WarframeVoidFissure).GetFissureIndex());

            foreach (var m in voidFissureMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";

                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                {
                    finalMsg.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false));
                }
                finalMsg.Append("```" + Environment.NewLine);
            }

            if (fissureMessage == null)
                fissureMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), fissureMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            voidFissureMessagePostQueue.Clear();
        }

        private void PostSortieMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();
            //Build all alert strings into a single message
            if (sortieMessagePostQueue.Count > 0) finalMsg.Append("```ACTIVE SORTIES```" + Environment.NewLine);
            else finalMsg.Append("```NO SORTIES```" + Environment.NewLine);

            foreach (var m in sortieMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```xl";

                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, true));
                }
                finalMsg.Append("```" + Environment.NewLine);
            }

            if (sortieMessage == null)
                sortieMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), sortieMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            sortieMessagePostQueue.Clear();
        }

        private void PostTimeCycleMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            List<string> messagesToNotify = new List<string>();

            foreach (var m in timeCycleMessagePostQueue)
            {
                string heading = (m.EventHasExpired) ? "```" : "```";

                finalMsg.Append(heading + Environment.NewLine
                    + WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false) + Environment.NewLine);
                postWillNotify = m.NotifyClient;

                if (postWillNotify)
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(m.wfEvent as dynamic, false));

                finalMsg.Append("```" + Environment.NewLine);
            }

            if (timeCycleMessage == null)
                timeCycleMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                EditMessage(finalMsg.ToString(), timeCycleMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }

            timeCycleMessagePostQueue.Clear();
        }

        private void AddToAlertPostQueue(WarframeAlert alert, bool notifyClient, bool alertHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            alertMessagePostQueue.Add(new MessageQueueEntry(alert, notifyClient, alertHasExpired));
        }

        private void AddToInvasionPostQueue(WarframeInvasion invasion, bool notifyClient, bool invasionHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            //invasionMessagePostQueue.Add(new MessageQueueEntry(message, notifyClient, notificationContent, invasionHasExpired));
            invasionMessagePostQueue.Add(new MessageQueueEntry(invasion, notifyClient, invasionHasExpired));
        }

        private void AddToVoidTraderPostQueue(WarframeVoidTrader trader, bool notifyClient, bool traderHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            voidTraderMessagePostQueue.Add(new MessageQueueEntry(trader, notifyClient, traderHasExpired));
        }

        private void AddToVoidFissurePostQueue(WarframeVoidFissure fissure, bool notifyClient, bool fissureHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            voidFissureMessagePostQueue.Add(new MessageQueueEntry(fissure, notifyClient, fissureHasExpired));
        }

        private void AddToSortiePostQueue(WarframeSortie sortie, bool notifyClient, bool sortieHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            sortieMessagePostQueue.Add(new MessageQueueEntry(sortie, notifyClient, sortieHasExpired));
        }

        private void AddToTimeCyclePostQueue(WarframeTimeCycleInfo cycle, bool notifyClient, bool eventHasExpired)
        {
            //Log("The following message was added to the queue:" + Environment.NewLine + (String.IsNullOrEmpty(message) ? "Empty string" : message));
            timeCycleMessagePostQueue.Add(new MessageQueueEntry(cycle, notifyClient, false));
        }

        public void Shutdown()
        {
            Log("Shutting down...");
            DeleteMessage(alertMessage);
            DeleteMessage(fissureMessage);
            DeleteMessage(sortieMessage);
            DeleteMessage(traderMessage);
            DeleteMessage(timeCycleMessage);

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

                    /*if (doingInitialRun)
                    {
                        if (e.message.content.StartsWith("?authenticate"))
                        {
                            string[] split = e.message.content.Split(new char[] { ' ' }, 2);
                            if (split.Length > 1)
                            {
                                if (codeToEnter.Trim() == split[1].Trim())
                                {
                                    config.OwnerID = e.author.ID;
                                    doingInitialRun = false;
                                    e.Channel.SendMessage("Authentication successful! **You are now my owner, " + e.author.Username + ".**");
                                    CommandsManager.AddPermission(e.author, PermissionType.Owner);
                                    owner = e.author;
                                }
                            }
                        }
                    }
                    else*/
                    {
                        if (e.Message.Content.Length > 0 && (e.Message.Content[0] == BotConfig.commandPrefix))
                        {
                            string rawCommand = e.Message.Content.Substring(1);

                            /*if (e.message.content.StartsWith("!dice"))
                            {
                                int max = 1, r;
                                string[] split = e.message.content.Split(new char[] { ' ' }, 2);

                                if (int.TryParse(split[1], out max))
                                {
                                    if (max > 1)
                                    {
                                        bool isCoin = (max > 2);
                                        r = RollDice(1, max + 1);

                                        if (max > 2)
                                        {
                                            _client.SendMessageToChannel(String.Format("@{0} D{1} rolled **{2}**", e.author.Username, max, r), e.Channel);
                                        }
                                        else
                                        {
                                            string coin_result = (r == 1) ? "**heads**" : "**tails**";
                                            _client.SendMessageToChannel(String.Format("@{0} Luckiest penny yielded {1}!", e.author.Username, coin_result), e.Channel);
                                        }
                                    }
                                    else
                                    {
                                        _client.SendMessageToChannel(String.Format("@{0} Try a larger number, you big dummy.", e.author.Username), e.Channel);
                                    }
                                }
                                else
                                {
                                    _client.SendMessageToChannel(String.Format("@{0} Enter a number larger than 2.", e.author.Username), e.Channel);
                                }
                            }*/
                            //try
                            //{
                            //CommandsManager.ExecuteCommand(rawCommand, e.Channel, e.author);
                            //}
                            //catch(UnauthorizedAccessException ex)
                            //{
                            //e.Channel.SendMessage(ex.Message);
                            //}
                            //catch(Exception ex)
                            //{
                            //e.Channel.SendMessage("Exception occurred while running command:\n```" + ex.Message + "\n```");
                            //}
                        }
                    }
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

                    //ExecuteSQLNonQuery($"DELETE FROM alerts", _dbConnection);
                    //ReadDatabase();
                    //loginDate = DateTime.Now;

                    /*if (!String.IsNullOrEmpty(config.OwnerID))
                        owner = client.GetServersList().Find(x => x.members.Find(y => y.ID == config.OwnerID) != null).members.Find(x => x.ID == config.OwnerID);
                    else
                    {
                        doingInitialRun = true;
                        RandomCodeGenerator rcg = new RandomCodeGenerator();
                        codeToEnter = rcg.GenerateRandomCode();

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Important: ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("\tPlease authenticate yourself as owner by typing the following into any Discord server you and the bot are in: ");
                        Console.WriteLine($"\t{config.CommandPrefix}authenticate " + codeToEnter);
                    }*/
                    /*CommandsManager = new CommandsManager(client);
                    if (File.Exists("permissions.json"))
                    {
                        var permissionsDictionary = JsonConvert.DeserializeObject<Dictionary<string, PermissionType>>(File.ReadAllText("permissions.json"));
                        CommandsManager.OverridePermissionsDictionary(permissionsDictionary);
                    }*/
                    //SetupCommands();

                    InitSubsystems();
                    //LoadState();

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
                eventsContainer.AlertScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Alert Scraped!");
#endif
                    bool alertIsNew = eventsContainer.IsAlertNew(e.Alert);
                    AddToAlertPostQueue(e.Alert, alertIsNew, e.Alert.IsExpired());
                };

                eventsContainer.InvasionScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Invasion Scraped!");
#endif
                    bool invasionIsNew = eventsContainer.IsInvasionNew(e.Invasion);
                    AddToInvasionPostQueue(e.Invasion, invasionIsNew, e.Invasion.IsExpired());
                };

                eventsContainer.VoidTraderScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Void Trader Scraped!");
#endif
                    AddToVoidTraderPostQueue(e.Trader, false, e.Trader.IsExpired());
                };

                eventsContainer.VoidFissureScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Fissure Scraped!");
#endif
                    AddToVoidFissurePostQueue(e.Fissure, false, e.Fissure.IsExpired());
                };

                eventsContainer.SortieScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Sortie Scraped!");
#endif
                    AddToSortiePostQueue(e.Sortie, false, e.Sortie.IsExpired());
                };

                eventsContainer.DayCycleScraped += (sender, e) =>
                {
#if DEBUG
                    Log("Day Cycle Scraped!");
#endif
                    AddToTimeCyclePostQueue(e.cycleInfo, false, false);
                };

                eventsContainer.ExistingAlertFound += (sender, e) =>
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
                            alertMessageAssociations.Add(e.Alert, targetMessage);
                        }
                    }
                };
                
                eventsContainer.Start();
                StartPostTimer();
            });
        }

        private Task ClientTask(DiscordClient client)
        {
            return Task.Run(() =>
            {
                client.MessageReceived += (sender, e) =>
                {
                    /*if (e.message.content.StartsWith("?joinvoice"))
                    {
                        string[] split = e.message.content.Split(new char[] { ' ' }, 2);
                        if (split[1] != "")
                        {
                            DiscordChannel toJoin = e.Channel.parent.channels.Find(x => (x.name.ToLower() == split[1].ToLower()) && (x.type == DiscordSharp.ChannelType.Voice));
                            if (toJoin != null)
                            {
                                client.ConnectToVoiceChannel(toJoin);
                            }
                        }
                    }
                    else if (e.message.content.StartsWith("?voice"))
                    {
                        string[] split = e.message.content.Split(new char[] { ' ' }, 2);
                        if (File.Exists(split[1]))
                            DoVoice(client.GetVoiceClient(), split[1]);
                    }
                    else if (e.message.content.StartsWith("!disconnect"))
                    {
                        client.DisconnectFromVoice();
                    }
                    else */
                    if (e.Message.Content.StartsWith("!help"))
                    {
                        client.SendMessageToUser("I am helping :)", e.Message.Author);
                    }
                    else if (e.Message.Content.StartsWith("!proxymsg"))
                    {
                        string[] split = e.Message.Content.Split(new char[] { ' ' }, 3);
                        if (split[1] != "")
                        {
                            /*DiscordChannel toJoin = e.Channel.parent.channels.Find(x => (x.name.ToLower() == split[1].ToLower()) && (x.type == DiscordSharp.ChannelType.Voice));
                            if (toJoin != null)
                            {
                                client.ConnectToVoiceChannel(toJoin);
                            }*/
                        }
                    }
                    /*else if (e.message.content.StartsWith("!announce"))
                    {
                    }*/
                    else if (e.Message.Content.StartsWith("!dice"))
                    {
                        int max = 1, r;
                        string[] split = e.Message.Content.Split(new char[] { ' ' }, 2);

                        if (int.TryParse(split[1], out max))
                        {
                            if (max > 1)
                            {
                                bool isCoin = (max > 2);
                                r = RollDice(1, max + 1);

                                if (max > 2)
                                {
                                    client.SendMessageToChannel(String.Format("@{0} D{1} rolled **{2}**", e.Author.Username, max, r), e.Channel);
                                }
                                else
                                {
                                    string coin_result = (r == 1) ? "**heads**" : "**tails**";
                                    client.SendMessageToChannel(String.Format("@{0} Luckiest penny yielded {1}!", e.Author.Username, coin_result), e.Channel);
                                }
                            }
                            else
                            {
                                client.SendMessageToChannel(String.Format("@{0} Try a larger number, you big dummy.", e.Author.Username), e.Channel);
                            }
                        }
                        else
                        {
                            client.SendMessageToChannel(String.Format("@{0} Enter a number larger than 2.", e.Author.Username), e.Channel);
                        }
                    }
                    /*else if (e.message.content.StartsWith("!privileges"))
                    {
                        bool isPriviledged = false;
                        foreach (DiscordRole i in e.author.roles)
                        {
                            //client.SendMessageToChannel(string.Format("@{0}: {1}", e.author.user.username, i.name), e.message.channel);
                            if (i.name == "@privileged")
                            {
                                isPriviledged = true;
                            }
                        }

                        if (isPriviledged)
                            client.SendMessageToChannel(string.Format("@{0} You are privileged :D", e.author.user.username), e.Channel);
                        else
                            client.SendMessageToChannel(string.Format("@{0} You are not privileged :(", e.author.user.username), e.Channel);
                    }
                    else if (e.message.content.StartsWith("!time"))
                    {
                        client.SendMessageToChannel(string.Format("@{0} The current time is {1}", e.message.author.user.username, System.DateTime.Now.ToString("h:mm tt")), e.message.channel);
                    }
                    else if (e.message.content.StartsWith("!userid"))
                    {
                        client.SendMessageToUser(string.Format("Your user ID is {0}", e.message.author.user.id), e.message.author);
                    }
                    else if (e.message.content.StartsWith("@" + client.Me.user.id))
                    {
                        string message;
                        if (e.author.user.username != "wubby")
                            message = string.Format("@{0} Hi :)", e.author.user.username);
                        else
                            message = string.Format("@{0} Please don't talk to me", e.author.user.username);
                        client.SendMessageToUser(string.Format("Your user ID is {0}", e.message.author.user.id), e.message.author);
                    }*/
                };

                client.MessageEdited += (sender, e) =>
                {
                    Console.WriteLine(string.Format("I detected that a message was editted by {0}", e.Author.Username));
                };

                client.Connected += (sender, e) =>
                {
                    Console.WriteLine("Connected as " + e.User.Username);
                    //client.UpdateCurrentGame("With Your Emotions!", false);
                };
                client.Connect();
            });
        }

        //Send a log in request
        public async Task<bool> Connect()
        {
            bool x = false;
            if (Client.SendLoginRequest() != null)
                await ClientTask(Client);
            return x;
        }

        private int RollDice(int min, int max)
        {
            return _randomNumGen.Next(min, max);
        }
    }
}
