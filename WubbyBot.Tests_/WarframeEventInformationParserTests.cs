using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeWorldStateAPI.Components;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace WubbyBot.Tests
{
    [TestFixture]
    public class WarframeEventInformationParserTests
    {
        [Test]
        public void EventParserReturnsNoAlertsWhenThereAreNoAlerts()
        {
            var scraper = new WarframeJsonScraperMock();
            scraper.JsonFilepath = "ParserTests/NoAlerts.json";

            var parser = new WarframeEventInformationParser(scraper);
            var alerts = parser.GetAlerts();

            Assert.That(alerts.Count, Is.EqualTo(0));
        }

        internal class WarframeJsonScraperMock : IWarframeJSONScraper
        {
            //public JObject WorldState { get; set; }
            public string JsonFilepath { get; set; }

            public bool CanScrape()
            {
                return true;
            }

            public JObject ScrapeWorldState()
            {
                return ReadJsonFile();
            }

            private JObject ReadJsonFile()
            {
                var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var jsonFile = File.ReadAllText($"{directoryName}\\{JsonFilepath}");
                return JObject.Parse(jsonFile);
            }
        }
    }
}
