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

        public string BuildMessage(WarframeAlert alert, bool isFormatted)
        {
            WarframeEventMessageInfo msgInfo = ParseAlert(alert);
            string styleStr = isFormatted ? (alert.HasExpired() ? ITALIC : BOLD) : NO_STYLE;

            StringBuilder returnMessage;
            
            returnMessage = new StringBuilder(
                "Destination: " + styleStr + msgInfo.Destination + styleStr + Environment.NewLine +
                "Mission: " + styleStr + msgInfo.Faction + styleStr + Environment.NewLine +
                "Reward: " + styleStr + msgInfo.Reward + styleStr + Environment.NewLine +
                "Status: " + styleStr + msgInfo.Status + styleStr
                );

            return returnMessage.ToString();
        }

        public string BuildMessage(WarframeInvasion invasion)
        {
            throw new NotImplementedException();

            return "";
        }

        private WarframeEventMessageInfo ParseAlert(WarframeAlert alert)
        {
            MissionInfo info = alert.MissionDetails;
            string rewardMessage = (!String.IsNullOrEmpty(info.Reward) ? info.Reward : String.Empty),
                rewardQuantityMessage = (info.RewardQuantity > 1 ? info.RewardQuantity + " x " : ""),
                creditMessage = (/*info.RewardQuantity > 0*/ !String.IsNullOrEmpty(rewardQuantityMessage) ? ", " : "") + (info.Credits > 0 ? info.Credits + "cr" : "");

            string statusString =
                (!alert.HasExpired()) ? (DateTime.Now < alert.StartTime ? $"Starts {alert.StartTime: HH:mm} ({alert.GetMinutesRemaining(true)}m)" :
                $"Expires {alert.ExpireTime : HH:mm} ({alert.GetMinutesRemaining(false)}m)") : $"Expired ({alert.ExpireTime: HH:mm})";
            
            WarframeEventMessageInfo msgInfo = new WarframeEventMessageInfo(
                $"{alert.DestinationName} ({info.MinimumLevel}-{info.MaximumLevel})",
                $"{info.MissionType} ({info.Faction})",
                $"{rewardQuantityMessage + rewardMessage + creditMessage}",
                $"{statusString}"
                );

            return msgInfo;
        }

        private WarframeEventMessageInfo ParseInvasion(WarframeInvasion invasion)
        {
            MissionInfo attackerInfo = invasion.AttackerDetails;
            MissionInfo defenderInfo = invasion.DefenderDetails;

            //Check the invasion type - Invasions will have a reward but Outbreaks do not.
            //Check if there is a credit reward; reward can only either be a credit reward or loot reward
            string attackerRewardMessage = invasion.Type == InvasionType.INVASION ? (attackerInfo.Credits > 0 ? invasion.AttackerDetails.Credits.ToString() : invasion.AttackerDetails.Reward) + " // " : "",
                defenderRewardMessage = invasion.Type == InvasionType.INVASION ? (defenderInfo.Credits > 0 ? invasion.DefenderDetails.Credits.ToString() : invasion.DefenderDetails.Reward) : "",
                attackerQuantityMessage = (attackerInfo.RewardQuantity > 1 ? attackerInfo.RewardQuantity + " x " : ""),
                defenderQuantityMessage = (defenderInfo.RewardQuantity > 1 ? defenderInfo.RewardQuantity + " x " : "");

            WarframeEventMessageInfo msgInfo = new WarframeEventMessageInfo(
                $"{invasion.DestinationName}",
                $"{attackerInfo.Faction} ({attackerInfo.MissionType}) vs {defenderInfo.Faction} ({defenderInfo.MissionType})",
                $"{attackerQuantityMessage + attackerRewardMessage + defenderQuantityMessage + defenderRewardMessage}",
                $"{0}% ({0})"
                );

            return msgInfo;
        }
    }
}
