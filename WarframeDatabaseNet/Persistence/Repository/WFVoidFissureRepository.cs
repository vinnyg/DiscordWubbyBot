using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarframeDatabaseNet.Core.Domain;
using WarframeDatabaseNet.Core.Repository.Interfaces;

namespace WarframeDatabaseNet.Persistence.Repository
{
    class WFVoidFissureRepository : Repository<WFVoidFissure>, IWFVoidFissureRepository
    {
        public WFVoidFissureRepository(WarframeDataContext context) : base(context)
        {

        }

        public WarframeDataContext WFDataContext
        {
            get { return Context as WarframeDataContext; }
        }
        
        public string GetFissureName(string fissureURI)
        {
            string result = fissureURI;
            
            var item = WFDataContext.WFVoidFissures.Where(x => x.FissureURI == fissureURI);
            if (item.Count() > 0)
                result = item.Single().FissureName;

            return result;
        }
    }
}
