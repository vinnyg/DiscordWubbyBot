using Newtonsoft.Json.Linq;

namespace WarframeWorldStateAPI.Components
{
    public interface IWarframeJSONScraper
    {
        //JObject ScrapeWorldState();
        JObject WorldState { get; }
        JObject WarframeStatusWorldState { get; }
    }
}