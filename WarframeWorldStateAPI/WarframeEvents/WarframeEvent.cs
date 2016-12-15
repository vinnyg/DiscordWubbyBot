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
        private const int SECONDS_UNTIL_EVENT_NOT_NEW = 60;

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

        //Check if the event started recently
        public bool IsNew()
        {
            var timeEventIsNotNew = StartTime.AddSeconds(SECONDS_UNTIL_EVENT_NOT_NEW);
            return ((DateTime.Now >= StartTime) && (DateTime.Now < timeEventIsNotNew));
        }
    }
}
