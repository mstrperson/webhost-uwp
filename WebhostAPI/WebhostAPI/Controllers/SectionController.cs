using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebhostAPI.Filters;
using WebhostAPI.Models;
using WebhostAPI.RequestHandlers;
using WebhostMySQLConnection;

namespace WebhostAPI.Controllers
{
    public class SectionController : ApiController
    {
        [Route("api/sections/current")]
        [WebhostAuthorize(Roles = "Administrators,Auditors")]
        public IHttpActionResult GetAllCurrentSections([FromUri] bool detailed = false)
        {
            List<SectionInfo> sectionInfo = new List<SectionInfo>();
            using (WebhostEntities db = new WebhostEntities())
            {
                int termId = DateRange.GetCurrentOrLastTerm();
                List<int> sectionIds = db.Sections.Where(s => s.Terms.Where(t => t.id == termId).Count() > 0).Select(s => s.id).ToList();
                foreach(int id in sectionIds)
                {
                    sectionInfo.Add(new SectionInfo(id, detailed));
                }
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, sectionInfo, "text/json");
            return ResponseMessage(response);
        }

        [Route("api/self/sections/current")]
        [WebhostAuthorize(Roles = "Teachers,Students")]
        public IHttpActionResult GetCurrentSections([FromUri] bool detailed = false)
        {
            List<SectionInfo> sectionInfo = new List<SectionInfo>();
            using (WebhostEntities db = new WebhostEntities())
            {
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int tid = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(tid);

                int termId = DateRange.GetCurrentOrLastTerm();
                List<int> sectionIds = self.Sections.Where(s => s.Terms.Where(t => t.id == termId).Count() > 0).Select(s => s.id).ToList();
                foreach (int id in sectionIds)
                {
                    sectionInfo.Add(new SectionInfo(id, detailed));
                }
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, sectionInfo, "text/json");
            return ResponseMessage(response);
        }

        [Route("api/sections/{id:int}")]
        [WebhostAuthorize(Roles = "Teachers,Administrators,Auditors")]
        public IHttpActionResult GetSectionById(int id, [FromUri] bool detailed = true)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(id);
                if(section == null)
                {
                    return ResponseMessage(
                        Request.CreateErrorResponse(
                            HttpStatusCode.BadRequest, 
                            new ArgumentNullException(nameof(id), "Invaild Section Id.")));
                }

                return ResponseMessage(
                    Request.CreateResponse(
                        HttpStatusCode.OK, 
                        new SectionInfo(id, detailed), 
                        "text/json"));
            }
        }

        [Route("api/sections/{id:int}/roster")]
        [WebhostAuthorize(Roles ="Teachers,Administrators,Auditors")]
        public IHttpActionResult GetSectionRoster(int id)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(id);
                if (section == null)
                {
                    return ResponseMessage(
                        Request.CreateErrorResponse(
                            HttpStatusCode.BadRequest,
                            new ArgumentNullException(nameof(id), "Invaild Section Id.")));
                }

                List<StudentInfo> output = new List<StudentInfo>();
                foreach(int sid in section.Students.Select(s => s.ID).ToList())
                {
                    output.Add(new StudentInfo(sid));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, output, "text/json"));
            }
        }
    }
}
