using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net;
//using DiscordSharpTest.Events;
//using DiscordSharpTest.WarframeEvents;
using System.Configuration;
using WarframeDatabaseNet;
using WarframeDatabaseNet.Persistence;

namespace WarframeWorldStateAPI.Components
{
    //This class parses a JSON file and raises events regarding the contents
    internal class WarframeJSONScraper
    {
        //TODO: Break this class down into two; a dedicated scraper class and JSON parser class
        //Consider responsibility of raising events
        private const int SECONDS_PER_DAY_CYCLE = 14400;
        //Limit how often we can scrape the URL
        private const int SCRAPE_LIMIT_INTERVAL_IN_SECONDS = 60;

        //Store the JSON file
        private JObject _worldState { get; set; }
        private DateTime _lastScraped;

        //Download Warframe content information
        internal JObject ScrapeWorldState()
        {
            const int NUMBER_OF_RETRIES = 3;
            const int RETRY_DELAY_IN_MILLISECONDS = 2000;

            using (WebClient wc = new WebClient())
            {
                //If a failure to download the content occurs, retry two more times
                for (int attempt = 0; attempt < NUMBER_OF_RETRIES; ++attempt)
                {
                    try
                    {
                        //If the required time interval has passed, we attempt to scrape the URL, otherwise just return the last downloaded JSON object
                        if (CanScrape())
                        {
                            _worldState = JObject.Parse(wc.DownloadString(ConfigurationManager.AppSettings["WarframeContentURL"]));
                            _lastScraped = DateTime.Now;
                        }
                        return _worldState;
                    }
                    catch (WebException)
                    {
                        if (attempt >= NUMBER_OF_RETRIES)
                        {
                            if (_worldState != null)
                            {
                                return _worldState;
                            }
                            //Throw an exception if _worldState has never been assigned, i.e. we have not scraped the URL yet
                            throw;
                        }

                        Thread.Sleep(RETRY_DELAY_IN_MILLISECONDS);
                    }
                }
            }
            return null;
        }

        //Check whether the required amount of time has passed before another scrape attempt can be made
        public bool CanScrape()
        {
            return (DateTime.Now >= _lastScraped.AddSeconds(SCRAPE_LIMIT_INTERVAL_IN_SECONDS));
        }
    }
}
