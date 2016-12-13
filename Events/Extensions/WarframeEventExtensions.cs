using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordSharpTest;
using DiscordSharpTest.Events.Extensions;
using WarframeWorldStateAPI.WarframeEvents;
using WarframeWorldStateAPI.WarframeEvents.Properties;

namespace WubbyBot.Events.Extensions
{
    //Implemented as extension methods as they are not core responsibilities of the classes in question
    public static class WarframeEventExtensions
    {
        private static string ParseMinutesAsTime(int minutes)
        {
            var ts = TimeSpan.FromMinutes(minutes);
            int days = ts.Days;
            int hours = ts.Hours;
            int mins = ts.Minutes;

            var result = new StringBuilder();
            result.Append(days > 0 ? $"{days} Days " : string.Empty);
            result.Append((hours > 0) || (days > 0) ? $"{hours}h " : string.Empty);
            result.Append($"{mins}m");

            return result.ToString();
        }

        //Parse the mission information into a readable presentation
        public static string DiscordMessage(this WarframeAlert alert, bool isNotification)
        {
            MissionInfo info = alert.MissionDetails;
            string rewardMessage = (!string.IsNullOrEmpty(info.Reward) ? info.Reward : string.Empty);
            string rewardQuantityMessage = (info.RewardQuantity > 1 ? info.RewardQuantity + "x" : string.Empty);
            string creditMessage = (!string.IsNullOrEmpty(rewardMessage) ? ", " : "") + (info.Credits > 0 ? info.Credits + "cr" : string.Empty);

            var statusMessage = new StringBuilder();

            if (!alert.IsExpired())
            {
                if (DateTime.Now < alert.StartTime)
                    statusMessage.Append($"Starts {alert.StartTime:HH:mm} ({alert.GetMinutesRemaining(true)}m)");
                else
                    statusMessage.Append($"Expires {alert.ExpireTime:HH:mm} ({alert.GetMinutesRemaining(false)}m)");
            }
            else
            {
                statusMessage.Append($"Expired ({alert.ExpireTime:HH:mm})");
            }

            var returnMessage = new StringBuilder();
            var expireMessage = $"Expires {alert.ExpireTime:HH:mm} ({alert.GetMinutesRemaining(false)}m)";

            if (!isNotification)
            {
                returnMessage.AppendLine(alert.DestinationName);
                returnMessage.AppendLine($"{info.Faction} {info.MissionType} ({info.MinimumLevel}-{info.MaximumLevel}){(info.RequiresArchwing ? $" (Archwing)" : string.Empty)}");
                returnMessage.AppendLine($"{rewardQuantityMessage + rewardMessage + creditMessage}");
                returnMessage.Append(statusMessage.ToString());
            }
            else
            {
                returnMessage.AppendLine("New Alert");
                returnMessage.AppendLine($"{rewardQuantityMessage + rewardMessage + creditMessage}");
                returnMessage.Append(expireMessage);
            }

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeInvasion invasion, bool isNotification)
        {
            MissionInfo attackerInfo = invasion.AttackerDetails;
            MissionInfo defenderInfo = invasion.DefenderDetails;

            //Check the invasion type - Invasions will have a reward from both factions but Outbreaks only have a reward from the defending faction.
            //Check if there is a credit reward; reward can only either be a credit reward or loot reward
            var defenderAllianceRewardMessage = new StringBuilder();
            if (invasion.Type == InvasionType.INVASION)
            {
                if (attackerInfo.Credits > 0)
                {
                    defenderAllianceRewardMessage.Append($"{invasion.AttackerDetails.Credits.ToString()}cr");
                }
                else
                {
                    defenderAllianceRewardMessage.Append(attackerInfo.RewardQuantity > 1 ? attackerInfo.RewardQuantity + "x" : string.Empty);
                    defenderAllianceRewardMessage.Append(invasion.AttackerDetails.Reward);
                }
            }
            
            var attackerAllianceRewardMessage = new StringBuilder();
            if (defenderInfo.Credits > 0)
            {
                attackerAllianceRewardMessage.Append($"{invasion.DefenderDetails.Credits.ToString()}cr");
            }
            else
            {
                attackerAllianceRewardMessage.Append(defenderInfo.RewardQuantity > 1 ? defenderInfo.RewardQuantity + "x" : string.Empty);
                attackerAllianceRewardMessage.Append(invasion.DefenderDetails.Reward);
            }

            var winningFaction = (System.Math.Abs(invasion.Progress) / invasion.Progress) > 0 ? defenderInfo.Faction : attackerInfo.Faction;
            string changeRateSign = (invasion.ChangeRate < 0 ? "" : "+");

            var returnMessage = new StringBuilder();

            if (!isNotification)
            {
                returnMessage.AppendLine(invasion.DestinationName);
                returnMessage.AppendLine($"{defenderInfo.Faction} vs {attackerInfo.Faction}");
                returnMessage.AppendLine($"{(defenderInfo.Faction != Faction.INFESTATION ? ($"{defenderAllianceRewardMessage} / ") : "")}{attackerAllianceRewardMessage}");
                returnMessage.Append($"{string.Format("{0:0.00}", System.Math.Abs(invasion.Progress * 100.0f))}% ({changeRateSign + string.Format("{0:0.00}", invasion.ChangeRate * 100.0f)} p/hr){(defenderInfo.Faction != Faction.INFESTATION ? " (" + winningFaction + ")" : string.Empty)}");
            } 
            else
            {
                returnMessage.AppendLine("New Invasion");
                returnMessage.AppendLine($"{defenderInfo.Faction} vs {attackerInfo.Faction}");
                returnMessage.Append($"{(defenderInfo.Faction != Faction.INFESTATION ? ($"{defenderAllianceRewardMessage} / ") : "")}{attackerAllianceRewardMessage}");
            }

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeInvasionConstruction project, bool isNotification)
        {
            var factionName = project.Faction;
            var projectName = project.ProjectName;
            var progress = project.ProjectProgress;

            var returnMessage = new StringBuilder();

            if (progress > 0.0)
            {
                returnMessage.AppendLine($"{factionName} {projectName}: {string.Format("{0:0.00}", progress)}% Complete");
            }

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeSortie sortie, bool isNotification)
        {
            var info = sortie.VariantDetails;
            var varDest = sortie.VariantDestinations;
            var varConditions = sortie.VariantConditions;

            var statusMessage = new StringBuilder();

            if (!sortie.IsExpired())
            {
                if (DateTime.Now < sortie.StartTime)
                    statusMessage.Append($"Starts {sortie.StartTime:HH:mm} ({ParseMinutesAsTime(sortie.GetMinutesRemaining(true))})");
                else
                    statusMessage.Append($"Expires {sortie.ExpireTime:HH:mm} ({ParseMinutesAsTime(sortie.GetMinutesRemaining(false))})");
            }
            else
            {
                statusMessage.Append($"Expired ({sortie.ExpireTime:HH:mm})");
            }

            var returnMessage = new StringBuilder();

            if (!isNotification)
            {
                //Stored boss name in Reward property for convenience.
                returnMessage.AppendLine($"{sortie.VariantDetails.First().Reward}");
                returnMessage.AppendLine(statusMessage + Environment.NewLine);
                //Stored condition in parsed reward for convenience also.
                for (var i = 0; i < sortie.VariantDetails.Count; ++i)
                {
                    returnMessage.AppendLine(varDest[i]);
                    returnMessage.AppendLine($"{info[i].Faction} {info[i].MissionType}");
                    returnMessage.AppendLine(varConditions[i] + Environment.NewLine);
                }
            }
            else
            {
                returnMessage.AppendLine("New Sortie");
                returnMessage.AppendLine(sortie.VariantDetails.First().Faction);
            }
            
            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeVoidFissure fissure, bool isNotification)
        {
            MissionInfo info = fissure.MissionDetails;
            var rewardMessage = (!string.IsNullOrEmpty(info.Reward) ? info.Reward : string.Empty);

            var statusString = (!fissure.IsExpired()) ? (DateTime.Now < fissure.StartTime ? $"Starts {fissure.StartTime:HH:mm} ({fissure.GetMinutesRemaining(true)}m)" :
                $"Expires {fissure.ExpireTime:HH:mm} ({fissure.GetMinutesRemaining(false)}m)") : $"Expired ({fissure.ExpireTime:HH:mm})";

            StringBuilder returnMessage = new StringBuilder();
            if (!isNotification)
            {
                returnMessage.AppendLine(fissure.DestinationName);
                returnMessage.AppendLine($"{info.Faction} {info.MissionType}{(info.RequiresArchwing ? $" (Archwing)" : string.Empty)}");
                returnMessage.AppendLine(rewardMessage);
                returnMessage.Append(statusString);
            }
            else
            {
                returnMessage.AppendLine("New Void Fissure");
                returnMessage.AppendLine($"{info.Faction} {info.MissionType}");
                returnMessage.AppendLine(rewardMessage);
                returnMessage.Append(statusString);
            }

            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeVoidTrader trader, bool isNotification)
        {
            var ts = (DateTime.Now < trader.StartTime) ? trader.GetTimeRemaining(true) : trader.GetTimeRemaining(false);
            var days = ts.Days;
            var hours = ts.Hours;
            var minutes = ts.Minutes;
            var traderName = "Baro Ki Teer";
            var traderInventory = new StringBuilder();

            //Ensure that the trader's inventory is not empty first.
            if (trader.Inventory.Count() > 0)
            {
                foreach (var i in trader.Inventory)
                {
                    traderInventory.Append(i.Name);
                    if (i.Credits > 0)
                        traderInventory.Append($" {i.Credits}cr{(i.Ducats > 0 ? " +" : string.Empty)}");
                    if (i.Ducats > 0)
                        traderInventory.Append($" {i.Ducats}dc");
                    traderInventory.AppendLine();

                }
            }

            var traderAction = (DateTime.Now < trader.StartTime) ? "arriving at" : "leaving";

            var returnMessage = new StringBuilder();

            if (!isNotification)
            {
                returnMessage.AppendLine($"{traderName} is {traderAction} {trader.DestinationName} in {$"{days} days {hours} hours and {minutes} minutes"}.{Environment.NewLine}");
                returnMessage.Append(traderInventory.ToString());
            }
            else
            {
                returnMessage.AppendLine(traderName);
                returnMessage.AppendLine(trader.DestinationName);
            }
                
            return returnMessage.ToString();
        }

        public static string DiscordMessage(this WarframeTimeCycleInfo cycleInfo, bool isNotification)
        {
            var timeOfDay = cycleInfo.TimeIsDay() ? "Day" : "Night";
            var cycleStatus = $"{cycleInfo.TimeOfNextCycleChange:HH:mm} ({(cycleInfo.TimeUntilNextCycleChange.Hours > 0 ? $"{cycleInfo.TimeUntilNextCycleChange.Hours}h " : string.Empty)}{cycleInfo.TimeUntilNextCycleChange.Minutes}m)";

            var returnMessage = new StringBuilder();
            returnMessage.AppendLine($"The current cycle is {timeOfDay}.");
            returnMessage.AppendLine($"The next cycle begins at {cycleStatus}.");

            return returnMessage.ToString();
        }

        //Encapsulates Discord Message formatting to aid with code reuse and maintainability
        public static StringBuilder FormatMessage(string content, string markdownLanguageIdentifier = "xl", MessageFormat formatType = MessageFormat.CodeBlocks)
        {
            var formatString = string.Empty;

            switch (formatType)
            {
                case MessageFormat.CodeBlocks:
                    formatString = "```";
                    break;
                case MessageFormat.Bold:
                    formatString = "**";
                    break;
                case MessageFormat.Italics:
                    formatString = "*";
                    break;
                case MessageFormat.BoldItalics:
                    formatString = "***";
                    break;
                case MessageFormat.Underline:
                    formatString = "__";
                    break;
                case MessageFormat.Strikeout:
                    formatString = "~~";
                    break;
                case MessageFormat.UnderlineItalics:
                    formatString = "__*";
                    break;
                case MessageFormat.UnderlineBold:
                    formatString = "__**";
                    break;
                case MessageFormat.UnderlineBoldItalics:
                    formatString = "__***";
                    break;
                case MessageFormat.None:
                    formatString = string.Empty;
                    break;
            }

            //Only CodeBlocks and CodeLine format modes support a markdown language identifier
            if ((formatType != MessageFormat.CodeBlocks) && (formatType != MessageFormat.CodeLine))
                markdownLanguageIdentifier = string.Empty;

            var result = new StringBuilder();
            result.AppendLine($"{formatString}{(string.IsNullOrEmpty(markdownLanguageIdentifier) ? string.Empty : markdownLanguageIdentifier)}");
            result.AppendLine(content);
            result.AppendLine($"{formatString.Reverse()}");

            return result;
        }
    }

    //Contains valid formatting types for Discord messages
    public enum MessageFormat
    {
        CodeBlocks = 0,
        Bold,
        Italics,
        BoldItalics,
        CodeLine,
        Underline,
        Strikeout,
        UnderlineItalics,
        UnderlineBold,
        UnderlineBoldItalics,
        None
    }
}
