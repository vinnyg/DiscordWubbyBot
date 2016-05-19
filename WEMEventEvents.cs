using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest.Events
{
    class WarframeAlertScrapedArgs : EventArgs
    {
        /*public WarframeEventMessage Message { get { return this.Message; } private set { this.Message = value; } }*/
        public WarframeAlert Alert { get; private set; }

        public WarframeAlertScrapedArgs(WarframeAlert newAlert)
        {
            Alert = newAlert;
        }
    }

    class WarframeInvasionScrapedArgs : EventArgs
    {
        /*public WarframeEventMessage Message { get; set; }*/
        public WarframeInvasion Invasion { get; private set; }

        public WarframeInvasionScrapedArgs(WarframeInvasion invasion)
        {
            //Message = new WarframeEventMessage(content, unformattedContent);
            Invasion = invasion;
        }
    }

    class WarframeAlertUpdatedEventArgs : EventArgs
    {
        public WarframeAlert Alert { get; private set; }

        public WarframeAlertUpdatedEventArgs(WarframeAlert alert)
        {
            Alert = alert;
        }
    }
}
