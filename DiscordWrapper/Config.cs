using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordWrapper
{
    public class Config
    {
        [JsonProperty("bot_email")]
        public string email { get; internal set; }

        [JsonProperty("bot_password")]
        public string password { get; internal set; }

        [JsonProperty("command_prefix")]
        public char commandPrefix { get; internal set; }

        [Obsolete]
        [JsonProperty("consumer_key")]
        public string consumerKey { get; internal set; }

        [Obsolete]
        [JsonProperty("consumer_secret")]
        public string consumerSecret { get; internal set; }

        [Obsolete]
        [JsonProperty("access_token")]
        public string accessToken { get; internal set; }

        [Obsolete]
        [JsonProperty("access_token_secret")]
        public string accessTokenSecret { get; internal set; }

        [JsonProperty("discord_token")]
        public string DiscordToken { get; internal set; }
    }
}
