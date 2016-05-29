using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class WarframeInvasion : WarframeEvent
    {
        public string Type { get; private set; }
        public MissionInfo AttackerDetails { get; private set; }
        public MissionInfo DefenderDetails { get; private set; }
        private float Progress { get; set; }
        private int Goal { get; set; }

        public WarframeInvasion(MissionInfo attackerInfo, MissionInfo defenderInfo, string guid, string destinationName, DateTime startTime, int goal) : base(guid, destinationName, startTime)
        {
            //Indicates the progress made towards a particular side as a percentage.
            Progress = .0f;
            AttackerDetails = attackerInfo;
            DefenderDetails = defenderInfo;
            //We check the defender information because the defender information contains information corresponding to the mission that they give and vice versa.
            Type = DefenderDetails.Faction == Faction.INFESTATION ? InvasionType.OUTBREAK : InvasionType.INVASION;
            Goal = goal;
        }

        public void UpdateProgress(int progress)
        {
            int direction = progress != 0 ? (System.Math.Abs(progress) / progress) : 1;
            Progress = (((float)System.Math.Abs(progress) / (float)Goal) * direction);
        }

        public float GetProgress()
        {
            return Progress;
        }

        override public bool IsExpired()
        {
            return (System.Math.Abs(Progress) >= 1.0f);
        }
    }
}
