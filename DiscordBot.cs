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
            Client = new DiscordClient(tokenOverride: @"MTQ0ODU2MDA0NzcxNzA4OTI5.Cecd8g.RaYmOns1l9G2BxmVA9KVz2Jstt0", isBotAccount: true);
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
        virtual public DiscordMessage SendMessage(string message, DiscordChannel channel)
        {
            return Client.SendMessageToChannel(message, channel);
        }

        //Send a message to the specified user
        virtual public DiscordMessage SendMessage(string message, DiscordMember user)
        {
            return Client.SendMessageToUser(message, user);
        }

        virtual public void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
            
            if ((Client != null) && (Client.GetServersList() != null))
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
