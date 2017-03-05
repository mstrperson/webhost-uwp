using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using WebhostAPI.RequestHandlers;

namespace WebhostAPI.Models
{
    public class WebhostUserPrinciple : IPrincipal
    {
        protected WebhostIdentity _Identity;

        public IIdentity Identity
        {
            get
            {
                return _Identity;
            }
        }

        public WebhostUserPrinciple(byte[] fingerprint)
        {
            _Identity = new Models.WebhostIdentity(fingerprint);
        }

        public bool IsInRole(string role)
        {
            return _Identity.User.Groups.Contains(role);
        }
    }
    
    public class WebhostIdentity : IIdentity
    {
        public RequestHandlers.WebhostUserInfo User
        {
            get;
            protected set;
        }

        public WebhostIdentity(byte[] fingerprint)
        {
            try
            {
                User = new RequestHandlers.TeacherInfo(fingerprint, true);
            }
            catch(AccessViolationException)
            {
                try
                {
                    User = new StudentInfo(fingerprint);
                }
                catch (AccessViolationException)
                {
                    User = null;
                }
            }
        }

        public string AuthenticationType
        {
            get
            {
                return "Webhost";
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return User != null;
            }
        }

        public string Name
        {
            get
            {
                return User.Email;
            }
        }
    }
}