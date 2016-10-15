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

            StringBuilder returnMessage = new StringBuilder(
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

            StringBuilder returnMessage = new StringBuilder(
                msgInfo.Destination + Environment.NewLine +
                msgInfo.Faction + Environment.NewLine +
                msgInfo.Reward + Environment.NewLine +
                msgInfo.Status
                );

            return returnMessage.ToString();
        }

        public string BuildMessage(WarframeVoidTrader trader, bool formatMessage)
        {
            WarframeEventMessageInfo msgInfo = ParseVoidTrader(trader);
            string traderAction = (DateTime.Now < trader.StartTime) ? "arriving at" : "leaving";

            StringBuilder returnMessage = new StringBuilder(
                $"{msgInfo.Faction} is {traderAction} {msgInfo.Destination} in {msgInfo.Status}.{Environment.NewLine + Environment.NewLine + msgInfo.Reward}");

            return returnMessage.ToString();
        }

        public string BuildMessage(WarframeVoidFissure fissure, bool formatMessage)
        {
            WarframeEventMessageInfo msgInfo = ParseVoidFissure(fissure);
            string styleStr = formatMessage ? (fissure.IsExpired() ? ITALIC : BOLD) : NO_STYLE;

            StringBuilder returnMessage = new StringBuilder(
                msgInfo.Destination + Environment.NewLine +
                msgInfo.Reward + Environment.NewLine +
                msgInfo.Status
                );

            return returnMessage.ToString();
        }

        public string BuildMessage(WarframeSortie sortie, bool formatMessage)
        {
            var msgInfo = ParseSortie(sortie);
            string styleStr = formatMessage ? (sortie.IsExpired() ? ITALIC : BOLD) : NO_STYLE;

            StringBuilder returnMessage = returnMessage = new StringBuilder();

            //Stored boss name in Reward property for convenience.
            returnMessage.Append($"Defeat {sortie.VariantDetails.First().Reward}'s Forces" + Environment.NewLine);
            returnMessage.Append(msgInfo.First().Status + Environment.NewLine + Environment.NewLine);
            //Stored condition in parsed reward for convenience also.
            foreach (var variant in msgInfo)
            {
                returnMessage.Append(
                variant.Destination + Environment.NewLine +
                variant.Faction + Environment.NewLine +
                variant.Reward + Environment.NewLine + Environment.NewLine
                );
            }
            
            return returnMessage.ToString();
        }

        public string BuildNotificationMessage(WarframeAlert alert)
        {
            WarframeEventMessageInfo msgInfo = ParseAlert(alert);
            string expireMsg = $"Expires {alert.ExpireTime:HH:mm} ({alert.GetMinutesRemaining(false)}m)";

            StringBuilder msg = new StringBuilder(
                "New Alert" + Environment.NewLine +
                msgInfo.Reward + Environment.NewLine +
                expireMsg
                );

            return msg.ToString();
        }

        public string BuildNotificationMessage(WarframeInvasion invasion)
        {
            WarframeEventMessageInfo msgInfo = ParseInvasion(invasion);

            StringBuilder msg = new StringBuilder(
                "New Invasion" + Environment.NewLine +
                msgInfo.Faction + Environment.NewLine +
                msgInfo.Reward + Environment.NewLine
                );

            return msg.ToString();
        }

        public string BuildNotificationMessage(WarframeVoidTrader trader)
        {
            WarframeEventMessageInfo msgInfo = ParseVoidTrader(trader);

            StringBuilder msg = new StringBuilder(
                msgInfo.Faction + Environment.NewLine +
                msgInfo.Destination + Environment.NewLine
                );

            return msg.ToString();
        }

        public string BuildNotificationMessage(WarframeVoidFissure fissure)
        {
            WarframeEventMessageInfo msgInfo = ParseVoidFissure(fissure);
            string expireMsg = $"Expires {fissure.ExpireTime:HH:mm} ({fissure.GetMinutesRemaining(false)}m)";

            StringBuilder msg = new StringBuilder(
                "New Void Fissure" + Environment.NewLine +
                msgInfo.Reward + Environment.NewLine +
                expireMsg
                );

            return msg.ToString();
        }

        public string BuildNotificationMessage(WarframeSortie sortie)
        {
            var msgInfo = ParseSortie(sortie);
            string expireMsg = $"Expires {sortie.ExpireTime:HH:mm} ({sortie.GetMinutesRemaining(false)}m)";

            //We only care about the faction for the sortie.
            StringBuilder msg = new StringBuilder(
                "New Sortie" + Environment.NewLine +
                sortie.VariantDetails.First().Faction + Environment.NewLine +
                expireMsg
                );

            return msg.ToString();
        }

        private string ParseTimeAsUnits(int minutes)
        {
            //TimeSpan ts = (DateTime.Now < trader.StartTime) ? trader.GetTimeRemaining(true) : trader.GetTimeRemaining(false);
            //int days = ts.Days, hours = ts.Hours, minutes = ts.Minutes;

            TimeSpan ts = TimeSpan.FromMinutes(minutes);
            int days = ts.Days, hours = ts.Hours, mins = ts.Minutes;

            var result = new StringBuilder((days > 0 ? $"{days} Days " : String.Empty) 
                                        + ((hours > 0) || (days > 0) ? $"{hours}h " : "")
                                        + ($"{mins}m"));

            return result.ToString();
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

            string winningFaction = (System.Math.Abs(invasion.Progress) / invasion.Progress) > 0 ? defenderInfo.Faction : attackerInfo.Faction,
                changeRateSign = (invasion.ChangeRate < 0 ? "" : "+");
            
            WarframeEventMessageInfo msgInfo = new WarframeEventMessageInfo(
                $"{invasion.DestinationName}",
                $"{defenderInfo.Faction} vs {attackerInfo.Faction}",
                $"{(defenderInfo.Faction != Faction.INFESTATION ? ($"{attackerQuantityMessage + attackerRewardMessage} ({defenderInfo.MissionType}) / ") : "")}{defenderQuantityMessage + defenderRewardMessage} ({attackerInfo.MissionType})",
                $"{String.Format("{0:0.00}", System.Math.Abs(invasion.Progress * 100.0f))}% ({changeRateSign + String.Format("{0:0.00}", invasion.ChangeRate * 100.0f)} p/hr){(defenderInfo.Faction != Faction.INFESTATION ? " (" + winningFaction + ")": "")}"
                //$"{String.Format("{0:0.00}", System.Math.Abs(invasion.Progress * 100.0f))}% ({winningFaction})"
                );

            return msgInfo;
        }

        private WarframeEventMessageInfo ParseVoidTrader(WarframeVoidTrader trader)
        {
            TimeSpan ts = (DateTime.Now < trader.StartTime) ? trader.GetTimeRemaining(true) : trader.GetTimeRemaining(false);
            int days = ts.Days, hours = ts.Hours, minutes = ts.Minutes;
            string traderName = "Baro Ki Teer";

            StringBuilder rewardString = new StringBuilder();
            
            //Ensure that the trader's inventory is not empty first.
            if (trader.Inventory.Count() > 0)
            {
                foreach (var i in trader.Inventory)
                {
                    rewardString.Append($"{i.Name} {i.Credits}cr + {i.Ducats}dc{Environment.NewLine}");
                }
            }

            WarframeEventMessageInfo msgInfo = new WarframeEventMessageInfo(
                trader.DestinationName,
                traderName,
                rewardString.ToString(),
                $"{days} days {hours} hours and {minutes} minutes");

            return msgInfo;
        }

        private WarframeEventMessageInfo ParseVoidFissure(WarframeVoidFissure fissure)
        {
            MissionInfo info = fissure.MissionDetails;
            string rewardMessage = (!String.IsNullOrEmpty(info.Reward) ? info.Reward : String.Empty);

            string statusString =
                (!fissure.IsExpired()) ? (DateTime.Now < fissure.StartTime ? $"Starts {fissure.StartTime:HH:mm} ({fissure.GetMinutesRemaining(true)}m)" :
                $"Expires {fissure.ExpireTime:HH:mm} ({fissure.GetMinutesRemaining(false)}m)") : $"Expired ({fissure.ExpireTime:HH:mm})";

            WarframeEventMessageInfo msgInfo = new WarframeEventMessageInfo(
                $"{fissure.DestinationName}",
                $"{info.Faction}",
                $"{rewardMessage}",
                $"{statusString}"
                );

            return msgInfo;
        }

        private List<WarframeEventMessageInfo> ParseSortie(WarframeSortie sortie)
        {
            var info = sortie.VariantDetails;
            var varDest = sortie.VariantDestinations;
            var varConditions = sortie.VariantConditions;

            string rewardMessage = String.Empty;// (!String.IsNullOrEmpty(info.Reward) ? info.Reward : String.Empty);

            string statusString =
                (!sortie.IsExpired()) ? (DateTime.Now < sortie.StartTime ? $"Starts {sortie.StartTime:HH:mm} ({sortie.GetMinutesRemaining(true)}m)" :
                $"Expires {ParseTimeAsUnits(sortie.GetMinutesRemaining(false))}") : $"Expired ({sortie.ExpireTime:HH:mm})";

            var msgInfo = new List<WarframeEventMessageInfo>();

            for (var i = 0; i < sortie.VariantDetails.Count; ++i)
            {
                msgInfo.Add(new WarframeEventMessageInfo(
                $"{varDest[i]}",
                $"{info[i].Faction} {info[i].MissionType}",
                $"{varConditions[i]}",
                $"{statusString}"
                ));
            }
            
            return msgInfo;
        }
    }
}
