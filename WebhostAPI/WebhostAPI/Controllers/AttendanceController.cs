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
using WebhostMySQLConnection;

namespace WebhostAPI.Controllers
{
    /// <summary>
    /// Access to Attendance Records.
    /// </summary>
    public class AttendanceController : ApiController
    {
        /// <summary>
        /// Submit attendance for a given section.  Request Content must include a List of <see cref="AttendanceInfo"/>.
        /// </summary>
        /// <param name="section_id">Id for the section that you are submitting attendance for.</param>
        /// <param name="content">List of attendance markings.</param>
        /// <returns>
        /// If the section Id is invalid, returns <see cref="HttpStatusCode.BadRequest">BadRequest</see> with an <see cref="ArgumentException"/>.
        /// If The currently authorized teacher is not a teacher for this section, then returns <see cref="HttpStatusCode.NotAcceptable"/> with an <see cref="InvalidOperationException"/>.
        /// 
        /// Otherwise, returns <see cref="HttpStatusCode.OK">OK</see> with the List of completed <see cref="AttendanceInfo">AttendanceInfo</see> objects filled out.
        /// </returns>
        /// <example>
        /// POST api/self/sections/278/attendance
        /// CONTENT-TYPE text/json
        /// CONTENT
        /// {
        ///     [
        ///         {
        ///             "StudentId":"17482",
        ///             "SectionId":"278",
        ///             "Marking":"Present",
        ///             "Date":"2016-12-2"
        ///         },
        ///         {
        ///             "StudentId":"28192",
        ///             "SectionId":"278",
        ///             "Marking":"Present",
        ///             "Date":"2016-12-2"
        ///         },
        ///         {
        ///             "StudentId":"12738",
        ///             "SectionId":"278",
        ///             "Marking":"Late",
        ///             "Date":"2016-12-2"
        ///         }
        ///     ]
        /// }
        /// </example>
        [Route("api/self/sections/{section_id:int}/attendance")]
        [WebhostAuthorize(Roles = "Teachers")]
        public IHttpActionResult PutMyAttendance(int section_id, [FromBody] List<AttendanceInfo> content)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int tid = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(tid);

                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                if (!section.Teachers.Contains(self))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, new InvalidOperationException("That is not your class.")));


                List<int> outputIds = new List<int>();

                int nextId = db.AttendanceMarkings.OrderBy(m => m.id).ToList().Last().id;

                foreach (AttendanceInfo info in content)
                {
                    if (info.SectionId != section_id)
                        continue;

                    AttendanceMarking marking = null;

                    if (section.AttendanceMarkings.Where(s => s.AttendanceDate.Equals(info.Date) && s.StudentID == info.StudentId).Count() > 0)
                    {
                        marking = section.AttendanceMarkings.Where(s => s.AttendanceDate.Equals(info.Date) && s.StudentID == info.StudentId).Single();
                        marking.MarkingIndex = AttendanceInfo.LookUpAttendanceMarking(info.Marking);
                        marking.SubmittedBy = tid;
                        if (!String.IsNullOrEmpty(info.Notes))
                            marking.Notes += String.Format("[{0} {1}] {2}", self.FirstName, self.LastName, info.Notes);
                        marking.SubmissionTime = DateTime.Now;

                        outputIds.Add(marking.id);
                    }
                    else
                    {
                        marking = new WebhostAPI.AttendanceMarking()
                        {
                            id = ++nextId,
                            AttendanceDate = info.Date,
                            MarkingIndex = AttendanceInfo.LookUpAttendanceMarking(info.Marking),
                            SubmittedBy = tid,
                            Notes = String.Format("[{0} {1}] {2}", self.FirstName, self.LastName, String.IsNullOrEmpty(info.Notes) ? "" : info.Notes),
                            StudentID = info.StudentId,
                            SectionIndex = info.SectionId,
                            SubmissionTime = DateTime.Now
                        };

                        db.AttendanceMarkings.Add(marking);
                        outputIds.Add(marking.id);
                    }
                }

                List<AttendanceInfo> output = new List<AttendanceInfo>();
                foreach (int iid in outputIds)
                {
                    output.Add(new RequestHandlers.AttendanceInfo(iid, true));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, output, "text/json"));
            }
        }

        /// <summary>
        /// Get the list of <see cref="AttendanceInfo">AttendanceInfo</see> markings for a given Date.
        /// </summary>
        /// <param name="section_id"><see cref="SectionInfo.Id">Id</see> for the object</param>
        /// <param name="datebinary">Optionally, you can provide a Date object in the query string to say what Date to pull data for.</param>
        /// <returns>
        /// Gets the List of <see cref="AttendanceInfo">AttendanceInfo</see> as json.
        /// </returns>
        /// <see cref="AttendanceInfo"/>
        [Route("api/self/sections/{section_id:int}/attendance")]
        [WebhostAuthorize(Roles = "Teachers")]
        public IHttpActionResult GetAttendances(int section_id, [FromUri] long datebinary = -1)
        {
            DateTime date = DateTime.Today;
            if (datebinary != -1)
                date = DateTime.FromBinary(datebinary);


            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int tid = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(tid);

                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                if (!section.Teachers.Contains(self))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, new InvalidOperationException("That is not your class.")));

                List<int> ids = section.AttendanceMarkings.Where(a => a.AttendanceDate.Equals(date)).Select(a => a.id).ToList();
                List<AttendanceInfo> output = new List<AttendanceInfo>();
                foreach (int id in ids)
                {
                    output.Add(new RequestHandlers.AttendanceInfo(id));
                }
                
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, output, "text/json"));
            }
        }

        /// <summary>
        /// Students can access their AttendanceWeek data.
        /// </summary>
        /// <returns>
        /// Gets a List of <see cref="AttendanceInfo">Attendance Info</see> with all the attendance submitted for the authorized student.
        /// </returns>
        [Route("api/self/attendance")]
        [WebhostAuthorize(Roles = "Students")]
        public IHttpActionResult GetStudentAttendance()
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Student self = db.Students.Find(id);

                DateRange thisWeek = DateRange.ThisAttendanceWeek;

                List<AttendanceInfo> output = new List<RequestHandlers.AttendanceInfo>();
                foreach(AttendanceMarking mark in self.AttendanceMarkings.Where(a => a.AttendanceDate >= thisWeek.Start && a.AttendanceDate <= thisWeek.End).ToList())
                {
                    output.Add(new RequestHandlers.AttendanceInfo(mark.id));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, output, "text/json"));
            }
        }


    }
}
