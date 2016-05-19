using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeEventsMessageHelper
    {
        private Dictionary<string, StringBuilder> AlertMessages { get; set; }
        private Dictionary<string, StringBuilder> InvasionMessages { get; set; }

        string ParseEvent(WarframeAlert alert)
        {
            throw new NotImplementedException();

            MissionInfo info = alert.MissionDetails;
            string rewardMessage = (!String.IsNullOrEmpty(info.Reward) ? info.Reward : String.Empty),
                rewardQuantityMessage = (info.RewardQuantity > 1 ? info.RewardQuantity + " x " : ""),
                creditMessage = (info.RewardQuantity > 0 ? ", " : "") + (info.Credits > 0 ? info.Credits + "cr" : "");

            StringBuilder result = new StringBuilder();

            /*StringBuilder result = new StringBuilder(
                $"Destination: {alert.DestinationName} ({info.MinimumLevel}-{info.MaximumLevel})" + Environment.NewLine + 
                 $"Mission: {info.MissionType} ({info.Faction})" + Environment.NewLine +
                //If there is no reward then skip straight to credits. If the quantity of the reward is less than two, omit quantifier.
                //If there is no credit reward then return an empty "substring". 
                $"Reward: {rewardMessage + rewardQuantityMessage + creditMessage}" + Environment.NewLine +
                //If alert has not started, display minutes to start. If alert has started, display minutes to expiration.
                $"Status: {(DateTime.Now < alert.StartTime ? $"Starts {alert.StartTime} ({alert.StartTime.Subtract(DateTime.Now)}m)" : $"Expires {alert.ExpireTime} ({alert.ExpireTime.Subtract(DateTime.Now)}m)")}");*/

            AlertMessages.Add(alert.GUID, result);

            return result.ToString();
        }

        string ParseEvent(WarframeInvasion invasion)
        {
            MissionInfo attackerInfo = invasion.AttackerDetails;
            MissionInfo defenderInfo = invasion.DefenderDetails;

            //Check the invasion type - Invasions will have a reward but Outbreaks do not.
            //Check if there is a credit reward; reward can only either be a credit reward or loot reward
            string attackerRewardMessage = invasion.Type == InvasionType.INVASION ? (attackerInfo.Credits > 0 ? invasion.AttackerDetails.Credits.ToString() : invasion.AttackerDetails.Reward) + " // " : "",
                defenderRewardMessage = invasion.Type == InvasionType.INVASION ? (defenderInfo.Credits > 0 ? invasion.DefenderDetails.Credits.ToString() : invasion.DefenderDetails.Reward) : "",
                attackerQuantityMessage = (attackerInfo.RewardQuantity > 1 ? attackerInfo.RewardQuantity + " x " : ""),
                defenderQuantityMessage = (defenderInfo.RewardQuantity > 1 ? defenderInfo.RewardQuantity + " x " : "");

            StringBuilder result = new StringBuilder(
                $"Destination: {invasion.DestinationName}" + Environment.NewLine +
                 $"Mission: {attackerInfo.Faction} ({attackerInfo.MissionType}) vs {defenderInfo.Faction} ({defenderInfo.MissionType})" + Environment.NewLine +
                //If the quantity of the reward is less than two, omit quantifier.
                //Reward is either loot or credits. It can never be both. 
                $"Reward: {attackerQuantityMessage + attackerRewardMessage + defenderQuantityMessage + defenderRewardMessage}" + Environment.NewLine +
                //
                $"Progress: {0}% ({0})");

            return result.ToString();
        }
    }
}
