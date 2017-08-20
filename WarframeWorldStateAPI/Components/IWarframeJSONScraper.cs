using Newtonsoft.Json.Linq;

namespace WarframeWorldStateAPI.Components
{
    public interface IWarframeJSONScraper
    {
        bool CanScrape();
        JObject ScrapeWorldState();
    }
}