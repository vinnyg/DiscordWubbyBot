using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    abstract public class WarframeEventMessageInfo
    {
        public struct LevelRange
        {
            public int Min;
            public int Max;
        }

    }

    public class WarframeAlertMessageInfo : WarframeEventMessageInfo
    {
        public string Destination
        {
            get { return Destination; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("Destination cannot be null or empty");
                else
                    Destination = value;
            }
        }
        public string Faction
        {
            get { return Faction; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("Faction cannot be null or empty");
                else
                    Faction = value;
            }
        }
        public string MissionType
        {
            get { return MissionType; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("MissionType cannot be null or empty");
                else
                    MissionType = value;
            }
        }

        private LevelRange Levels;
        public int GetMinLevel()
        {
            return Levels.Min;
        }

        public int GetMaxLevel()
        {
            return Levels.Max;
        }

        public void SetLevelRange(int min, int max)
        {
            Levels.Min = min;
            Levels.Max = max;
        }

        public int RewardQuantity
        {
            get { return RewardQuantity; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("RewardQuantity cannot be negative.");
                else
                    RewardQuantity = value;
            }
        }
        public string Reward { get; set; }

        public int Credits
        {
            get { return Credits; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Credits cannot be negative.");
                else
                    Credits = value;
            }
        }

        public string Status { get; set; }

        public bool RequiresArchwing { get; set; }
    }
}
