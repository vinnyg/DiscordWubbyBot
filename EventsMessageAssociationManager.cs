using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordSharp.Objects;

namespace DiscordSharpTest
{
    static class EventsMessageAssociationManager
    {
        public static Dictionary<WarframeAlert, DiscordMessage> AlertsMessagesMap { get; private set; }
        public static Dictionary<WarframeInvasion, DiscordMessage> InvasionsMessagesMap { get; private set; }

        static EventsMessageAssociationManager()
        {
            AlertsMessagesMap = new Dictionary<WarframeAlert, DiscordMessage>();
            InvasionsMessagesMap = new Dictionary<WarframeInvasion, DiscordMessage>();
        }

        static void ProcessEvent(WarframeEvent targetEvent)
        {
            
        }
    };
}
