using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Core.Repository.Interfaces;

namespace WarframeDatabaseNet.Persistence.Repository
{
    class WFSolarNodeRepository : Repository<WFSolarNode>, IWFSolarNodeRepository
    {
        public WFSolarNodeRepository(WarframeDataContext context) : base(context)
        {

        }

        public WarframeDataContext WFDataContext
        {
            get { return Context as WarframeDataContext; }
        }

        public bool ArchwingRequired(string nodeURI)
        {
            bool result = false;

            int nodeID = WFDataContext.SolarNodes.Where(x => x.NodeURI == nodeURI).Single().ID;

            var node = WFDataContext.SolarMapMissions.Where(x => x.NodeID == nodeID);
            if (node.Count() > 0)
                result = (node.Single().RequiresArchwing > 0);

            return result;
        }

        public string GetFaction(string nodeURI)
        {
            string result = "FC_OROKIN";

            int nodeID = WFDataContext.SolarNodes.Where(x => x.NodeURI == nodeURI).Single().ID;

            var node = WFDataContext.SolarMapMissions.Where(x => x.NodeID == nodeID);
            if (node.Count() > 0)
                result = node.Single().Faction;

            return result;
        }

        public int GetMaxLevel(string nodeURI)
        {
            int result = 0;

            int nodeID = WFDataContext.SolarNodes.Where(x => x.NodeURI == nodeURI).Single().ID;

            var node = WFDataContext.SolarMapMissions.Where(x => x.NodeID == nodeID);
            if (node.Count() > 0)
                result = node.Single().MaxLevel;
                
            return result;
        }

        public int GetMinLevel(string nodeURI)
        {
            int result = 0;

            int nodeID = WFDataContext.SolarNodes.Where(x => x.NodeURI == nodeURI).Single().ID;

            var node = WFDataContext.SolarMapMissions.Where(x => x.NodeID == nodeID);
            if (node.Count() > 0)
                result = node.Single().MinLevel;

            return result;
        }

        public string GetMissionType(string nodeURI)
        {
            string result = nodeURI;

            int nodeID = WFDataContext.SolarNodes.Where(x => x.NodeURI == nodeURI).Single().ID;

            var node = WFDataContext.SolarMapMissions.Where(x => x.NodeID == nodeID);
            if (node.Count() > 0)
                result = node.Single().MissionType;

            return result;
        }

        public string GetNodeName(string nodeURI)
        {
            string result = nodeURI;
            
            try
            {
                result = WFDataContext.SolarNodes.Where(x => x.NodeURI == nodeURI).Single().NodeName;
            }
            catch (InvalidOperationException)
            {
                result = nodeURI;
            }
            return result;
        }

        public string GetNodeType(string nodeURI)
        {
            string result = "MT_GENERIC";

            int nodeID = WFDataContext.SolarNodes.Where(x => x.NodeURI == nodeURI).Single().ID;

            var node = WFDataContext.SolarMapMissions.Where(x => x.NodeID == nodeID);
            if (node.Count() > 0)
                result = node.Single().NodeType;
                
            return result;
        }
    }
}
