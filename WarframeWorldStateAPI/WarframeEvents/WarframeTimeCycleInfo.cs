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
        const long SECONDS_PER_CETUS_DAY_CYCLE = 6000;
        const long SECONDS_PER_CETUS_NIGHT_CYCLE = 3000;

        public TimeSpan TimeUntilNextCycleChange { get; private set; }
        public DateTime TimeOfNextCycleChange { get; private set; }
        public TimeSpan TimeSinceLastCycleChange { get; private set; }

        public TimeSpan TimeUntilNextCycleChangeCetus { get; private set; }
        public DateTime TimeOfNextCycleChangeCetus { get; private set; }

        private long CurrentTimeInSeconds { get; set; }

        private bool _isDayEarth { get; set; }
        private bool _isDayCetus { get; set; }

        public WarframeTimeCycleInfo() : base(string.Empty, "Earth", DateTime.Now)
        {
        }

        public void UpdateEarthTime(DateTime expirationTime, string timeRemaining, bool isDayEarth)
        {
            TimeUntilNextCycleChange = expirationTime.Subtract(DateTime.Now);
            TimeOfNextCycleChange = expirationTime;
            _isDayEarth = isDayEarth;
        }

        public void UpdateCetusTime(DateTime expirationTime, string timeRemaining, bool isDayCetus)
        {
            TimeUntilNextCycleChangeCetus = expirationTime.Subtract(DateTime.Now);
            TimeOfNextCycleChangeCetus = expirationTime;
            _isDayCetus = isDayCetus;
        }
        
        public bool EarthIsDay()
        {
            return _isDayEarth;
        }

        public bool CetusIsDay()
        {
            return _isDayCetus;
        }

        override public bool IsExpired()
        {
            return false;
        }
    }
}
