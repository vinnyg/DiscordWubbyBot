using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Repository.Interfaces;

namespace WarframeDatabaseNet.Core
{
    interface IUnitOfWork : IDisposable
    {
        IWarframeItemRepository WarframeItems { get; }
        IWFDBOptionsRepository WFDatabaseOptions { get; }
        IWFSolarNodeRepository WFSolarNodes { get; }
        IWFSortieRepository WFSorties { get; }
        IWFVoidFissureRepository WFVoidFissures { get; }
        IWFEnemyRepository WFEnemies { get; }
        int Complete();

    }
}
