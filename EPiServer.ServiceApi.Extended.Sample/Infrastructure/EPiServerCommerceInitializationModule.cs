using System.Web.Routing;
using EPiServer.Commerce.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using InitializationModule = EPiServer.Commerce.Initialization.InitializationModule;

namespace EPiServer.ServiceApi.Extended.Sample.Infrastructure
{
    [InitializableModule]
    [ModuleDependency(typeof(InitializationModule))]
    public class EPiServerCommerceInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            CatalogRouteHelper.MapDefaultHierarchialRouter(RouteTable.Routes, false);
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }
    }
}