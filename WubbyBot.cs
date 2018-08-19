using DiscordWrapper;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WarframeWorldStateAPI.Components;
using WarframeWorldStateAPI.WarframeEvents;
using WubbyBot.Events.Extensions;

namespace DiscordSharpTest
{
    /// <summary>
    /// This is an extension of DiscordBot implementing specific features for Warframe
    /// </summary>
    public class WubbyBot : DiscordBot
    {

        private long _alertsChannelID = long.Parse(ConfigurationManager.AppSettings["WubbyBotAlertsChannel"]);
        private ulong _alertChannelId = ulong.Parse(ConfigurationManager.AppSettings["WubbyBotAlertChannel"]);
        private ulong _invasionChannelId = ulong.Parse(ConfigurationManager.AppSettings["WubbyBotInvasionChannel"]);
        private ulong _sortieChannelId = ulong.Parse(ConfigurationManager.AppSettings["WubbyBotSortieChannel"]);
        private ulong _fissureChannelId = ulong.Parse(ConfigurationManager.AppSettings["WubbyBotFissureChannel"]);
        private ulong _voidTraderChannelId = ulong.Parse(ConfigurationManager.AppSettings["WubbyBotVoidTraderChannel"]);
        private ulong _earthChannelId = ulong.Parse(ConfigurationManager.AppSettings["WubbyBotEarthChannel"]);
        private ulong _acolyteChannelId = ulong.Parse(ConfigurationManager.AppSettings["WubbyBotAcolyteChannel"]);

        private const string ALERTS_ARCHIVE_CHANNEL = "wf-alert-archive";

        private const int EVENT_UPDATE_TIMER_DUE_TIME_MILLISECONDS = 3000;
        private const int EVENT_UPDATE_INTERVAL_MILLISECONDS = 60000;

        private readonly Random _randomNumGen;

        /// <summary>
        /// This is how often we will update our Discord messages and scrape for new event information
        /// </summary>
        private Timer _eventCheckTimer;
        private Timer _eventUpdateTimer;
        private WarframeEventInformationParser _eventsParser = new WarframeEventInformationParser();

        /// <summary>
        /// These lists store containers which hold information such as message content and additional property information
        /// </summary>
        private List<MessageQueueElement<WarframeAlert>> _alertMessagePostQueue = new List<MessageQueueElement<WarframeAlert>>();
        private List<MessageQueueElement<WarframeInvasion>> _invasionMessagePostQueue = new List<MessageQueueElement<WarframeInvasion>>();
        private List<MessageQueueElement<WarframeInvasionConstruction>> _invasionConstructionMessagePostQueue = new List<MessageQueueElement<WarframeInvasionConstruction>>();
        private List<MessageQueueElement<WarframeVoidTrader>> _voidTraderMessagePostQueue = new List<MessageQueueElement<WarframeVoidTrader>>();
        private List<MessageQueueElement<WarframeVoidFissure>> _voidFissureMessagePostQueue = new List<MessageQueueElement<WarframeVoidFissure>>();
        private List<MessageQueueElement<WarframeSortie>> _sortieMessagePostQueue = new List<MessageQueueElement<WarframeSortie>>();
        private List<MessageQueueElement<WarframeTimeCycleInfo>> _timeCycleMessagePostQueue = new List<MessageQueueElement<WarframeTimeCycleInfo>>();
        private List<MessageQueueElement<WarframeOstronBounty>> _ostronBountyMessagePostQueue = new List<MessageQueueElement<WarframeOstronBounty>>();
        private List<MessageQueueElement<WarframeOstronBountyCycle>> _ostronBountyCyclePostQueue = new List<MessageQueueElement<WarframeOstronBountyCycle>>();
        private List<MessageQueueElement<WarframeAcolyte>> _acolyteMessagePostQueue = new List<MessageQueueElement<WarframeAcolyte>>();
        
        /// <summary>
        /// These are the Discord message representations of all Warframe events
        /// </summary>
        private DiscordMessage _alertMessage;
        private DiscordMessage _traderMessage;
        private DiscordMessage _fissureMessage;
        private DiscordMessage _sortieMessage;
        private DiscordMessage _timeCycleMessage;
        private DiscordMessage _ostronBountyInformationMessage;
        private DiscordMessage _acolyteMessage;

        /// <summary>
        /// Store invasion discord messages in a list, as the number of invasions can sometimes cause the Discord message to exceed the maximum character limit
        /// </summary>
        private List<DiscordMessage> _invasionMessages = new List<DiscordMessage>();

        public WubbyBot(string name, string devLogName = "") : base(name, devLogName)
        {
            _randomNumGen = new Random((int)DateTime.Now.Ticks);
        }

        //Start the task
        public void Init()
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

            Client.MessageCreated += e =>
            {
                var task = new Task(() =>
                {
                    if (e.Channel.Name != LogChannelName)
                    {
                        Log($"Message from {e.Author.Username} in #{e.Channel.Name} on {e.Guild?.Name}: {e.Message.Id}");
                    }
                });
                task.Start();
                return task;
            };

            Client.Ready += e =>
            {
                var task = new Task(async () =>
                {
                    Log($"Connected as {e.Client.CurrentUser.Username}");
                    await AssignExistingChannelMessagesAsync();
                    StartPostTimer();

                    SetCurrentGame(false);
                });
                task.Start();
                return task;
            };

            Connect();
        }

        private async Task AssignExistingChannelMessagesAsync()
        {
            _alertMessage = (await GetChannelMessagesAsync(GetChannelByID(_alertChannelId))).FirstOrDefault();
            _invasionMessages = (await GetChannelMessagesAsync(GetChannelByID(_invasionChannelId))).ToList();
            _sortieMessage = (await GetChannelMessagesAsync(GetChannelByID(_sortieChannelId))).FirstOrDefault();
            _fissureMessage = (await GetChannelMessagesAsync(GetChannelByID(_fissureChannelId))).FirstOrDefault();
            _traderMessage = (await GetChannelMessagesAsync(GetChannelByID(_voidTraderChannelId))).FirstOrDefault();
            _timeCycleMessage = (await GetChannelMessagesAsync(GetChannelByID(_earthChannelId))).FirstOrDefault();
            _ostronBountyInformationMessage = (await GetChannelMessagesAsync(GetChannelByID(_earthChannelId))).FirstOrDefault();
            _acolyteMessage = (await GetChannelMessagesAsync(GetChannelByID(_acolyteChannelId))).FirstOrDefault();
        }

        private void CheckForWarframeEvents()
        {
            foreach (var alert in _eventsParser.GetAlerts())
            {
                AddToAlertPostQueue(alert, alert.IsNew(), alert.IsExpired());
            }

            foreach (var invasion in _eventsParser.GetInvasions())
            {
                AddToInvasionPostQueue(invasion, invasion.IsNew(), invasion.IsExpired());
            }

            foreach (var project in _eventsParser.GetInvasionConstruction())
            {
                AddToInvasionConstructionPostQueue(project, false);
            }

            foreach (var fissure in _eventsParser.GetVoidFissures())
            {
                AddToVoidFissurePostQueue(fissure, false, fissure.IsExpired());
            }

            foreach (var sortie in _eventsParser.GetSorties())
            {
                AddToSortiePostQueue(sortie, false, sortie.IsExpired());
            }

            foreach (var trader in _eventsParser.GetVoidTrader())
            {
                AddToVoidTraderPostQueue(trader, false, trader.IsExpired());
            }

            foreach (var acolyte in _eventsParser.GetAcolytes())
            {
                AddToAcolytePostQueue(acolyte, acolyte.IsLocated(), acolyte.IsExpired());
            }

            foreach (var bounty in _eventsParser.GetOstronBounties())
            {
                AddToBountyPostQueue(bounty, bounty.IsNew(), bounty.IsExpired());
            }

            AddToTimeCyclePostQueue(_eventsParser.GetTimeCycle(), false);
            AddToOstronBountyCyclePostQueue(_eventsParser.GetOstronBountyCycle(), false);
        }
        
        /// <summary>
        /// Start application operation cycle
        /// </summary>
        private void StartPostTimer()
        {
            _eventCheckTimer = new Timer((e) =>
            {
                CheckForWarframeEvents();
#if DEBUG
                Log($"{_alertMessagePostQueue.Count} Alert(s) Scraped!");
                Log($"{_invasionMessagePostQueue.Count} Invasion(s) Scraped!");
                Log($"{_voidFissureMessagePostQueue.Count} Fissure(s) Scraped!");
                Log($"{_sortieMessagePostQueue.Count} Sortie(s) Scraped!");
                Log($"{_voidTraderMessagePostQueue.Count} Trader(s) Scraped!");
                Log($"{_acolyteMessagePostQueue.Count} Acolyte(s) Scraped!");
#endif
            },
            null, EVENT_UPDATE_TIMER_DUE_TIME_MILLISECONDS, EVENT_UPDATE_INTERVAL_MILLISECONDS / 2);

            _eventUpdateTimer = new Timer((e) =>
            {
                PostAlertMessage();
                PostInvasionMessage();
                PostSortieMessage();
                PostVoidFissureMessage();
                PostVoidTraderMessage();
                PostTimeCycleMessage();
                PostOstronBountyCycleMessage();
                PostAcolyteMessage();
            },
            null, EVENT_UPDATE_TIMER_DUE_TIME_MILLISECONDS, EVENT_UPDATE_INTERVAL_MILLISECONDS);
        }
        
        /// <summary>
        /// Build and post the Discord message for alerts
        /// </summary>
        private void PostAlertMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            if (_alertMessagePostQueue.Count == 0)
            {
                finalMessage.Append(WarframeEventExtensions.FormatMessage("NO ACTIVE ALERTS", preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            _alertMessagePostQueue = _alertMessagePostQueue
                .OrderBy(s => s.WarframeEvent.GetMinutesRemaining(false)).ToList();

            foreach (var message in _alertMessagePostQueue)
            {
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                var messageMarkdownPreset = MessageMarkdownLanguageIdPreset.ActiveEvent;

                if (message.NotifyClient)
                {
                    coreMessageContent.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, true));
                    messageMarkdownPreset = MessageMarkdownLanguageIdPreset.NewEvent;
                }
                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: messageMarkdownPreset, formatType: MessageFormat.CodeBlocks));
            }

            if (_alertMessage == null)
            {
                _alertMessage = SendMessageToChannel(finalMessage.ToString(), _alertChannelId);
            }
            else
            {
                EditEventMessage(finalMessage.ToString(), _alertMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _alertChannelId);
                }
            }
            _alertMessagePostQueue.Clear();
        }
        
        /// <summary>
        /// Build and post the Discord message for invasions
        /// </summary>
        private void PostInvasionMessage()
        {
            var finalMessagesToPost = new List<StringBuilder>();
            var messagesToNotify = new List<string>();

            //Messages will append to this builder until the length reaches the MESSAGE_CHAR_LIMIT value
            //Due to the potential length of an invasion message, it may need to be broken down into smaller messages. Hence - entryForFinalMessage
            var entryForFinalMessage = new StringBuilder();
            finalMessagesToPost.Add(entryForFinalMessage);

            if (_invasionMessagePostQueue.Count == 0)
            {
                entryForFinalMessage.Append(WarframeEventExtensions.FormatMessage("NO ACTIVE INVASIONS", preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            _invasionMessagePostQueue = _invasionMessagePostQueue
                .OrderByDescending(s => Math.Abs(s.WarframeEvent.Progress)).ToList();

            foreach (var message in _invasionMessagePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContentEntry = new StringBuilder();
                coreMessageContentEntry.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                MessageMarkdownLanguageIdPreset markdownPreset = MessageMarkdownLanguageIdPreset.ActiveEvent;

                if (message.NotifyClient)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, true));
                    coreMessageContentEntry.Append("( new )");
                    markdownPreset = MessageMarkdownLanguageIdPreset.NewEvent;
                }

                //Create a new entry in the post queue if the character length of the current message hits the character limit
                if (entryForFinalMessage.Length + coreMessageContentEntry.Length < MESSAGE_CHAR_LIMIT)
                {
                    entryForFinalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContentEntry.ToString(), preset: markdownPreset, formatType: MessageFormat.CodeBlocks));
                }
                else
                {
                    entryForFinalMessage.Append(coreMessageContentEntry.ToString());
                    finalMessagesToPost.Add(entryForFinalMessage);
                }
            }

            //Project Construction Information
            var constructionMessage = new StringBuilder();
            if (_invasionConstructionMessagePostQueue.Count > 0)
            {
                foreach (var message in _invasionConstructionMessagePostQueue)
                {
                    constructionMessage.Append(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                }

                entryForFinalMessage.Append(WarframeEventExtensions.FormatMessage(constructionMessage.ToString(), preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            if (_invasionMessages.Count > 0)
            {
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _invasionChannelId);
                }
            }

            for (var i = 0; i < finalMessagesToPost.Count; ++i)
            {
                //If invasion messages already exist
                if (i < _invasionMessages.Count)
                {
                    EditEventMessage(finalMessagesToPost.ElementAt(i).ToString(), _invasionMessages.ElementAt(i));
                }
                else //When we run out of available invasion messages to edit
                {
                    _invasionMessages.Add(SendMessageToChannel(finalMessagesToPost.ElementAt(i).ToString(), _invasionChannelId));
                }
            }

            //Get rid of any extra messages which have been created as a result of long character counts in Discord messages
            if (_invasionMessages.Count > finalMessagesToPost.Count)
            {
                var range = _invasionMessages.GetRange(finalMessagesToPost.Count, _invasionMessages.Count - finalMessagesToPost.Count);
                range.ForEach(msg => DeleteMessage(msg));

                _invasionMessages.RemoveRange(finalMessagesToPost.Count, _invasionMessages.Count - finalMessagesToPost.Count);
            }
#if DEBUG
            foreach (var i in finalMessagesToPost)
            {
                Log(i.Length + " characters long");
            }
#endif
            _invasionMessagePostQueue.Clear();
            _invasionConstructionMessagePostQueue.Clear();
        }

        /// <summary>
        /// Build and post the Discord message for Void Traders
        /// </summary>
        private void PostVoidTraderMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();
            finalMessage.Append(WarframeEventExtensions.FormatMessage($"VOID TRADER{(_voidTraderMessagePostQueue.Count == 0 ? " HAS LEFT" : string.Empty)}", MessageMarkdownLanguageIdPreset.ActiveEvent, string.Empty, MessageFormat.Bold));

            //Core content of the Discord message without any formatting
            var coreMessageContent = new StringBuilder();

            foreach (var message in _voidTraderMessagePostQueue)
            {
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false) + Environment.NewLine);
            }
            finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));

            if (_traderMessage == null)
            {
                _traderMessage = SendMessageToChannel(finalMessage.ToString(), _voidTraderChannelId);
            }
            else
            {
                EditEventMessage(finalMessage.ToString(), _traderMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _voidTraderChannelId);
                }
            }

            _voidTraderMessagePostQueue.Clear();
        }
        
        /// <summary>
        /// Build and post the Discord message for Void Fissures
        /// </summary>
        private void PostVoidFissureMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            if (_voidFissureMessagePostQueue.Count == 0)
            {
                finalMessage.Append(WarframeEventExtensions.FormatMessage("NO VOID FISSURES", preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            _voidFissureMessagePostQueue = _voidFissureMessagePostQueue
                .OrderBy(s => s.WarframeEvent.GetFissureIndex())
                .ThenBy(s => s.WarframeEvent.GetMinutesRemaining(false)).ToList();

            foreach (var message in _voidFissureMessagePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                var markdownPreset = MessageMarkdownLanguageIdPreset.ActiveEvent;

                if (message.NotifyClient)
                {
                    coreMessageContent.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                    markdownPreset = MessageMarkdownLanguageIdPreset.NewEvent;
                }
                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: markdownPreset, formatType: MessageFormat.CodeBlocks));
            }

            if (_fissureMessage == null)
            {
                _fissureMessage = SendMessageToChannel(finalMessage.ToString(), _fissureChannelId);
            }
            else
            {
                EditEventMessage(finalMessage.ToString(), _fissureMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _fissureChannelId);
                }
            }

            _voidFissureMessagePostQueue.Clear();
        }
        
        /// <summary>
        /// Build and post the Discord message for sorties
        /// </summary>
        private void PostSortieMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            if (_sortieMessagePostQueue.Count == 0)
            {
                finalMessage.Append(WarframeEventExtensions.FormatMessage("NO SORTIES", preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            foreach (var message in _sortieMessagePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                if (message.NotifyClient)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, true));
                }
                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            if (_sortieMessage == null)
            {
                _sortieMessage = SendMessageToChannel(finalMessage.ToString(), _sortieChannelId);
            }
            else
            {
                EditEventMessage(finalMessage.ToString(), _sortieMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _sortieChannelId);
                }
            }

            _sortieMessagePostQueue.Clear();
        }

        /// <summary>
        /// Build and post the Discord message for day cycle information
        /// </summary>
        private void PostTimeCycleMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            finalMessage.Append(WarframeEventExtensions.FormatMessage("DAY CYCLE", preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.Bold));

            foreach (var message in _timeCycleMessagePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                if (message.NotifyClient)
                {
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                }

                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            if (_timeCycleMessage == null)
            {
                _timeCycleMessage = SendMessageToChannel(finalMessage.ToString(), _earthChannelId);
            }
            else
            {
                EditEventMessage(finalMessage.ToString(), _timeCycleMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _earthChannelId);
                }
            }

            _timeCycleMessagePostQueue.Clear();
        }

        private void PostOstronBountyCycleMessage()
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            finalMessage.Append(WarframeEventExtensions.FormatMessage("OSTRON BOUNTIES", preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.Bold));

            foreach (var message in _ostronBountyCyclePostQueue)
            {
                //Core content of the Discord message without any formatting
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));

                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }
            
            if (_ostronBountyMessagePostQueue.Count > 0)
            {
                foreach (var message in _ostronBountyMessagePostQueue)
                {
                    var bountiesMessage = new StringBuilder().AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                    finalMessage.Append(WarframeEventExtensions.FormatMessage(bountiesMessage.ToString(), preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
                }
            }
            var t = finalMessage.ToString();
            var p = t.Length;

            if (_ostronBountyInformationMessage == null)
            {
                _ostronBountyInformationMessage = SendMessageToChannel(finalMessage.ToString(), _earthChannelId);
            }
            else
            {
                EditEventMessage(finalMessage.ToString(), _ostronBountyInformationMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _earthChannelId);
                }
            }

            _ostronBountyMessagePostQueue.Clear();
            _ostronBountyCyclePostQueue.Clear();
        }

        private void PostAcolyteMessage()
        {
            //Ignore the acolytes section if there aren't any
            if (_acolyteMessagePostQueue.Count() == 0)
            {
                return;
            }

            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            foreach (var message in _acolyteMessagePostQueue)
            {
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, false));
                var markdownPreset = MessageMarkdownLanguageIdPreset.ActiveEvent;

                if (message.NotifyClient)
                {
                    coreMessageContent.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(message.WarframeEvent as dynamic, true));
                    markdownPreset = MessageMarkdownLanguageIdPreset.NewEvent;
                }
                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: markdownPreset, formatType: MessageFormat.CodeBlocks));
            }

            if (_acolyteMessage == null)
            {
                _acolyteMessage = SendMessageToChannel(finalMessage.ToString(), _acolyteChannelId);
            }
            else
            {
                EditEventMessage(finalMessage.ToString(), _acolyteMessage);
                foreach (var item in messagesToNotify)
                {
                    NotifyClient(item, _acolyteChannelId);
                }
            }
            _acolyteMessagePostQueue.Clear();
        }

        private void AddToAlertPostQueue(WarframeAlert alert, bool notifyClient, bool alertHasExpired)
        {
            if (!_alertMessagePostQueue.Any(x => x.WarframeEvent.GUID == alert.GUID))
            {
                _alertMessagePostQueue.Add(new MessageQueueElement<WarframeAlert>(alert, notifyClient, alertHasExpired));
            }
        }

        private void AddToInvasionPostQueue(WarframeInvasion invasion, bool notifyClient, bool invasionHasExpired)
        {
            if (!_invasionMessagePostQueue.Any(x => x.WarframeEvent.GUID == invasion.GUID))
            {
                _invasionMessagePostQueue.Add(new MessageQueueElement<WarframeInvasion>(invasion, notifyClient, invasionHasExpired));
            }
        }

        private void AddToInvasionConstructionPostQueue(WarframeInvasionConstruction construction, bool invasionHasExpired)
        {
            if (!_invasionConstructionMessagePostQueue.Any(x => x.WarframeEvent.GUID == construction.GUID))
            {
                _invasionConstructionMessagePostQueue.Add(new MessageQueueElement<WarframeInvasionConstruction>(construction, false, invasionHasExpired));
            }
        }

        private void AddToVoidTraderPostQueue(WarframeVoidTrader trader, bool notifyClient, bool traderHasExpired)
        {
            if (!_voidTraderMessagePostQueue.Any(x => x.WarframeEvent.GUID == trader.GUID))
            {
                _voidTraderMessagePostQueue.Add(new MessageQueueElement<WarframeVoidTrader>(trader, notifyClient, traderHasExpired));
            }
        }

        private void AddToVoidFissurePostQueue(WarframeVoidFissure fissure, bool notifyClient, bool fissureHasExpired)
        {
            if (!_voidFissureMessagePostQueue.Any(x => x.WarframeEvent.GUID == fissure.GUID))
            {
                _voidFissureMessagePostQueue.Add(new MessageQueueElement<WarframeVoidFissure>(fissure, notifyClient, fissureHasExpired));
            }
        }

        private void AddToSortiePostQueue(WarframeSortie sortie, bool notifyClient, bool sortieHasExpired)
        {
            if (!_sortieMessagePostQueue.Any(x => x.WarframeEvent.GUID == sortie.GUID))
            {
                _sortieMessagePostQueue.Add(new MessageQueueElement<WarframeSortie>(sortie, notifyClient, sortieHasExpired));
            }
        }

        private void AddToTimeCyclePostQueue(WarframeTimeCycleInfo cycle, bool notifyClient)
        {
            if (!_timeCycleMessagePostQueue.Any(x => x.WarframeEvent.GUID == cycle.GUID))
            {
                _timeCycleMessagePostQueue.Add(new MessageQueueElement<WarframeTimeCycleInfo>(cycle, notifyClient, false));
            }
        }

        private void AddToOstronBountyCyclePostQueue(WarframeOstronBountyCycle bounty, bool notifyClient)
        {
            if (!_ostronBountyCyclePostQueue.Any(x => x.WarframeEvent.GUID == bounty.GUID))
            {
                _ostronBountyCyclePostQueue.Add(new MessageQueueElement<WarframeOstronBountyCycle>(bounty, notifyClient, false));
            }
        }

        private void AddToBountyPostQueue(WarframeOstronBounty bounty, bool notifyClient, bool bountyHasExpired)
        {
            if (!_ostronBountyMessagePostQueue.Any(x => x.WarframeEvent.GUID == bounty.GUID))
            {
                _ostronBountyMessagePostQueue.Add(new MessageQueueElement<WarframeOstronBounty>(bounty, notifyClient, bountyHasExpired));
            }
        }

        private void AddToAcolytePostQueue(WarframeAcolyte acolyte, bool notifyClient, bool acolyteHasExpired)
        {
            if (!_acolyteMessagePostQueue.Any(x => x.WarframeEvent.GUID == acolyte.GUID))
            {
                _acolyteMessagePostQueue.Add(new MessageQueueElement<WarframeAcolyte>(acolyte, notifyClient, acolyteHasExpired));
            }
        }

        public void Shutdown()
        {
            Log("Shutting down...");
            DeleteMessage(_alertMessage);
            DeleteMessage(_fissureMessage);
            DeleteMessage(_sortieMessage);
            DeleteMessage(_traderMessage);
            DeleteMessage(_timeCycleMessage);
            DeleteMessage(_ostronBountyInformationMessage);
            DeleteMessage(_acolyteMessage);

            //Sometimes the invasions message may be split up over multiple Discord messages so each one needs to be deleted.
            foreach (var i in _invasionMessages)
            {
                DeleteMessage(i);
            }

            Logout();
        }

        /// <summary>
        /// Set the "currently playing" label in Discord
        /// </summary>
        /// <param name="isStreaming"></param>
        /// <param name="gameName">Name of the game</param>
        /// <param name="url"></param>
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

        private DiscordMessage SendMessageToChannel(string content, ulong channelId)
        {
            return SendMessage(content, Client.GetChannelAsync((ulong)channelId).Result);
        }

        private DiscordMessage EditEventMessage(string newContent, DiscordMessage targetMessage)
        {
            return EditMessage(newContent, targetMessage, targetMessage.Channel);
        }

        /// <summary>
        /// Creates a new message which is automatically deleted shortly after to force a DiscordApp notification
        /// </summary>
        /// <param name="content">Notification message content</param>
        /// <param name="channelId">The channel to notify</param>
        private void NotifyClient(string content, ulong channelId)
        {
            DiscordMessage message = SendMessageToChannel(content, channelId);
            DeleteMessage(message);
        }

        /// <summary>
        /// Contains information about the message and its contents
        /// </summary>
        public class MessageQueueElement<T> where T : WarframeEvent
        {
            public T WarframeEvent { get; set; }
            public bool NotifyClient { get; set; }
            public bool EventHasExpired { get; set; }

            public MessageQueueElement(T warframeEvent, bool notify, bool eventHasExpired)
            {
                NotifyClient = notify;
                WarframeEvent = warframeEvent;
                EventHasExpired = eventHasExpired;
            }

            public MessageQueueElement(MessageQueueElement<T> message)
            {
                NotifyClient = message.NotifyClient;
                WarframeEvent = message.WarframeEvent;
                EventHasExpired = message.EventHasExpired;
            }
        };
    }
}
