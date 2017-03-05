using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using WebhostAPI.Models;

namespace WebhostAPI.Filters
{
    public class WebhostAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            base.OnAuthorization(actionContext);
        }

        public override Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            return base.OnAuthorizationAsync(actionContext, cancellationToken);
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            actionContext.Response = new System.Net.Http.HttpResponseMessage(HttpStatusCode.Forbidden);

        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Authorization != null)
            {
                String fpString = actionContext.Request.Headers.Authorization.Parameter;

                actionContext.RequestContext.Principal = new WebhostUserPrinciple(Convert.FromBase64String(fpString));
            }
            else
            {
                return false;
            }
            if (!actionContext.RequestContext.Principal.Identity.IsAuthenticated)
                return false;
            
            foreach(String role in Roles.Split(','))
            {
                if(actionContext.RequestContext.Principal.IsInRole(role))
                {
                    return true;
                }
            }

            return false;
        }
    }
}