using System.Web;
using System.Web.Mvc;
using WebhostAPI.Filters;

namespace WebhostAPI
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
