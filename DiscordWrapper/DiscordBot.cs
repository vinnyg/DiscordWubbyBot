using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DiscordSharp;
using DiscordSharp.Objects;
using Newtonsoft.Json;

namespace DiscordWrapper
{
    public abstract class DiscordBot
    {
        //Milliseconds which must pass before another Discord request can be made.
        private const int REQUEST_TIME_LIMIT = 1000;
        public const int MESSAGE_CHAR_LIMIT = 2000;

        public DiscordClient Client { get; internal set; }   //Client
        public string Name { get; set; }       //Name of bot
        public DiscordMember Owner { get; set; }
        public Config BotConfig { get; internal set; }
        public StreamWriter LogFile { get; internal set; }          //File to log messages to
        public string LogChannelName { get; internal set; }     //Name of channel to post log messages to

        private DateTime timeOfLastDiscordRequest;

        public DiscordBot(string name = "DiscordBot", string logChannelName = "")
        {
            Name = name;
            LogChannelName = logChannelName;
            timeOfLastDiscordRequest = DateTime.Now;

            if (File.Exists(Name + ".json"))
                BotConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Name + ".json"));
            else
            {
                BotConfig = new Config();
                Console.WriteLine("No config for " + Name + " was found.");
            }
        }

        public void Login()
        {
            Client = new DiscordClient(tokenOverride: BotConfig.DiscordToken, isBotAccount: true, enableLogging: true);
            Client.RequestAllUsersOnStartup = true;
            Client.EnableVerboseLogging = true;

            if (BotConfig.email == null || BotConfig.password == null)
            {
                Console.WriteLine("Please provide login credentials in \"" + Name + ".json\"");
                return;
            }

            //Client.ClientPrivateInformation.Email = BotConfig.email;
            //Client.ClientPrivateInformation.Password = BotConfig.password;
        }

        public abstract void Init();

        //Send a message to the specified channel
        virtual public DiscordMessage SendMessage(string content, DiscordChannel channel)
        {
#if DEBUG
            //Log($"SendMessage({content})");
            Console.WriteLine($"SendMessage({content})");
#endif
            DiscordMessage m = null;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                m = Client.SendMessageToChannel(content, channel);
                timeOfLastDiscordRequest = DateTime.Now;
            }
            catch (NullReferenceException)
            {
                Log("SendMessage threw a NullReferenceException.");
            }
            catch(Exception)
            {
                Log("SendMessage threw an exception.");
            }

            return m;
        }

        //Send a message to the specified user
        virtual public DiscordMessage SendMessage(string content, DiscordMember user)
        {
            DiscordMessage m = null;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                m = Client.SendMessageToUser(content, user);
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

            return m;
        }

        virtual public DiscordMessage EditMessage(string newContent, DiscordMessage targetMessage, DiscordChannel channel)
        {
            DiscordMessage m = targetMessage;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                m = Client.EditMessage(targetMessage.ID, newContent, channel);
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
            return m;
        }

        virtual public void DeleteMessage(DiscordMessage target)
        {
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                if (target != null)
                {
                    Client.DeleteMessage(target);
                    timeOfLastDiscordRequest = DateTime.Now;
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

        virtual public DiscordMessage GetMessageByID(string messageID, DiscordChannel channel)
        {
            List<DiscordMessage> messageHistory = new List<DiscordMessage>();
            DiscordMessage targetMessage = null;
            String lastDiscordMessage = String.Empty;
            int messageBatch = 0;

            do
            {
                messageHistory = Client.GetMessageHistory(channel, 40, lastDiscordMessage);
#if DEBUG
                Log($"Looping for message ({messageID}) in batch {messageBatch * 40}-{((messageBatch * 40) + 39)}");
#endif

                targetMessage = messageHistory.Find(x => x.ID == messageID);
                if (messageHistory.Count > 0)
                    lastDiscordMessage = messageHistory.Last().ID;

                ++messageBatch;
            } while ((targetMessage == null) && (messageHistory.Count > 0));

            return targetMessage;
        }

        virtual public void NotifyClient(string content, DiscordChannel channel)
        {
            DiscordMessage m = SendMessage(content, channel);
            System.Threading.Thread.Sleep(REQUEST_TIME_LIMIT);
            Client.DeleteMessage(m);
        }

        virtual public void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
            return;

            /*var r = Client.GetServersList();

            if ((Client != null) && (Client.GetServersList() != null) && (Client.GetServersList().Count > 0))
            {
                DiscordChannel chan = Client.GetChannelByName(LogChannelName);
                if (chan != null)
                {
                    if (LogChannelName.Length > 0)
                        SendMessage(message, chan);

                    if (LogFile != null)
                    {
                        //LogFile.
                    }
                }
            }*/
        }

        private int GetTimeUntilCanRequest()
        {
            DateTime timeOfNextAvailableRequest = timeOfLastDiscordRequest.AddMilliseconds(REQUEST_TIME_LIMIT);
            double timeUntilNextAvailableRequest = timeOfNextAvailableRequest.Subtract(DateTime.Now).TotalMilliseconds;
            return timeUntilNextAvailableRequest < 0 ? 0 : (int)timeUntilNextAvailableRequest;
        }
    }
}
