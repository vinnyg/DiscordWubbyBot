using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    struct WarframeEventMessage
    {
        public string Content;
        public string UnformattedContent;

        public WarframeEventMessage(string content, string unformattedContent)
        {
            Content = content;
            UnformattedContent = unformattedContent;
        }
    }
}
