using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Commerce.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Newtonsoft.Json;
using InitializationModule = EPiServer.Commerce.Initialization.InitializationModule;

namespace EPiServer.ServiceApi.Extended.Sample.Business
{
    [ModuleDependency(typeof(InitializationModule))]
    public class SiteInitialization : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
            CatalogRouteHelper.MapDefaultHierarchialRouter(RouteTable.Routes, false);
            EPiServer.Global.RoutesRegistered += Global_RoutesRegistered;
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            var services = context.Services;


            DependencyResolver.SetResolver(new StructureMapDependencyResolver(context.StructureMap()));
            GlobalConfiguration.Configure(config =>
            {
                config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.LocalOnly;
                config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings();
                config.Formatters.XmlFormatter.UseXmlSerializer = true;
                config.DependencyResolver = new StructureMapResolver(context.StructureMap());
                config.MapHttpAttributeRoutes();
            });
        }

        public void Uninitialize(InitializationEngine context)
        {
        }


        private void Global_RoutesRegistered(object sender, RouteRegistrationEventArgs e)
        {
        }
    }
}