using System;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net;
using System.Configuration;
using System.Runtime.Caching;

namespace WarframeWorldStateAPI.Components
{
    //This class parses a JSON file and raises events regarding the contents
    internal class WarframeJSONScraper : IWarframeJSONScraper
    {
        private MemoryCache jsonCache = MemoryCache.Default;
        private const int SECONDS_PER_DAY_CYCLE = 14400;

        //Store the JSON file
        private JObject _worldState { get; set; }

        public JObject WorldState
        {
            get => ScrapeWorldState("http://content.warframe.com/dynamic/worldState.php") ?? _worldState;
            private set => _worldState = value;
        }

        //Store the JSON file
        private JObject _warframeStatusWorldState { get; set; }

        public JObject WarframeStatusWorldState
        {
            get => ScrapeWorldState("http://ws.warframestat.us/pc") ?? _worldState;
            private set => _worldState = value;
        }

        //Download Warframe content information
        private JObject ScrapeWorldState(string warframeApiUrl)
        {
            var jsonObject = jsonCache.Get(warframeApiUrl) as JObject;
            if (jsonObject == null)
            {
                jsonObject = Request(warframeApiUrl);
                jsonCache.Add(warframeApiUrl, jsonObject, DateTimeOffset.Now.AddMinutes(1));
                
            }
            return jsonObject;
        }

        private JObject Request(string url)
        {
            using (WebClient wc = new WebClient())
            {
                _worldState = JObject.Parse(wc.DownloadString(url));

                return _worldState;
            }
        }
    }
}
