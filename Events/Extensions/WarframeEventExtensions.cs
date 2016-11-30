using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordSharpTest;
using DiscordSharpTest.WarframeEvents;

namespace WubbyBot.Events.Extensions
{
    public static class WarframeEventExtensions
    {
        private static string ParseMinutesAsTime(int minutes)
        {
            TimeSpan ts = TimeSpan.FromMinutes(minutes);
            int days = ts.Days, hours = ts.Hours, mins = ts.Minutes;

            var result = new StringBuilder((days > 0 ? $"{days} Days " : String.Empty)
                                        + ((hours > 0) || (days > 0) ? $"{hours}h " : "")
                                        + ($"{mins}m"));

            return result.ToString();
        }

        public static string DiscordMessage(this WarframeAlert alert, bool isNotification)
        {
            MissionInfo info = alert.MissionDetails;
            string rewardMessage = (!string.IsNullOrEmpty(info.Reward) ? info.Reward : string.Empty),
                rewardQuantityMessage = (info.RewardQuantity > 1 ? info.RewardQuantity + "x" : ""),
                creditMessage = (!string.IsNullOrEmpty(rewardMessage) ? ", " : "") + (info.Credits > 0 ? info.Credits + "cr" : "");

            string statusString =
                (!alert.IsExpired()) ? (DateTime.Now < alert.StartTime ? $"Starts {alert.StartTime:HH:mm} ({alert.GetMinutesRemaining(true)}m)" :
                $"Expires {alert.ExpireTime:HH:mm} ({alert.GetMinutesRemaining(false)}m)") : $"Expired ({alert.ExpireTime:HH:mm})";

            StringBuilder returnMessage = new StringBuilder();

            string expireMsg = $"Expires {alert.ExpireTime:HH:mm} ({alert.GetMinutesRemaining(false)}m)";

            if (!isNotification)
                returnMessage.Append(
                    alert.DestinationName + Environment.NewLine +
                    $"{info.Faction} {info.MissionType} ({info.MinimumLevel}-{info.MaximumLevel}){(info.RequiresArchwing ? $" (Archwing)" : String.Empty)}" + Environment.NewLine +
                    $"{rewardQuantityMessage + rewardMessage + creditMessage}" + Environment.NewLine +
                    statusString
                    );
            else
                returnMessage.Append(
                    "New Alert" + Environment.NewLine +
                    $"{rewardQuantityMessage + rewardMessage + creditMessage}" + Environment.NewLine +
                    expireMsg
                    );

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeInvasion invasion, bool isNotification)
        {
            MissionInfo attackerInfo = invasion.AttackerDetails;
            MissionInfo defenderInfo = invasion.DefenderDetails;

            //Check the invasion type - Invasions will have a reward from both factions but Outbreaks only have a reward from the defending faction.
            //Check if there is a credit reward; reward can only either be a credit reward or loot reward
            string defenderAllianceRewardMessage = invasion.Type == InvasionType.INVASION ? (attackerInfo.Credits > 0 ? invasion.AttackerDetails.Credits.ToString() + "cr" : invasion.AttackerDetails.Reward) : "",
                attackerAllianceRewardMessage = (defenderInfo.Credits > 0 ? invasion.DefenderDetails.Credits.ToString() + "cr" : invasion.DefenderDetails.Reward),
                defenderAllianceQuantityMessage = (attackerInfo.RewardQuantity > 1 ? attackerInfo.RewardQuantity + "x" : ""),
                attackerAllianceQuantityMessage = (defenderInfo.RewardQuantity > 1 ? defenderInfo.RewardQuantity + "x" : "");

            string winningFaction = (System.Math.Abs(invasion.Progress) / invasion.Progress) > 0 ? defenderInfo.Faction : attackerInfo.Faction,
                changeRateSign = (invasion.ChangeRate < 0 ? "" : "+");

            StringBuilder returnMessage = new StringBuilder();

            if (!isNotification)
                returnMessage.Append(
                    invasion.DestinationName + Environment.NewLine +
                    $"{defenderInfo.Faction} vs {attackerInfo.Faction}" + Environment.NewLine +
                    $"{(defenderInfo.Faction != Faction.INFESTATION ? ($"{defenderAllianceQuantityMessage + defenderAllianceRewardMessage} / ") : "")}{attackerAllianceQuantityMessage + attackerAllianceRewardMessage}" + Environment.NewLine +
                    //$"{(defenderInfo.Faction != Faction.INFESTATION ? ($"{attackerQuantityMessage + attackerRewardMessage} ({defenderInfo.MissionType}) / ") : "")}{defenderQuantityMessage + defenderRewardMessage} ({attackerInfo.MissionType})" + Environment.NewLine +
                    $"{string.Format("{0:0.00}", System.Math.Abs(invasion.Progress * 100.0f))}% ({changeRateSign + string.Format("{0:0.00}", invasion.ChangeRate * 100.0f)} p/hr){(defenderInfo.Faction != Faction.INFESTATION ? " (" + winningFaction + ")" : "")}"
                    );
            else
                returnMessage.Append(
                    "New Invasion" + Environment.NewLine +
                    $"{defenderInfo.Faction} vs {attackerInfo.Faction}" + Environment.NewLine +
                    $"{(defenderInfo.Faction != Faction.INFESTATION ? ($"{defenderAllianceQuantityMessage + defenderAllianceRewardMessage} / ") : "")}{attackerAllianceQuantityMessage + attackerAllianceRewardMessage}" + Environment.NewLine
                    );

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeSortie sortie, bool isNotification)
        {
            var info = sortie.VariantDetails;
            var varDest = sortie.VariantDestinations;
            var varConditions = sortie.VariantConditions;

            string statusString =
                (!sortie.IsExpired()) ? (DateTime.Now < sortie.StartTime ? $"Starts {sortie.StartTime:HH:mm} ({sortie.GetMinutesRemaining(true)}m)" :
                $"Expires {ParseMinutesAsTime(sortie.GetMinutesRemaining(false))}") : $"Expired ({sortie.ExpireTime:HH:mm})";

            StringBuilder returnMessage = returnMessage = new StringBuilder();

            if (!isNotification)
            {
                //Stored boss name in Reward property for convenience.
                returnMessage.Append($"{sortie.VariantDetails.First().Reward}" + Environment.NewLine);
                returnMessage.Append(statusString + Environment.NewLine + Environment.NewLine);
                //Stored condition in parsed reward for convenience also.
                for (var i = 0; i < sortie.VariantDetails.Count; ++i)
                {
                    returnMessage.Append(
                    varDest[i] + Environment.NewLine +
                    $"{info[i].Faction} {info[i].MissionType}" + Environment.NewLine +
                    varConditions[i] + Environment.NewLine + Environment.NewLine
                    );
                }
            }
            else
                returnMessage.Append(
                "New Sortie" + Environment.NewLine +
                sortie.VariantDetails.First().Faction + Environment.NewLine
                );

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeVoidFissure fissure, bool isNotification)
        {
            MissionInfo info = fissure.MissionDetails;
            string rewardMessage = (!string.IsNullOrEmpty(info.Reward) ? info.Reward : string.Empty);

            string statusString =
                (!fissure.IsExpired()) ? (DateTime.Now < fissure.StartTime ? $"Starts {fissure.StartTime:HH:mm} ({fissure.GetMinutesRemaining(true)}m)" :
                $"Expires {fissure.ExpireTime:HH:mm} ({fissure.GetMinutesRemaining(false)}m)") : $"Expired ({fissure.ExpireTime:HH:mm})";

            StringBuilder returnMessage = new StringBuilder();
            if (!isNotification)
                returnMessage.Append(
                    fissure.DestinationName + Environment.NewLine +
                    $"{info.Faction} {info.MissionType}{(info.RequiresArchwing ? $" (Archwing)" : String.Empty)}" + Environment.NewLine +
                    rewardMessage + Environment.NewLine +
                    statusString
                    );
            else
                returnMessage.Append(
                    "New Void Fissure" + Environment.NewLine +
                    $"{info.Faction} {info.MissionType}" + Environment.NewLine +
                    rewardMessage + Environment.NewLine +
                    statusString
                    );

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeVoidTrader trader, bool isNotification)
        {
            TimeSpan ts = (DateTime.Now < trader.StartTime) ? trader.GetTimeRemaining(true) : trader.GetTimeRemaining(false);
            int days = ts.Days, hours = ts.Hours, minutes = ts.Minutes;
            string traderName = "Baro Ki Teer";

            StringBuilder traderInventory = new StringBuilder();

            //Ensure that the trader's inventory is not empty first.
            if (trader.Inventory.Count() > 0)
            {
                foreach (var i in trader.Inventory)
                {
                    traderInventory.Append($"{i.Name} {(i.Credits > 0 ? $"{i.Credits}cr{(i.Ducats > 0 ? " + " : string.Empty)}" : string.Empty)}{(i.Ducats > 0 ? $"{i.Ducats}dc" : string.Empty)}{Environment.NewLine}");
                }
            }
            string traderAction = (DateTime.Now < trader.StartTime) ? "arriving at" : "leaving";

            StringBuilder returnMessage = new StringBuilder();

            if (!isNotification)
                returnMessage.Append(
                    $"{traderName} is {traderAction} {trader.DestinationName} in {$"{days} days {hours} hours and {minutes} minutes"}.{Environment.NewLine + Environment.NewLine + traderInventory.ToString()}");
            else
                returnMessage.Append(
                    traderName + Environment.NewLine +
                    trader.DestinationName + Environment.NewLine
                    );

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeTimeCycleInfo cycleInfo, bool isNotification)
        {
            string timeOfDay = cycleInfo.TimeIsDay() ? "Day" : "Night";
            string cycleStatus = $"{cycleInfo.TimeOfNextCycleChange:HH:mm} ({(cycleInfo.TimeUntilNextCycleChange.Hours > 0 ? $"{cycleInfo.TimeUntilNextCycleChange.Hours}h " : String.Empty)}{cycleInfo.TimeUntilNextCycleChange.Minutes}m)";

            StringBuilder returnMessage = new StringBuilder(
                $"The current cycle is {timeOfDay}." + Environment.NewLine +
                $"The next cycle begins at {cycleStatus}.{Environment.NewLine}");

            return returnMessage.ToString();
        }
    }
}
