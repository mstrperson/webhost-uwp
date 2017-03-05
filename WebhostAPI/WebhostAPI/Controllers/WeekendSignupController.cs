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
    /// Weekend Singups for Students.  Access this Weekend's activities and post your sign-ups.
    /// </summary>
    public class WeekendSignupController : ApiController
    {
        /// <summary>
        /// Get this weekend's list of activities.
        /// 
        /// </summary>
        /// <param name="listStudents">Show who has already signed up for this trip</param>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Students,Teachers,Administrators,Auditors")]
        [Route("api/weekend/activities")]
        public IHttpActionResult GetWeekendActivities([FromUri] bool listStudents = true)
        {
            DateTime friday = DateRange.ThisFriday;
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                if (db.Weekends.Where(w => w.StartDate.Equals(friday)).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new InvalidOperationException("No Weekend Information for this week.")));

                List<WeekendActivityInfo> info = new List<WeekendActivityInfo>();

                Weekend thisWeekend = db.Weekends.Where(w => w.StartDate.Equals(friday)).Single();
                foreach(WeekendActivity activity in thisWeekend.WeekendActivities.Where(act => !act.IsDeleted).OrderBy(act => act.DateAndTime).ToList())
                {
                    info.Add(new RequestHandlers.WeekendActivityInfo(activity.id, listStudents));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, info, "text/json"));
            }
        }

        /// <summary>
        /// Get info on a specific weekend activity.
        /// </summary>
        /// <param name="id">Activity Id (primary key in the WebhostDatabase).</param>
        /// <param name="listStudents">Show who has already signed up.</param>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Students,Teachers,Administrators,Auditors")]
        [Route("api/weekend/activities/{id:int}")]
        public IHttpActionResult GetWeekendActivity(int id, [FromUri] bool listStudents)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                WeekendActivity activity = db.WeekendActivities.Find(id);
                if (activity == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentOutOfRangeException(nameof(id))));

                WeekendActivityInfo info = null;
                try
                {
                    info = new WeekendActivityInfo(id);                    
                }
                catch(Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, info, "text/json"));
            }
        }

        /// <summary>
        /// Info about a student's signup for a particular activity.
        /// </summary>
        /// <param name="activity_id"></param>
        /// <param name="student_id"></param>
        /// <returns>StudentSignupInfo</returns>
        [WebhostAuthorize(Roles = "Students,Teachers,Administrators,Auditors")]
        [Route("api/weekend/activities/{activity_id:int}/signups/{student_id:int}")]
        public IHttpActionResult GetStudentSignupInfo(int activity_id, int student_id)
        {
            StudentSignupInfo info = null;
            try
            {
                info = new StudentSignupInfo(activity_id, student_id);
            }
            catch (Exception e)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, e));
            }

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, info, "text/json"));
        }


        /// <summary>
        /// Update or Create a new weekend signup for the authorized student.
        /// </summary>
        /// <param name="activity_id">the Activity ID of the activity you want to sign up for.</param>
        /// <param name="isRescend">Pass true if you are removing yourself from the signp.</param>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Students")]
        [Route("api/weekend/activities/{activity_id:int}/signups")]
        public IHttpActionResult PutSignupRequest(int activity_id, [FromUri] bool isRescend = false)
        {
            if(!DateRange.WeekendSignupsAreOpen)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, new MethodAccessException("Weekend Signups will be available at 11:30 on Friday.")));
            }

            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                WeekendActivity activity = db.WeekendActivities.Find(activity_id);
                if (activity == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new InvalidOperationException("Invalid Activity Id.")));

                StudentSignup signup = db.StudentSignups.Find(activity_id, id);

                if(signup == null) // New Signup!
                {
                    signup = new WebhostAPI.StudentSignup()
                    {
                        ActivityId = activity_id,
                        Attended = false,
                        IsBanned = false,
                        IsRescended = isRescend,
                        StudentId = id,
                        TimeStamp = DateTime.Now
                    };

                    db.StudentSignups.Add(signup);
                    db.SaveChanges();
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, new StudentSignupInfo(activity_id, id), "text/json"));
                }

                signup.IsRescended = isRescend;
                signup.TimeStamp = DateTime.Now;
                db.SaveChanges();
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new StudentSignupInfo(activity_id, id), "text/json"));
            }
        }

        /// <summary>
        /// Administrators and Teachers may override a student sign-up
        /// </summary>
        /// <param name="activity_id"></param>
        /// <param name="student_id"></param>
        /// <returns></returns>
        [WebhostAuthorize(Roles = "Administrators,Auditors,Teachers")]
        [Route("api/weekend/activities/{activity_id:int}/signups/{student_id:int}")]
        public IHttpActionResult PutOverrideSignup(int activity_id, int student_id, [FromBody] StudentSignupInfo info)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                WeekendActivity activity = db.WeekendActivities.Find(activity_id);
                if (activity == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new InvalidOperationException("Invalid Activity Id.")));

                Student student = db.Students.Find(student_id);
                if (student == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new InvalidOperationException("Invalid Student Id.")));

                StudentSignup signup = db.StudentSignups.Find(activity_id, student_id);
                if (signup == null) // Create a new one.
                {
                    signup = new WebhostAPI.StudentSignup()
                    {
                        ActivityId = activity_id,
                        StudentId = student_id,
                        IsBanned = info.IsBanned,
                        IsRescended = info.IsRescended,
                        Attended = false,
                        TimeStamp = DateTime.Now
                    };
                    db.StudentSignups.Add(signup);
                    db.SaveChanges();
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, new StudentSignupInfo(activity_id, student_id), "text/json"));
                }

                signup.IsBanned = info.IsBanned;
                signup.IsRescended = info.IsRescended;
                signup.TimeStamp = DateTime.Now;

                db.SaveChanges();
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new StudentSignupInfo(activity_id, student_id), "text/json"));
            }
        }
    }
}
