using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WarframeWorldStateAPI.WarframeEvents
{
    //Base class for all Warframe Events to extend.
    public abstract class WarframeEvent
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
