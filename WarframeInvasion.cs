﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    public class WarframeInvasion : WarframeEvent
    {
        private const float CHANGE_RATE_MAX_HISTORY = 60.0f;
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
            //Calculates the faction which has greater progression.
            int direction = progress != 0 ? (System.Math.Abs(progress) / progress) : 1;
            float prevProg = Progress;
            //We want the absolute progress towards goal regardless of the direction.
            Progress = (((float)Math.Abs(progress) / (float)Goal) * direction);
            //If there is no previous history, calculate an estimated progression rate based on when the invasion started.
            if (ChangeRateHistory.Count() == 0)
            {
                //Console.WriteLine(DateTime.Now.ToString() + " :: " + StartTime.ToString());
                TimeSpan timeElapsedSinceStart = (DateTime.Now).Subtract(StartTime);
                //Calculate an estimated rate.
                //Prevent divide by zero when a new invasion has started
                int totalMins = timeElapsedSinceStart.Days * 24 * 60 + timeElapsedSinceStart.Hours * 60 + timeElapsedSinceStart.Minutes;

                if (totalMins > 0)
                    ChangeRateHistory.Enqueue((Progress / totalMins) * direction);
                else
                    ChangeRateHistory.Enqueue(0);
            }
            else
                //Enqueue new entries every minute so that a more accurate average can be calculated.
                ChangeRateHistory.Enqueue((Progress - prevProg) * direction);
            //We are only measuring the past hour.
            if (ChangeRateHistory.Count > CHANGE_RATE_MAX_HISTORY)
                ChangeRateHistory.Dequeue();
            float sigmaChange = .0f;
            foreach(var i in ChangeRateHistory)
            {
                sigmaChange = sigmaChange + i;
            }
            ChangeRate = (sigmaChange / ChangeRateHistory.Count()) * CHANGE_RATE_MAX_HISTORY;
        }

        override public bool IsExpired()
        {
            return (System.Math.Abs(Progress) >= 1.0f);
        }
    }
}