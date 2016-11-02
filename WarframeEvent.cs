using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace DiscordSharpTest
{
    /*----Message Example----/
    Destination: Acanth, Eris (28-30)
    Mission: Hive Sabotage (Infestation)
    Reward: 1xNitain Extract, 11800cr    
    Status: Expired 13:37
    ------------------------*/

    /*----Message Example----/
    Destination: Acanth, Eris
    Mission: Grineer (Extermination) vs Corpus (Spy)
    Reward: Orokin Catalyst // Orokin Reactor
    Progress: 69% (Corpus)
    ------------------------*/

    /*----Message Example----/
    Destination: Acanth, Eris
    Mission: Infestation vs Corpus (Spy)
    Reward: 3xFieldron
    Progress: 69% (Corpus)
    ------------------------*/

    abstract public class WarframeEvent
    {
        public string GUID { get; private set; }
        public string DestinationName { get; private set; }
        public DateTime StartTime { get; private set; }

        public WarframeEvent(string guid, string destinationName, DateTime startTime)
        {
            GUID = guid;
            DestinationName = destinationName;
            StartTime = startTime;
        }

        public abstract bool IsExpired();

        protected void SetDestinationNode(string destination)
        {
            DestinationName = destination;
        }
    }
}
