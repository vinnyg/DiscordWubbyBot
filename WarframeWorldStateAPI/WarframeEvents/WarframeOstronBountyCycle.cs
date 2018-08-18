using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarframeWorldStateAPI.WarframeEvents
{
    public class WarframeOstronBountyCycle : WarframeEvent
    {
        const long SECONDS_PER_DAY_CYCLE = 9000;
        const long BOUNTY_CYCLE_OFFSET = 7740;
        public TimeSpan TimeUntilNextCycleChange { get; private set; }
        public DateTime TimeOfNextCycleChange { get; private set; }
        public List<Bounty> Bounties { get; private set; }
        private long CurrentTimeInSeconds { get; set; }
        private bool _isDay { get; set; }
        public string ExpiryTime { get; private set; }

        public WarframeOstronBountyCycle(long currentWarframeServerTime) : base(string.Empty, "Earth", DateTime.Now)
        {
            UpdateTimeInformation(currentWarframeServerTime);
        }

        public WarframeOstronBountyCycle(DateTime expiryTime) : base(string.Empty, "Earth", DateTime.Now)
        {
            TimeOfNextCycleChange = expiryTime;
            TimeUntilNextCycleChange = expiryTime.Subtract(DateTime.Now);
        }

        public void UpdateTimeInformation(long currentTime)
        {
            long secondsSinceLastCycleChange = ((currentTime + BOUNTY_CYCLE_OFFSET) % SECONDS_PER_DAY_CYCLE);

            CurrentTimeInSeconds = currentTime;
            TimeUntilNextCycleChange = TimeSpan.FromSeconds(SECONDS_PER_DAY_CYCLE - secondsSinceLastCycleChange);
            TimeOfNextCycleChange = DateTime.Now.Add(TimeUntilNextCycleChange);
        }

        override public bool IsExpired()
        {
            return false;
        }

        public class Bounty
        {
            private string jobType { get; }
            private List<string> rewards { get; }
            private int minimumLevel { get; }
            private int maximumLevel { get; }
            private int standing { get; }

            Bounty(string jobType, List<string> rewards, int minimumLevel, int maximumLevel, int standing)
            {
                this.jobType = jobType;
                this.rewards = rewards;
                this.minimumLevel = minimumLevel;
                this.maximumLevel = maximumLevel;
                this.standing = standing;
            }
        }
    }
}
