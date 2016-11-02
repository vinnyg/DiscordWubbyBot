using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeAlertMessageInformation
    {
        public string Destination { get; set; }
        public string Faction { get; set; }
        private int RewardQuantity { get; set; }
        public string Reward { get; set; }
        public string ExpireTime { get; set; }
    }
}
