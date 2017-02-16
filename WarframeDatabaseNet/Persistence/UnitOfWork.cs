using WarframeDatabaseNet.Core;
using WarframeDatabaseNet.Core.Repository.Interfaces;
using WarframeDatabaseNet.Persistence.Repository;

namespace WarframeDatabaseNet.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly WarframeDataContext _context;

        public IWarframeItemRepository WarframeItems { get; private set; }
        public IWFDBOptionsRepository WFDatabaseOptions { get; private set; }
        public IWFSolarNodeRepository WFSolarNodes { get; private set; }
        public IWFSortieRepository WFSorties { get; private set; }
        public IWFVoidFissureRepository WFVoidFissures { get; private set; }
        public IWFEnemyRepository WFEnemies { get; private set; }

        //Create a new context per unit of work.
        public UnitOfWork(WarframeDataContext context)
        {
            _context = context;

            WarframeItems = new WarframeItemRepository(_context);
            WFDatabaseOptions = new WFDBOptionsRepository(_context);
            WFSolarNodes = new WFSolarNodeRepository(_context);
            WFSorties = new WFSortieRepository(_context);
            WFVoidFissures = new WFVoidFissureRepository(_context);
            WFEnemies = new WFEnemyRepository(_context);
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}
