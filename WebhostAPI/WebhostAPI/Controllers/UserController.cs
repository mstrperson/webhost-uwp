using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebhostAPI.Filters;
using WebhostAPI.Models;
using WebhostAPI.RequestHandlers;
using Newtonsoft.Json;
using System.IO;
using WebhostMySQLConnection;

namespace WebhostAPI.Controllers
{
    /// <summary>
    /// get information about a user.
    /// </summary>
    public class UserController : ApiController
    {
        /// <summary>
        /// Pass the the following information in json to the Body of the request.
        /// 
        /// {
        ///     "EncodedCredential":"<Base64EncodedData/>"
        /// }
        /// 
        /// where the data is a base64 encoded version of the user's credentials
        /// 
        /// 
        /// </summary>
        /// 
        /// <returns></returns>
        [Route("api/authenticate")]
        public async Task<IHttpActionResult> PostAuthentication()
        {
            try
            {
                AuthenticationInfo info = await Request.Content.ReadAsAsync<AuthenticationInfo>();
                WebhostEventLog.APILog.LogInformation("Authenticated {0} {1}", )
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, info, "text/json"));
            }
            catch (InvalidOperationException e)
            {
                try
                {
                    int statusCode = Convert.ToInt32(e.Message);
                    return ResponseMessage(Request.CreateResponse(statusCode));
                }
                catch
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Forbidden, e));
                }
            }
        }

        /// <summary>
        /// Who am I?
        /// </summary>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Administrators,Teachers,Auditors")]
        [Route("api/self")]
        public IHttpActionResult GetSelf()
        {
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, ((WebhostIdentity)((WebhostUserPrinciple)RequestContext.Principal).Identity).User, "text/json");
            return ResponseMessage(response);
        }
        
        /// <summary>
        /// Get's the current teacher's list of advisees.
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Administrators,Teachers")]
        [Route("api/self/advisees")]
        public IHttpActionResult GetAdvisees([FromUri] bool active = true)
        {
            List<StudentInfo> advisees = new List<StudentInfo>();
            using (WebhostEntities db = new WebhostEntities())
            {
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                foreach(Student student in self.Students.Where(s => s.isActive || !active).ToList())
                {
                    advisees.Add(new RequestHandlers.StudentInfo(student.ID));
                }
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, advisees, "text/json");
            return ResponseMessage(response);
        }

        /// <summary>
        /// Administrator access to other teachers' information.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Administrators,Auditors")]
        [Route("api/teacher/{id:int}")]
        public IHttpActionResult GetOther(int id)
        {
            TeacherInfo info = null;
            try
            {
                info = new RequestHandlers.TeacherInfo(id, true);
            }
            catch(ArgumentException e)
            {
                HttpResponseMessage fail = Request.CreateResponse(HttpStatusCode.BadRequest, e);
                return ResponseMessage(fail);
            }
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, info, "text/json");
            return ResponseMessage(response);
        }

        /// <summary>
        /// Administrative access to another teacher's advisee list.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="active"></param>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Administrators,Auditors")]
        [Route("api/teacher/{id:int}/advisees")]
        public IHttpActionResult GetOtherAdvisees(int id, bool active = true)
        {
            List<StudentInfo> advisees = new List<StudentInfo>();
            using (WebhostEntities db = new WebhostEntities())
            {
                Faculty self = db.Faculties.Find(id);
                if(self == null)
                {
                    HttpResponseMessage fail = Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid Teacher Id.");
                    return ResponseMessage(fail);
                }
                foreach (Student student in self.Students.Where(s => s.isActive || !active).ToList())
                {
                    advisees.Add(new RequestHandlers.StudentInfo(student.ID));
                }
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, advisees, "text/json");
            return ResponseMessage(response);
        }
    }
}
