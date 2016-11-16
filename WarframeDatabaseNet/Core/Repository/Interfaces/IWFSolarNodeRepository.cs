using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Enums.MissionType;
using WarframeDatabaseNet.Enums.NodeType;

namespace WarframeDatabaseNet.Core.Repository.Interfaces
{
    public interface IWFSolarNodeRepository : IRepository<WFSolarNode>
    {
        string GetNodeName(string nodeURI);
        string GetMissionType(string nodeURI);
        int GetMinLevel(string nodeURI);
        int GetMaxLevel(string nodeURI);
        string GetNodeType(string nodeURI);
        string GetFaction(string nodeURI);
        bool ArchwingRequired(string nodeURI);
    }
}
