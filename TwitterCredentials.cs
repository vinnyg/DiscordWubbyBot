using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordSharpTest
{
    class TwitterCredentials
    {
        [JsonProperty("consumer_key")]
        public string consumerKey { get; internal set; }

        [JsonProperty("consumer_secret")]
        public string consumerSecret { get; internal set; }

        [JsonProperty("access_token")]
        public string accessToken { get; internal set; }

        [JsonProperty("access_token_secret")]
        public string accessTokenSecret { get; internal set; }
    }
}
