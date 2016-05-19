using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    struct WarframeEventMessageInfo
    {
        //This is the initial message; it will be shown in Windows notifications.
        //private StringBuilder Message;
        //This is the formatted message. Message will be immediately replaced with this string to reflect formatting in Discord app.
        //private StringBuilder FormattedMessage;

        public string Destination { get; private set; }
        public string Faction { get; private set; }
        public string Reward { get; private set; }
        public string Status { get; private set; }

        public WarframeEventMessageInfo(string destination, string faction, string reward, string status)
        {
            Destination = destination;
            Faction = faction;
            Reward = reward;
            Status = status;
        }

        void UpdateStatusString(string status)
        {
            Status = status;
        }

        /*public string GetMessage()
        {
          return FormattedMessage.ToString();
        }

        public string GetUnformattedMessage()
        {
            return Message.ToString();
        }*/
    }
}
