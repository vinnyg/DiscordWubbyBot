using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeInvasion : WarframeEvent
    {
        private const CHANGE_RATE_MAX_HISTORY = 60;
        public string Type { get; private set; }
        public MissionInfo AttackerDetails { get; private set; }
        public MissionInfo DefenderDetails { get; private set; }
        public float Progress { get; private set; }
        private int Goal { get; set; }
        public float ChangeRate { get; private set; }
        private Queue<float> ChangeRateHistory { get; set; }

        public WarframeInvasion(MissionInfo attackerInfo, MissionInfo defenderInfo, string guid, string destinationName, DateTime startTime, int goal) : base(guid, destinationName, startTime)
        {
            //Indicates the progress made towards a particular side as a percentage.
            Progress = .0f;
            AttackerDetails = attackerInfo;
            DefenderDetails = defenderInfo;
            //We check the defender information because the defender information contains information corresponding to the mission that they give and vice versa.
            Type = DefenderDetails.Faction == Faction.INFESTATION ? InvasionType.OUTBREAK : InvasionType.INVASION;
            Goal = goal;
            ChangeRateHistory = new Queue<float>();
        }

        public void UpdateProgress(int progress)
        {
            int direction = progress != 0 ? (System.Math.Abs(progress) / progress) : 1;
            float prevProg = Progress;
            Progress = (((float)Math.Abs(progress) / (float)Goal) * direction);
            //Enqueue new entries every minute so that an more accurate average can be calculated.
            ChangeRateHistory.Enqueue((Progress - prevProg) * direction);
            //We are only measuring the past hour.
            if (ChangeRateHistory.Count > CHANGE_RATE_MAX_HISTORY)
                ChangeRateHistory.Dequeue();
            float sigmaChangeRate = .0f;
            foreach(var i in ChangeRateHistory)
            {
                sigmaChangeRate = sigmaChangeRate + i;
            }
            ChangeRate = (sigmaChangeRate / ChangeRateHistory.Count()) * CHANGE_RATE_MAX_HISTORY;
        }

        override public bool IsExpired()
        {
            return (System.Math.Abs(Progress) >= 1.0f);
        }
    }
}
