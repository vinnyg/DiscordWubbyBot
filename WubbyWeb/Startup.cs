using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WubbyWeb.Startup))]
namespace WubbyWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
