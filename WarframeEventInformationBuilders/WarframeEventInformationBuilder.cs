using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeWorldStateAPI.WarframeEvents;
using WubbyBot.Events.Extensions;

namespace DiscordSharpTest.WarframeEventInfoStringBuilders
{
    public class WarframeEventInfoStringBuilder
    {
        public string BuildAlertInformation(IReadOnlyList<WarframeAlert> alerts)
        {
            var finalMessage = new StringBuilder();
            var messagesToNotify = new List<string>();

            if (alerts.Count == 0)
            {
                finalMessage.Append(WarframeEventExtensions.FormatMessage("NO ACTIVE ALERTS", preset: MessageMarkdownLanguageIdPreset.ActiveEvent, formatType: MessageFormat.CodeBlocks));
            }

            alerts = alerts.OrderBy(s => s.GetMinutesRemaining(false)).ToList();

            foreach (var alert in alerts)
            {
                var coreMessageContent = new StringBuilder();
                coreMessageContent.AppendLine(WarframeEventExtensions.DiscordMessage(alert as dynamic, false));
                var messageMarkdownPreset = MessageMarkdownLanguageIdPreset.ActiveEvent;

                if (alert.IsNew())
                {
                    coreMessageContent.Append("( new )");
                    messagesToNotify.Add(WarframeEventExtensions.DiscordMessage(alert as dynamic, true));
                    messageMarkdownPreset = MessageMarkdownLanguageIdPreset.NewEvent;
                }

                finalMessage.Append(WarframeEventExtensions.FormatMessage(coreMessageContent.ToString(), preset: messageMarkdownPreset, formatType: MessageFormat.CodeBlocks));
            }

            return finalMessage.ToString();
        }
    }
}
