using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DiscordSharpTest
{
    static class WarframeAlertTweetParser
    {
        static public WarframeEvent ParseTweet(string tweetText)
        {
            string[] splitAlert = tweetText.Split('|');
            string destinationName = splitAlert[0];
            string mission = (splitAlert[1].Split('('))[0].Trim();
            string faction = Regex.Match(splitAlert[1], @"\((.*?)\)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
            int credits = int.Parse(Regex.Match(tweetText, @"([\d,])+(?=cr)", RegexOptions.None).Value, NumberStyles.AllowThousands);
            string loot = splitAlert[5].Split('-')[1];

            //Get the total timespan until the alert expires
            int i, j;
            int.TryParse(Regex.Match(splitAlert[3], @"(\d+)", RegexOptions.None).Value, out i);
            int.TryParse(Regex.Match(splitAlert[4], @"(\d+)", RegexOptions.None).Value, out j);

            //The time that the alert will expire is all we will pass for now
            /*TimeSpan timeToExpire = new TimeSpan(0, i + j, 0);
            DateTime expirationTime = DateTime.Now + timeToExpire;*/

            //return new WarframeAlert("", destinationName, faction, mission, credits, loot, i + j, );
            return new WarframeAlert(null, "", "", DateTime.Now, DateTime.Now);
        }
    }
}
