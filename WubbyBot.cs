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
using Tweetinvi;
using Tweetinvi.Streams;
using Tweetinvi.Core.Interfaces.Streaminvi;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace DiscordSharpTest
{
    struct MessageQueueEntry
    {
        public string Content;
        public bool NotifyClient;
        public bool AlertHasExpired;

        public MessageQueueEntry(string content, bool notify, bool alertHasExpired)
        {
            NotifyClient = notify;
            Content = content;
            AlertHasExpired = alertHasExpired;
        }
    };

    class WubbyBot : DiscordBot
    {
#if DEBUG
        const string ALERTS_CHANNEL = "wf-dev";
#else
        const string ALERTS_CHANNEL = "wf-worldstate";
#endif
        const string ALERTS_ARCHIVE_CHANNEL = "wf-alert-archive";

        private Random _randomNumGen;

        //Events
        private Timer _eventUpdateInterval { get; set; }

        //Miscellaneous
        private string _currentGame { get; set; }

        //Cut out the middle-man "helper" class.
        private WarframeEventsContainer eventsContainer;
        private WarframeEventMessageBuilder messageBuilder;
        private Dictionary<WarframeAlert, DiscordMessage> alertMessageAssociations;
        private EventsDatabase database;
        private List<MessageQueueEntry> alertMessagePostQueue;

        private DiscordMessage alertMessage = null;

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
            
            /*_activeAlerts = new Dictionary<WarframeAlert, DiscordMessage>();
            _activeInvasions = new Dictionary<WarframeInvasion, DiscordMessage>();*/

            //_eventUpdateInterval = new Timer((e) => UpdateAlerts(), null, 0, (int)(TimeSpan.FromMinutes(1).TotalMilliseconds));

            //Auth.SetUserCredentials(BotConfig.consumerKey, BotConfig.consumerSecret, BotConfig.accessToken, BotConfig.accessTokenSecret);
            //_alertsStream = Tweetinvi.Stream.CreateFilteredStream(); 
            //_alertsStream.AddFollow(1966470036);
#if DEBUG
            //_alertsStream.AddFollow(708307281436397568);
#endif
            //SetupTwitterStreamEvents();
            SetupEvents();
        }

        private void InitSubsystems()
        {
            Log("Initialising sub-systems");
            eventsContainer = new WarframeEventsContainer();
            messageBuilder = new WarframeEventMessageBuilder();
            alertMessageAssociations = new Dictionary<WarframeAlert, DiscordMessage>();
            database = new EventsDatabase();

            //eventsContainer.HandleOldAlerts(database.ReadDatabase());
            //eventsContainer.Start();

            alertMessagePostQueue = new List<MessageQueueEntry>();
            //alertMessagePostQueue.Add(new MessageQueueEntry("Alerts", false, false));
            _eventUpdateInterval = new Timer((e) => PostAlertMessage(), null, 0, (int)(TimeSpan.FromMinutes(1).TotalMilliseconds));

            Log("Sub-systems initialised");
        }

        private void DeleteOldEventMessages()
        {
            Dictionary<WarframeAlert, string> oldAlerts = database.ReadDatabase();
            
            foreach(var item in oldAlerts)
            {
                DiscordMessage m = GetMessageByID(item.Value, Client.GetChannelByName(ALERTS_CHANNEL));
                if (m != null)
                    Client.DeleteMessage(m);
            }
        }

        private void PostAlertMessage()
        {
            StringBuilder finalMsg = new StringBuilder();
            bool postWillNotify = false;
            //Build all alert strings into a single message
            finalMsg.Append("```ACTIVE ALERTS```" + Environment.NewLine);

            foreach (var m in alertMessagePostQueue)
            {
                string heading = (m.AlertHasExpired) ? "```" : "```xl";
                finalMsg.Append(heading);
                finalMsg.Append(Environment.NewLine);
                finalMsg.Append(m.Content);
                finalMsg.Append("```");
                finalMsg.Append(Environment.NewLine);
                postWillNotify = m.NotifyClient;
            }

            /*if (alertMessage != null)
                Client.DeleteMessage(alertMessage);*/
            /*if (alertMessage != null)
            {
                if (!postWillNotify)
                    EditMessage(finalMsg.ToString(), alertMessage, Client.GetChannelByName(ALERTS_CHANNEL));
                else
                    alertMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
            }
            else
                alertMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));*/

            if ((!postWillNotify) && (alertMessage != null))
                EditMessage(finalMsg.ToString(), alertMessage, Client.GetChannelByName(ALERTS_CHANNEL));
            else
            {
                if (alertMessage != null)
                    Client.DeleteMessage(alertMessage);
                alertMessage = SendMessage(finalMsg.ToString(), Client.GetChannelByName(ALERTS_CHANNEL));
                //SaveState();
            }

            alertMessagePostQueue.Clear();
        }

        private void AddToAlertPostQueue(string message, bool notifyClient, bool alertHasExpired)
        {
#if DEBUG
            Log("Added message to post queue");
#endif
            alertMessagePostQueue.Add(new MessageQueueEntry(message, notifyClient, alertHasExpired));
        }

        private void SaveState()
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(@"wubbybot-state.txt");
            file.WriteLine(alertMessage.ID);
            file.Close();
        }

        private void LoadState()
        {
            try
            {
                alertMessage = GetMessageByID(File.ReadAllLines(@"wubbybot-state.txt")[0], Client.GetChannelByName(ALERTS_CHANNEL));
                DeleteMessage(alertMessage);
            }
            catch (FileNotFoundException e)
            {
                Log($"{e.FileName} could not be found.");
            }
            catch (IndexOutOfRangeException)
            {
                Log($"No message ID found");
            }
        }

        private void UpdateAlerts()
        {
#if DEBUG
            Log("UpdateAlerts()");
#endif

            /*foreach (var alert in _activeAlerts.ToList())
            {
                alert.Item1.UpdateStatus();
                ProcessAlertMessage(alert.Item1);
            }

            if ((_activeAlerts.Count() > 0) && (_activeAlerts.First().Item1.MinutesRemaining <= 0))
            {
                ProcessAlertMessage(_activeAlerts.First().Item1);
                _activeAlerts.Remove(_activeAlerts.First());
            }*/
        }

        /*private DiscordMessage ProcessAlertMessage(WarframeEvent alert)
        {
            if (!string.IsNullOrEmpty(alert.AssociatedMessageID))
            {
                if (alert.MinutesRemaining > 0)
                {
#if DEBUG
                    Log($"Updating Alert Message ({alert.AssociatedMessageID})");

                    if (!String.IsNullOrEmpty(alert.AssociatedMessageID))
                        SendMessage(alert.AssociatedMessageID, Client.GetChannelByName(ALERTS_CHANNEL));
                    else
                        SendMessage("No associated message ID set!", Client.GetChannelByName(ALERTS_CHANNEL));
#endif
                    return Client.EditMessage(alert.AssociatedMessageID, $"Destination: **{alert.DestinationName}**\nMission: **{alert.Mission} ({alert.Faction})**\nReward: **{alert.Loot}, {alert.Credits}**\nExpires: **{alert.ExpirationTime:HH:mm} ({alert.MinutesRemaining}m)**", Client.GetChannelByName(ALERTS_CHANNEL));
                }
                else
                {
                    //Delete the entry from the database
                    ExecuteSQLNonQuery($"DELETE FROM alerts WHERE alertID = '{alert.AssociatedMessageID}'", _dbConnection);
                    return Client.EditMessage(_activeAlerts.First().Item1.AssociatedMessageID,
                    $"Destination: {_activeAlerts.First().Item1.DestinationName}\nMission: {_activeAlerts.First().Item1.Mission} ({_activeAlerts.First().Item1.Faction})\nReward: {_activeAlerts.First().Item1.Loot}, {_activeAlerts.First().Item1.Credits}\nStatus: *Expired {_activeAlerts.First().Item1.ExpirationTime:HH:mm}*",
                    Client.GetChannelByName(ALERTS_CHANNEL));
                }
            }
            else
            {
#if DEBUG
                if (!String.IsNullOrEmpty(alert.AssociatedMessageID))
                    SendMessage(alert.AssociatedMessageID, Client.GetChannelByName(ALERTS_CHANNEL));
                else
                    SendMessage("No associated message ID set!", Client.GetChannelByName(ALERTS_CHANNEL));
#endif

                return SendMessage($"Destination: **{alert.DestinationName}** \nMission: **{alert.Mission} ({alert.Faction})**\nReward: **{alert.Loot}, {alert.Credits}**\nExpires: **{alert.ExpirationTime:HH:mm} ({alert.MinutesRemaining}m)**", Client.GetChannelByName(ALERTS_CHANNEL));
            }
        }*/

        public void Shutdown()
        {
            Log("Shutting down...");
            DeleteMessage(alertMessage);
            //StopStream();
            //SendMessage($"*{Name} is now offline*", Client.GetChannelByName(ALERTS_CHANNEL));
        }

        /*private Task SetupTwitterStreamEvents()
        {
            return Task.Run(() =>
            {

            _alertsStream.StreamStarted += (sender, args) =>
            {
                //Log("Stream started");
            };

            _alertsStream.StreamStopped += (sender, args) =>
            {
                var exception = args.Exception;
                var disconnectMessage = args.DisconnectMessage;
                if (args != null)
                {
                    Log($"Stream has stopped: {args.Exception}, \n{args.DisconnectMessage}");
                    SendMessage($"@Wubby STREAM HAS STOPPED: {args.Exception} \n-{args.DisconnectMessage}", Client.GetChannelByName("dev"));
                }
            };

            _alertsStream.MatchingTweetReceived += (sender, args) =>
            {
#if DEBUG
                Log("_userStream.MatchingTweetReceived");
#endif
                //var newAlert = new WarframeAlert(args.Tweet.Text);
                var newAlert = WarframeAlertTweetParser.ParseTweet(args.Tweet.Text);

                DiscordMessage alertMessage = ProcessAlertMessage(newAlert);
                newAlert.AssociatedMessageID = alertMessage.ID;
                //Insert this entry into the database
                if (_dbConnection != null)
                {
                    ExecuteSQLNonQuery(
                        $"INSERT INTO alerts (destinationName, factionName, missionName, credits, lootName, expirationTime, alertID)" +
                        $" VALUES ('{newAlert.DestinationName}', '{newAlert.Faction}', '{newAlert.Mission}', {newAlert.Credits}, '{newAlert.Loot}', '{newAlert.ExpirationTime:yyyy-MM-dd HH:mm:ss}', '{newAlert.AssociatedMessageID}')",
                        _dbConnection);
                }

                _activeAlerts.Add(new Tuple<WarframeEvent, DiscordMessage>(newAlert, alertMessage));
                _activeAlerts.Sort((Tuple<WarframeEvent, DiscordMessage> a, Tuple<WarframeEvent, DiscordMessage> b) =>
                {
                    return a.Item1.ExpirationTime.CompareTo(b.Item1.ExpirationTime);
                }
                );
            };
                //ReadDatabase();
                StartStream();
            }
            );
        }*/

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
                Client.UpdateCurrentGame(_currentGame, false);
                InitSubsystems();
                //LoadState();

                SetupWarframeEventsTask();
            }
            );
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
                    AddToAlertPostQueue(messageBuilder.BuildMessage(e.Alert, false), alertIsNew, e.Alert.IsExpired());
                };

                eventsContainer.InvasionScraped += (sender, e) =>
                {

                };

                eventsContainer.AlertExpired += (sender, e) =>
                {
                    Log("Alert Expired");
                    if (Client.ReadyComplete == true)
                    {
                        /*DiscordMessage targetMessage = null;
                        targetMessage = GetMessageByID(e.MessageID, Client.GetChannelByName(ALERTS_CHANNEL));

                        if (targetMessage == null)
                        {
                            Log($"Message {e.MessageID} could not be found.");
                        }
                        else
                        {
#if DEBUG
                            Log($"Message {e.MessageID} was found.");
#endif
                            Client.EditMessage(targetMessage.ID, messageBuilder.BuildMessage(e.Alert, true), Client.GetChannelByName(ALERTS_CHANNEL));
                        }*/

                        
                    }
                    database.DeleteAlert(e.Alert);
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

                //DeleteOldEventMessages();
                //eventsContainer.HandleOldAlerts(database.ReadDatabase());
                eventsContainer.Start();
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
    }
}
