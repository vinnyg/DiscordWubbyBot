﻿using System;
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
        //Milliseconds which must pass before another Discord request can be made
        private const int REQUEST_TIME_LIMIT = 1000;
        //Maximum character limit for a single Discord Message
        public const int MESSAGE_CHAR_LIMIT = 2000;

        public DiscordClient Client { get; internal set; }
        public string Name { get; set; }       //Display name of bot
        public DiscordMember Owner { get; set; }
        public Config BotConfig { get; internal set; }
        public StreamWriter LogFile { get; internal set; }
        public string LogChannelName { get; internal set; }     //Name of channel to post log messages to

        private DateTime timeOfLastDiscordRequest;

        public DiscordBot(string name = "DiscordBot", string logChannelName = "")
        {
            Name = name;
            LogChannelName = logChannelName;
            timeOfLastDiscordRequest = DateTime.Now;

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
            Client = new DiscordClient(tokenOverride: BotConfig.DiscordToken, isBotAccount: true, enableLogging: true);
            Client.RequestAllUsersOnStartup = true;
            Client.EnableVerboseLogging = true;

            if (BotConfig.email == null || BotConfig.password == null)
            {
                Console.WriteLine("Please provide login credentials in \"" + Name + ".json\"");
                return;
            }
        }

        public abstract void Init();

        //Send a message to the specified channel
        virtual public DiscordMessage SendMessage(string content, DiscordChannel channel)
        {
#if DEBUG
            //Log($"SendMessage({content})");
            Console.WriteLine($"SendMessage({content})");
#endif
            DiscordMessage message = null;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                message = Client.SendMessageToChannel(content, channel);
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

            return message;
        }

        //Send a message to the specified user
        virtual public DiscordMessage SendMessage(string content, DiscordMember user)
        {
            DiscordMessage message = null;
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                message = Client.SendMessageToUser(content, user);
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
                message = Client.EditMessage(targetMessage.ID, newContent, channel);
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

        virtual public void DeleteMessage(DiscordMessage targetMsg)
        {
            try
            {
                System.Threading.Thread.Sleep(GetTimeUntilCanRequest());
                if (targetMsg != null)
                {
                    Client.DeleteMessage(targetMsg);
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

        //Return a reference to the specific instance of a DiscordMessage using the message ID
        virtual public DiscordMessage GetMessageByID(string messageID, DiscordChannel channel)
        {
            var messageHistory = new List<DiscordMessage>();
            DiscordMessage targetMessage = null;
            var lastDiscordMessage = string.Empty;
            var messageBatch = 0;

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

        //Creates a new message which is automatically deleted shortly after to force a DiscordApp notification
        virtual public void NotifyClient(string content, DiscordChannel channel)
        {
            DiscordMessage message = SendMessage(content, channel);
            System.Threading.Thread.Sleep(REQUEST_TIME_LIMIT);
            Client.DeleteMessage(message);
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
