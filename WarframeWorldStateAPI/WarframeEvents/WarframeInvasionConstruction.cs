using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using DiscordSharpTest.WarframeEvents.Properties;

namespace WarframeWorldStateAPI.WarframeEvents
{
    /*----Message Example----/
    Grineer Balor Fomorian 21%
    Corpus Razorback 24%
    ------------------------*/

    //Contains information regarding a faction's construction project
    public class WarframeInvasionConstruction : WarframeEvent
    {
        public WarframeInvasionConstruction(string guid, int factionID, double progress) : base(guid, "World", DateTime.Now)
        {
            ProjectName = Properties.Faction.GetProjectNameByID(factionID);
            ProjectProgress = progress;
            Faction = Properties.Faction.GetNameByID(factionID);
        }
        
        public string Faction { get; private set; }
        public string ProjectName { get; private set; }
        public double ProjectProgress { get; private set; }

        public override bool IsExpired()
        {
            return false;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
        }

        public void UpdateProgress(double progress)
        {
            if (progress >= 0)
            {
                ProjectProgress = progress;
            }
        }
    }
}
