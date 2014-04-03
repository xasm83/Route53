using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Route53
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                name: "Update",
                url: "{action}",
                defaults: new { controller = "Route53" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{action}",
                defaults: new { controller = "Route53", action = "InfoPage" }
            );
        }
    }
}