using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;

namespace WarframeDatabaseNet.Core.Repository.Interfaces
{
    public interface IWFEnemyRepository : IRepository<WFEnemy>
    {
        string GetNameByURI(string uri);
    }
}
