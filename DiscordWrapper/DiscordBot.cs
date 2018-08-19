using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace DiscordWrapper
{
    public abstract class DiscordBot
    {
        //Milliseconds which must pass before another Discord request can be made
        private const int REQUEST_TIME_LIMIT = 1100;
        //Maximum character limit for a single Discord Message
        public const int MESSAGE_CHAR_LIMIT = 2000;
        //Milliseconds which must pass before a delete request can be made.
        private const int DELETE_REQUEST_TIME_LIMIT = 250;

        public DiscordClient Client { get; set; }
        /// <summary>
        /// Display name of bot
        /// </summary>
        public string Name { get; set; }
        public DiscordMember Owner { get; set; }
        public DiscordConfiguration DiscordConfig { get; internal set; }
        public Config BotConfig { get; internal set; }
        public StreamWriter LogFile { get; internal set; }
        /// <summary>
        /// Name of channel to post log messages to
        /// </summary>
        public string LogChannelName { get; internal set; }

        private DateTime timeOfLastDiscordRequest;
        private List<DiscordChannel> _channelList { get; set; }

        public DiscordBot(string name = "DiscordBot", string logChannelName = "")
        {
            Name = name;
            LogChannelName = logChannelName;
            timeOfLastDiscordRequest = DateTime.Now;

            string fileName = "discordbot.json";//ConfigurationManager.AppSettings["DiscordBotSettings"].ToString();
            if (File.Exists(fileName))
            {
                BotConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(fileName));
            }

            //Instantiate config
            DiscordConfig = new DiscordConfiguration();
            DiscordConfig.AutoReconnect = true;
            DiscordConfig.Token = BotConfig.DiscordToken;
            DiscordConfig.LogLevel = LogLevel.Warning;
        }

        public void Login()
        {
            Client = new DiscordClient(DiscordConfig);
        }

        /// <summary>
        /// Send a message to the specified channel
        /// </summary>
        /// <param name="content">Content of the message to be sent</param>
        /// <param name="channel">Detination channel to send the message to</param>
        /// <returns></returns>
        virtual public DiscordMessage SendMessage(string content, DiscordChannel channel)
        {
#if DEBUG
            Console.WriteLine($"SendMessage({content})");
#endif
            DiscordMessage message = null;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                message = Client.SendMessageAsync(channel, content, false).Result;

                timeOfLastDiscordRequest = DateTime.Now;
            }
            catch (NullReferenceException)
            {
                Log("SendMessage threw a NullReferenceException.");
            }
            catch (Exception)
            {
                Log("SendMessage threw an exception.");
            }

            return message;
        }

        virtual public void Connect()
        {
            Client.ConnectAsync().Wait();
        }

        /// <summary>
        /// Send a message to the specified user
        /// </summary>
        /// <param name="content">Content of the message to be sent</param>
        /// <param name="user">Target user to send the message at</param>
        /// <returns></returns>
        virtual public DiscordMessage SendMessage(string content, DiscordMember user)
        {
            DiscordMessage message = null;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                var dmChannel = Client.CreateDmAsync(user).Result;
                message = dmChannel.SendMessageAsync(content).Result;
                timeOfLastDiscordRequest = DateTime.Now;
            }
            catch (NullReferenceException)
            {
                Log("SendMessage threw a NullReferenceException.");
            }
            catch (Exception e)
            {
                Log($"SendMessage threw an exception. {e.Message}, {e.StackTrace}");
            }

            return message;
        }

        virtual public DiscordMessage EditMessage(string newContent, DiscordMessage targetMessage, DiscordChannel channel)
        {
            var message = targetMessage;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());

                message.ModifyAsync(newContent).Wait();

                timeOfLastDiscordRequest = DateTime.Now;
            }
            catch (NullReferenceException)
            {
                Log("EditMessage threw a NullReferenceException.");
            }
            catch (Exception)
            {
                Log("EditMessage threw an exception.");
            }
            return message;
        }

        virtual public void DeleteMessage(DiscordMessage targetMessage)
        {
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest(DELETE_REQUEST_TIME_LIMIT));
                if (targetMessage != null)
                {
                    targetMessage.DeleteAsync().Wait();
                    
                    timeOfLastDiscordRequest = DateTime.Now;
                    Log($"Message {targetMessage.Id} from {targetMessage.Channel.Name} was deleted.");
                }
            }
            catch (NullReferenceException)
            {
                Log("DeleteMessage threw a NullReferenceException.");
            }
            catch (Exception)
            {
                Log("DeleteMessage threw an exception.");
            }
        }


        virtual public DiscordMessage GetMessageById(DiscordChannel channel, ulong id)
        {
            return channel.GetMessageAsync(id).Result;
        }

        virtual public async Task<IReadOnlyList<DiscordMessage>> GetChannelMessagesAsync(DiscordChannel channel, int limit = 100)
        {
            return await channel.GetMessagesAsync(limit);
        }

        virtual public async Task<IReadOnlyList<DiscordMessage>> GetChannelMessagesBeforeAsync(DiscordChannel channel, ulong before, int limit = 100)
        {
             return await channel.GetMessagesAsync(limit, before: before);
        }

        virtual public async Task<IReadOnlyList<DiscordMessage>> GetChannelMessagesAfterAsync(DiscordChannel channel, ulong after, int limit = 100)
        {
            return await channel.GetMessagesAsync(limit, after: after);
        }

        virtual public async Task<IReadOnlyList<DiscordMessage>> GetChannelMessagesAroundAsync(DiscordChannel channel, ulong around, int limit = 100)
        {
            return await channel.GetMessagesAsync(limit, around: around);
        }

        virtual public void SetCurrentGame(string gameName, bool isStreaming, string url = "")
        {
            Client.UpdateStatusAsync(new DiscordGame(gameName) { Url = url }, UserStatus.Online).Wait();
        }

        virtual public void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
            return;
        }

        virtual public DiscordChannel GetChannelByID(ulong id)
        {
            return Client.GetChannelAsync(id).Result;
        }

        virtual public DiscordGuild GetGuildByID(ulong id)
        {
            return Client.GetGuildAsync(id).Result;
        }

        virtual public DiscordApplication GetCurrentApplication()
        {
            return Client.GetCurrentApplicationAsync().Result;
        }

        virtual public DiscordInvite GetInviteByCode(string code)
        {
            return Client.GetInviteByCodeAsync(code).Result;
        }

        private int GetTimeUntilCanRequest(int limit = REQUEST_TIME_LIMIT)
        {
            DateTime timeOfNextAvailableRequest = timeOfLastDiscordRequest.AddMilliseconds(limit);
            double timeUntilNextAvailableRequest = timeOfNextAvailableRequest.Subtract(DateTime.Now).TotalMilliseconds;
            return timeUntilNextAvailableRequest < 0 ? 0 : (int)timeUntilNextAvailableRequest;
        }

        virtual public void Logout()
        {
            Client.DisconnectAsync().Wait();
        }
    }
}
