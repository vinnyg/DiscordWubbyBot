using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DiscordSharp;
using DiscordSharp.Objects;
using Newtonsoft.Json;

namespace DiscordSharpTest
{
    abstract class DiscordBot
    {
        public DiscordClient Client { get; internal set; }   //Client
        public string Name { get; set; }       //Name of bot
        public DiscordMember Owner { get; set; }
        public Config BotConfig { get; internal set; }
        public StreamWriter LogFile { get; internal set; }          //File to log messages to
        public string LogChannelName { get; internal set; }     //Name of channel to post log messages to

        public DiscordBot(string name, string logChannelName = "")
        {
            Name = name;
            LogChannelName = logChannelName;
            BotConfig = new Config();
            if (File.Exists(Name + ".json"))
            {
                BotConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Name + ".json"));
            }
            else
            {
                BotConfig = new Config();
                Console.WriteLine("No config for " + Name + " was found.");
            }
        }

        public void Login()
        {
            Client = new DiscordClient(tokenOverride: @"MTgzOTIyMjAwNTMxNjk3NjY2.CiM6ZQ.YRhWFuuqAQ0ZOJ8iOKWGkfMDl8Q", isBotAccount: true);
            //Client = new DiscordClient();
            Client.RequestAllUsersOnStartup = true;

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
            return Client.SendMessageToChannel(content, channel);
        }

        //Send a message to the specified user
        virtual public DiscordMessage SendMessage(string message, DiscordMember user)
        {
            return Client.SendMessageToUser(message, user);
        }

        virtual public DiscordMessage EditMessage(string newContent, DiscordMessage targetMessage, DiscordChannel channel)
        {
            return Client.EditMessage(targetMessage.ID, newContent, channel);
        }

        virtual public void DeleteMessage(DiscordMessage target)
        {
            if (target != null)
                Client.DeleteMessage(target);
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

        virtual public void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");

            var r = Client.GetServersList();

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
            }
        }
    }
}
