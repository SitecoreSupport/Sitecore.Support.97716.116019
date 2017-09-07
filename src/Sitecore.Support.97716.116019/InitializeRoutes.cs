using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace Sitecore.Support.Forms.Mvc.Pipelines.Initialize
{
    class InitializeRoutes : Sitecore.Mvc.Pipelines.Loader.InitializeRoutes
    {
        protected override void RegisterRoutes(RouteCollection routes, PipelineArgs args)
        {
            if (routes.Remove(RouteTable.Routes["Form"]))
            {
                routes.MapRoute(
                    "Form",
                    "form/{action}",
                    new { controller = "Form", action = "Process" },
                    new[] { "Sitecore.Support.Forms.Mvc.Controllers" });
            }
        }
    }
}