using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeEventMessageBuilder
    {
        const string BOLD = "**";
        const string ITALIC = "*";
        const string NO_STYLE = "";

        public string BuildMessage(WarframeAlert alert, bool formatMessage)
        {
            WarframeEventMessageInfo msgInfo = ParseAlert(alert);
            string styleStr = formatMessage ? (alert.IsExpired() ? ITALIC : BOLD) : NO_STYLE;

            StringBuilder returnMessage;

            /*returnMessage = new StringBuilder(
                "Destination: " + styleStr + msgInfo.Destination + styleStr + Environment.NewLine +
                "Mission: " + styleStr + msgInfo.Faction + styleStr + Environment.NewLine +
                "Reward: " + styleStr + msgInfo.Reward + styleStr + Environment.NewLine +
                "Status: " + styleStr + msgInfo.Status + styleStr
                );*/

            returnMessage = new StringBuilder(
                msgInfo.Destination + Environment.NewLine +
                msgInfo.Faction + Environment.NewLine +
                msgInfo.Reward + Environment.NewLine +
                msgInfo.Status
                );

            return returnMessage.ToString();
        }

        public string BuildMessage(WarframeInvasion invasion, bool formatMessage)
        {
            WarframeEventMessageInfo msgInfo = ParseInvasion(invasion);
            string styleStr = formatMessage ? (invasion.IsExpired() ? ITALIC : BOLD) : NO_STYLE;

            StringBuilder returnMessage;

            /*returnMessage = new StringBuilder(
                "Destination: " + styleStr + msgInfo.Destination + styleStr + Environment.NewLine +
                "Mission: " + styleStr + msgInfo.Faction + styleStr + Environment.NewLine +
                "Reward: " + styleStr + msgInfo.Reward + styleStr + Environment.NewLine +
                "Status: " + styleStr + msgInfo.Status + styleStr
                );*/

            returnMessage = new StringBuilder(
                msgInfo.Destination + Environment.NewLine +
                msgInfo.Faction + Environment.NewLine +
                msgInfo.Reward + Environment.NewLine +
                msgInfo.Status
                );

            return returnMessage.ToString();
        }

        private WarframeEventMessageInfo ParseAlert(WarframeAlert alert)
        {
            MissionInfo info = alert.MissionDetails;
            string rewardMessage = (!String.IsNullOrEmpty(info.Reward) ? info.Reward : String.Empty),
                rewardQuantityMessage = (info.RewardQuantity > 1 ? info.RewardQuantity + "x" : ""),
                creditMessage = (/*info.RewardQuantity > 0*/ !String.IsNullOrEmpty(rewardMessage) ? ", " : "") + (info.Credits > 0 ? info.Credits + "cr" : "");

            string statusString =
                (!alert.IsExpired()) ? (DateTime.Now < alert.StartTime ? $"Starts {alert.StartTime:HH:mm} ({alert.GetMinutesRemaining(true)}m)" :
                $"Expires {alert.ExpireTime:HH:mm} ({alert.GetMinutesRemaining(false)}m)") : $"Expired ({alert.ExpireTime:HH:mm})";
            
            WarframeEventMessageInfo msgInfo = new WarframeEventMessageInfo(
                $"{alert.DestinationName}",
                $"{info.Faction} {info.MissionType} ({info.MinimumLevel}-{info.MaximumLevel})",
                $"{rewardQuantityMessage + rewardMessage + creditMessage}",
                $"{statusString}"
                );

            return msgInfo;
        }

        private WarframeEventMessageInfo ParseInvasion(WarframeInvasion invasion)
        {
            MissionInfo attackerInfo = invasion.AttackerDetails;
            MissionInfo defenderInfo = invasion.DefenderDetails;

            //Check the invasion type - Invasions will have a reward from both factions but Outbreaks only have a reward from the defending faction.
            //Check if there is a credit reward; reward can only either be a credit reward or loot reward
            string attackerRewardMessage = invasion.Type == InvasionType.INVASION ? (attackerInfo.Credits > 0 ? invasion.AttackerDetails.Credits.ToString() + "cr" : invasion.AttackerDetails.Reward) : "",
                defenderRewardMessage = (defenderInfo.Credits > 0 ? invasion.DefenderDetails.Credits.ToString() + "cr" : invasion.DefenderDetails.Reward),
                attackerQuantityMessage = (attackerInfo.RewardQuantity > 1 ? attackerInfo.RewardQuantity + "x" : ""),
                defenderQuantityMessage = (defenderInfo.RewardQuantity > 1 ? defenderInfo.RewardQuantity + "x" : "");

            string winningFaction = (System.Math.Abs(invasion.GetProgress()) / invasion.GetProgress()) > 0 ? defenderInfo.Faction : attackerInfo.Faction;

            WarframeEventMessageInfo msgInfo = new WarframeEventMessageInfo(
                $"{invasion.DestinationName}",
                $"{defenderInfo.Faction} vs {attackerInfo.Faction}",
                $"{(defenderInfo.Faction != Faction.INFESTATION ? ($"{attackerQuantityMessage + attackerRewardMessage} ({attackerInfo.Faction + " " + defenderInfo.MissionType}) / ") : "")}{defenderQuantityMessage + defenderRewardMessage} ({defenderInfo.Faction + " " + attackerInfo.MissionType})",
                $"{String.Format("{0:0.00}", System.Math.Abs(invasion.GetProgress() * 100.0f))}% ({winningFaction})"
                );

            return msgInfo;
        }
    }
}
