using System;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net;
using System.Configuration;
using System.Runtime.Caching;

namespace WarframeWorldStateAPI.Components
{
    ///This class parses a JSON file and raises events regarding the contents
    internal class WarframeJSONScraper : IWarframeJSONScraper
    {
        private MemoryCache jsonCache = MemoryCache.Default;
        private const int SECONDS_PER_DAY_CYCLE = 14400;

        private JObject _worldState { get; set; } = new JObject();

        public JObject WorldState
        {
            get => ScrapeWorldState("http://content.warframe.com/dynamic/worldState.php") ?? _worldState;
            private set => _worldState = value;
        }
        
        private JObject _warframeStatusWorldState { get; set; }

        public JObject WarframeStatusWorldState
        {
            get => ScrapeWorldState("http://ws.warframestat.us/pc") ?? _warframeStatusWorldState;
            private set => _warframeStatusWorldState = value;
        }
        
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
                try
                {
                    _worldState = JObject.Parse(wc.DownloadString(url));
                }
                catch (WebException)
                {
                    //Log error
                }

                return _worldState;
            }
        }
    }
}
