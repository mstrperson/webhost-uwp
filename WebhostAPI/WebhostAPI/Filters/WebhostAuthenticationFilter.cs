using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using WebhostAPI.Models;

namespace WebhostAPI.Filters
{

    /// <summary>
    /// The framework for this class was taken verbatim from:
    /// https://github.com/mbenford/WebApi-AuthenticationFilter/blob/master/WebApi.AuthenticationFilter/AuthenticationFilterAttribute.cs
    /// 
    /// </summary>
    public class WebhostAuthenticationFilterAttribute : Attribute, IAuthenticationFilter
    {
        /// <summary>
        /// Authenticates the request asynchronously.
        /// </summary>
        /// <param name="context">The authentication context.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that will perform the authentication challenge.</returns>
        public Task OnAuthenticationAsync(AuthenticationContext context, CancellationToken cancellationToken)
        {
            try
            {
                OnAuthentication(context);
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }

            return Completed();
        }

        /// <summary>
        /// Authenticates the request.
        /// </summary>
        /// <param name="context">The authentication context.</param>
        public void OnAuthentication(AuthenticationContext context)
        {
            if (context.HttpContext.Request.Headers.AllKeys.Contains("Authorization"))
            {
                String fpString = context.HttpContext.Request.Headers["Authorization"];

                context.Principal = new WebhostUserPrinciple(Convert.FromBase64String(fpString));
            }
            else
            {
                context.Result = new HttpUnauthorizedResult("Authentication Failed.");
            }
            
        }

        /// <summary>
        /// Adds an authentication challenge to the inner IHttpActionResult asynchronously.
        /// </summary>
        /// <param name="context">The authentication challenge context.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that will perform the authentication challenge.</returns>
        public virtual Task OnAuthenticationChallengeAsync(AuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            try
            {
                OnAuthenticationChallenge(context);
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }

            return Completed();
        }

        /// <summary>
        /// Adds an authentication challenge to the inner IHttpActionResult.
        /// </summary>
        /// <param name="context">The authentication challenge context.</param>
        public virtual void OnAuthenticationChallenge(AuthenticationChallengeContext context)
        {
            if (context.HttpContext.Request.Headers.AllKeys.Contains("Authorization"))
            {
                String fpString = context.HttpContext.Request.Headers["Authorization"];
                HttpCookie cookie = new HttpCookie("auth", fpString);
                //context. = new WebhostUserPrinciple(Convert.FromBase64String(fpString));
            }
            else
            {
                context.Result = new HttpUnauthorizedResult("Authentication Failed.");
            }
            //throw new NotImplementedException("Challenge Authentication is not implemented.");
        }
        
        private async Task AuthenticateAsync(AuthenticationContext context, CancellationToken cancellationToken)
        {
            await OnAuthenticationAsync(context, cancellationToken);
        }

        private async Task ChallengeAsync(AuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            await OnAuthenticationChallengeAsync(context, cancellationToken);
        }

        private static Task Completed()
        {
            return Task.FromResult(default(AsyncVoid));
        }

        private static Task<AsyncVoid> FromError(Exception exception)
        {
            var tcs = new TaskCompletionSource<AsyncVoid>();
            tcs.SetException(exception);
            return tcs.Task;
        }
        
        private struct AsyncVoid
        {
        }
    }
}