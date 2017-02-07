using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeWorldStateAPI.WarframeEvents
{
    public class WarframeTimeCycleInfo : WarframeEvent
    {
        const long SECONDS_PER_DAY_CYCLE = 14400;
        public TimeSpan TimeUntilNextCycleChange { get; private set; }
        public DateTime TimeOfNextCycleChange { get; private set; }
        public TimeSpan TimeSinceLastCycleChange { get; private set; }
        private long CurrentTimeInSeconds { get; set; }
        private bool _isDay { get; set; }

        public WarframeTimeCycleInfo(long currentWarframeServerTime) : base(string.Empty, "Earth", DateTime.Now)
        {
            UpdateTimeInformation(currentWarframeServerTime);
        }

        public void UpdateTimeInformation(long currentTime)
        {
            long secondsSinceLastCycleChange = currentTime % SECONDS_PER_DAY_CYCLE;

            CurrentTimeInSeconds = currentTime;
            TimeSinceLastCycleChange = TimeSpan.FromSeconds(currentTime % SECONDS_PER_DAY_CYCLE);
            TimeUntilNextCycleChange = TimeSpan.FromSeconds(SECONDS_PER_DAY_CYCLE - secondsSinceLastCycleChange);
            TimeOfNextCycleChange = DateTime.Now.Add(TimeUntilNextCycleChange);
        }
        
        //TODO Finish this
        public bool TimeIsDay()
        {
            //Store the seconds since last change; we don't want this as a TimeSpan yet.
            long secondsSinceLastCycleChange = CurrentTimeInSeconds % SECONDS_PER_DAY_CYCLE;
            //Time information is updated every minute, so using old info has negligible effects
            long timeOfLastChangeInSeconds = CurrentTimeInSeconds - secondsSinceLastCycleChange;
            //Check whether the cycle is day/night
            long cycleCountMod = ((timeOfLastChangeInSeconds / SECONDS_PER_DAY_CYCLE) % 2);
            return (cycleCountMod == 0);
        }

        override public bool IsExpired()
        {
            return false;
        }
    }
}
