using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest.Events
{
    class WarframeAlertScrapedArgs : EventArgs
    {
        public WarframeAlert Alert { get; private set; }
        public string MessageID { get; private set; }

        public WarframeAlertScrapedArgs(WarframeAlert newAlert, string messageID = "")
        {
            Alert = newAlert;
            MessageID = messageID;
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

    class WarframeVoidTraderScrapedArgs : EventArgs
    {
        /*public WarframeEventMessage Message { get; set; }*/
        public WarframeVoidTrader Trader { get; private set; }

        public WarframeVoidTraderScrapedArgs(WarframeVoidTrader trader)
        {
            //Message = new WarframeEventMessage(content, unformattedContent);
            Trader = trader;
        }
    }

    class WarframeVoidFissureScrapedArgs : EventArgs
    {
        public WarframeVoidFissure Fissure { get; private set; }
        public string MessageID { get; private set; }

        public WarframeVoidFissureScrapedArgs(WarframeVoidFissure newFissure, string messageID = "")
        {
            Fissure = newFissure;
            MessageID = messageID;
        }
    }

    class WarframeSortieScrapedArgs : EventArgs
    {
        public WarframeSortie Sortie { get; private set; }
        public string MessageID { get; private set; }

        public WarframeSortieScrapedArgs(WarframeSortie newSortie, string messageID = "")
        {
            Sortie = newSortie;
            MessageID = messageID;
        }
    }

    class DayCycleTimeScrapedArgs : EventArgs
    {
        public WarframeTimeCycleInfo cycleInfo { get; private set; }
        public string MessageID { get; private set; }

        public DayCycleTimeScrapedArgs(WarframeTimeCycleInfo cycleInfo, string messageID = "")
        {
            this.cycleInfo = cycleInfo;
            MessageID = messageID;
        }
    }

    class ExistingAlertFoundArgs : EventArgs
    {
        public WarframeAlert Alert { get; private set; }
        public string MessageID { get; private set; }

        public ExistingAlertFoundArgs(WarframeAlert alert, string messageID = "")
        {
            Alert = alert;
            MessageID = messageID;
        }
    }

    class WarframeAlertExpiredArgs : EventArgs
    {
        public WarframeAlert Alert { get; private set; }
        public string MessageID { get; private set; }

        public WarframeAlertExpiredArgs(WarframeAlert alert, string messageID = "")
        {
            Alert = alert;
            MessageID = messageID;
        }
    }

    class WarframeVoidFissureExpiredArgs : EventArgs
    {
        public WarframeVoidFissure Fissure { get; private set; }
        public string MessageID { get; private set; }

        public WarframeVoidFissureExpiredArgs(WarframeVoidFissure fissure, string messageID = "")
        {
            Fissure = fissure;
            MessageID = messageID;
        }
    }

    class WarframeSortieExpiredArgs : EventArgs
    {
        public WarframeSortie Sortie { get; private set; }
        public string MessageID { get; private set; }

        public WarframeSortieExpiredArgs(WarframeSortie sortie, string messageID = "")
        {
            Sortie = sortie;
            MessageID = messageID;
        }
    }
}
