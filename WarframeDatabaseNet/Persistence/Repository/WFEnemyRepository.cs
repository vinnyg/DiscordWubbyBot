using System.Linq;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Core.Repository.Interfaces;

namespace WarframeDatabaseNet.Persistence.Repository
{
    public class WFEnemyRepository : Repository<WFEnemy>, IWFEnemyRepository
    {
        public WFEnemyRepository(WarframeDataContext context) : base(context)
        {
        }

        public WarframeDataContext WFDataContext
        {
            get { return Context as WarframeDataContext; }
        }

        public string GetNameByURI(string uri)
        {
            var result = uri;
            var item = WFDataContext.WFEnemies.Where(x => x.URI == uri);
            if (item.Count() > 0)
                result = item.Single().Name;

            return result;
        }
    }
}
