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
using WebhostMySQLConnection.EVOPublishing;

namespace WebhostAPI.Controllers
{
    /// <summary>
    /// Comment Letters Access for Teachers.
    /// </summary>
    public class CommentsController : ApiController
    {
        #region Comment Headers
        
        /// <summary>
        /// Get current user's comment headers for the current term.
        /// 
        /// </summary>
        /// <param name="includeComments"></param>
        /// <returns></returns>
        [Route("api/self/comment_headers")]
        [WebhostAuthorize(Roles = "Teachers")]
        public IHttpActionResult GetCurrentCommentHeaders([FromUri] bool includeComments = false)
        {
            List<CommentHeaderInfo> commentHeaders = new List<CommentHeaderInfo>();
            using (WebhostEntities db = new WebhostEntities())
            {
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                int termId = DateRange.GetCurrentOrLastTerm();
                List<int> headers = new List<int>();
                foreach (Section section in self.Sections.Where(s => s.Terms.Where(t => t.id == termId).Count() > 0).ToList())
                {
                    if (section.CommentHeaders.Where(h => h.TermIndex == termId).Count() > 0)
                    {
                        headers.Add(section.CommentHeaders.Where(h => h.TermIndex == termId).Single().id);
                    }
                }

                foreach (int headerId in headers)
                {
                    commentHeaders.Add(new RequestHandlers.CommentHeaderInfo(headerId, includeComments));
                }
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, commentHeaders, "text/json");
            return ResponseMessage(response);
        }

        /// <summary>
        /// Get the current comment header for a section that belongs to you.
        /// </summary>
        /// <param name="section_id"></param>
        /// <returns></returns>
        [Route("api/self/sections/{section_id:int}/comment_header")]
        [WebhostAuthorize(Roles = "Teachers")]
        public IHttpActionResult GetCommentHeaderForSection(int section_id)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                if (!self.Sections.Contains(section))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, new UnauthorizedAccessException("You are not a teacher for this class.")));

                int termId = DateRange.GetCurrentOrLastTerm();

                if (section.CommentHeaders.Where(h => h.TermIndex == termId).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NoContent, new ArgumentException("No Comment Header for this Term is saved.")));

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new CommentHeaderInfo(section.CommentHeaders.Where(h => h.TermIndex == termId).Single().id), "text/json"));
            }
        }

        /// <summary>
        /// Create a new comment header for a section that belongs to you.
        /// Responds with Conflict if there is a database error (possibly multiple paralel requests)
        /// Responds with NoContent if the section already has a header for this term.
        /// 
        /// Expects plaintext Base64 encoded HTML as the content.
        /// </summary>
        /// <param name="section_id"></param>
        /// <param name="content">Base 64 encoded HTML content.</param>
        /// <returns></returns>
        [Route("api/self/sections/{section_id:int}/comment_header")]
        [WebhostAuthorize(Roles = "Teachers")]
        [HttpPost]
        public IHttpActionResult CreateNewCommentHeader(int section_id, [FromBody] String content)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                if (!self.Sections.Contains(section))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, new UnauthorizedAccessException("You are not a teacher for this class.")));

                int termId = DateRange.GetCurrentOrLastTerm();

                if (section.CommentHeaders.Where(h => h.TermIndex == termId).Count() > 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Header Already Exists.  Use PUT to Update.")));

                CommentHeader newHeader = new CommentHeader()
                {
                    id = db.CommentHeaders.OrderBy(h => h.id).ToList().Last().id + 1,
                    SectionIndex = section_id,
                    TermIndex = termId,
                    HTML = ""
                };

                content = CommentLetter.ConvertFromBase64String(content);

                newHeader.HTML = CommentLetter.CleanTags(content);

                try
                {
                    db.CommentHeaders.Add(newHeader);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, new CommentHeaderInfo(newHeader.id), "text/json"));
            }
        }

        /// <summary>
        /// Update the content of the comment header for a section that belongs to you.
        /// Responds with Conflict if there is a database error (possibly multiple paralel requests)
        /// Responds with NoContent if the section does not have a header for this term.
        /// 
        /// Expects plaintext Base64 encoded HTML as the content.
        /// </summary>
        /// <param name="section_id"></param>
        /// <param name="content">Base 64 encoded HTML content.</param>
        /// <param name="format"></param>
        /// <returns></returns>
        [Route("api/self/sections/{section_id:int}/comment_header")]
        [WebhostAuthorize(Roles = "Teachers")]
        public IHttpActionResult PutUpdatedCommentHeader(int section_id, [FromBody] String content, [FromUri] String format = "rtf")
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                if (!self.Sections.Contains(section))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, new UnauthorizedAccessException("You are not a teacher for this class.")));

                int termId = DateRange.GetCurrentOrLastTerm();

                CommentHeader header;

                if (section.CommentHeaders.Where(h => h.TermIndex == termId).Count() <= 0)
                {
                    header = new CommentHeader()
                    {
                        id = db.CommentHeaders.OrderBy(h => h.id).ToList().Last().id + 1,
                        HTML = "",
                        SectionIndex = section_id,
                        TermIndex = termId
                    };
                    db.CommentHeaders.Add(header);
                }
                else
                {
                    header = section.CommentHeaders.Where(h => h.TermIndex == termId).Single();
                }

                content = CommentLetter.ConvertFromBase64String(content);

                if (format.Equals("html"))
                {
                    header.HTML = CommentLetter.ConvertFromBase64String(content);

                }
                else
                    header.RTF = Convert.FromBase64String(content);

                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new CommentHeaderInfo(header.id), "text/json"));
            }
        }

        #endregion  // Comment Headers

        #region Student Comments.

        /// <summary>
        /// Get the student comment for a section.
        /// </summary>
        /// <param name="section_id"></param>
        /// <param name="student_id"></param>
        /// <returns></returns>
        [Route("api/self/section/{section_id:int}/students/{student_id:int}/comment")]
        [Authorize(Roles = "Teachers")]
        public IHttpActionResult GetStudentComment(int section_id, int student_id)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                if (section.Students.Where(s => s.ID == student_id).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Student Id.")));

                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                if (!self.Sections.Contains(section))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, new UnauthorizedAccessException("You are not a teacher for this class.")));

                int termId = DateRange.GetCurrentOrLastTerm();

                if (section.CommentHeaders.Where(h => h.TermIndex == termId).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NoContent, new ArgumentException("No Comment Header for this Term is saved.")));

                CommentHeader header = section.CommentHeaders.Where(h => h.TermIndex == termId).Single();
                
                if(header.StudentComments.Where(comment => comment.StudentID == student_id).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NoContent, new ArgumentException("No Student Comment saved for this student.")));


                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new StudentCommentInfo(header.StudentComments.Where(com => com.StudentID == student_id).Single().id)));
            }            
        }

        /// <summary>
        /// Create a new student comment.
        /// </summary>
        /// <param name="section_id"></param>
        /// <param name="student_id"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        [Route("api/self/section/{section_id:int}/students/{student_id:int}/comment")]
        [Authorize(Roles = "Teachers")]
        [HttpPost]
        public IHttpActionResult CreateStudentComment(int section_id, int student_id, [FromBody] StudentCommentInfo info)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                if (section.Students.Where(s => s.ID == student_id).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Student Id.")));

                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                if (!self.Sections.Contains(section))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, new UnauthorizedAccessException("You are not a teacher for this class.")));

                int termId = DateRange.GetCurrentOrLastTerm();

                if (section.CommentHeaders.Where(h => h.TermIndex == termId).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NoContent, new ArgumentException("No Comment Header for this Term is saved.")));

                CommentHeader header = section.CommentHeaders.Where(h => h.TermIndex == termId).Single();

                if (header.StudentComments.Where(com => com.StudentID == student_id).Count() > 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, new ArgumentException("Comment Already Exists.")));

                StudentComment comment = new WebhostAPI.StudentComment()
                {
                    StudentID = student_id,
                    id = db.StudentComments.OrderBy(c => c.id).ToList().Last().id + 1,
                    HeaderIndex = header.id
                };

                Dictionary<String, int> GradeEntries = CommentLetter.GetGradeTableEntryData();

                comment.EffortGradeID = GradeEntries[info.Grades.EngagementGrade];
                comment.ExamGradeID = GradeEntries[info.Grades.ExamGrade];
                comment.TermGradeID = GradeEntries[info.Grades.TrimesterGrade];
                comment.FinalGradeID = GradeEntries[info.Grades.FinalGrade];

                comment.HTML = CommentLetter.ConvertFromBase64String(info.HtmlContent);

                try
                {
                    db.StudentComments.Add(comment);
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new StudentCommentInfo(header.StudentComments.Where(com => com.StudentID == student_id).Single().id)));
            }
        }

        /// <summary>
        /// Update an existing student comment.
        /// </summary>
        /// <param name="section_id"></param>
        /// <param name="student_id"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        [Route("api/self/section/{section_id:int}/students/{student_id:int}/comment")]
        [Authorize(Roles = "Teachers")]
        public IHttpActionResult PutUpdatedStudentComment(int section_id, int student_id, [FromBody] StudentCommentInfo info, [FromUri] String format = "rtf")
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(section_id);
                if (section == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Section Id.")));

                if (section.Students.Where(s => s.ID == student_id).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new ArgumentException("Invalid Student Id.")));

                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                Faculty self = db.Faculties.Find(id);

                if (!self.Sections.Contains(section))
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, new UnauthorizedAccessException("You are not a teacher for this class.")));

                int termId = DateRange.GetCurrentOrLastTerm();

                if (section.CommentHeaders.Where(h => h.TermIndex == termId).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NoContent, new ArgumentException("No Comment Header for this Term is saved.")));

                CommentHeader header = section.CommentHeaders.Where(h => h.TermIndex == termId).Single();

                if (header.StudentComments.Where(com => com.StudentID == student_id).Count() <= 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, new ArgumentException("No Comment for this student saved.")));

                StudentComment comment = header.StudentComments.Where(com => com.StudentID == student_id).Single();
                
                Dictionary<String, int> GradeEntries = CommentLetter.GetGradeTableEntryData();

                comment.EffortGradeID = GradeEntries[info.Grades.EngagementGrade];
                comment.ExamGradeID = GradeEntries[info.Grades.ExamGrade];
                comment.TermGradeID = GradeEntries[info.Grades.TrimesterGrade];
                comment.FinalGradeID = GradeEntries[info.Grades.FinalGrade];

                if (format.Equals("html"))
                {
                    comment.HTML = CommentLetter.ConvertFromBase64String(info.HtmlContent);                    
                }
                else
                {

                    comment.RTF = info.RtfContent;
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new StudentCommentInfo(header.StudentComments.Where(com => com.StudentID == student_id).Single().id)));
            }
        }
        #endregion

        #region Archive

        /// <summary>
        /// Get Student Comments for a particular term.
        /// </summary>
        /// <param name="term_id"></param>
        /// <param name="student_id"></param>
        /// <returns></returns>
        [Route("api/comment_archive/term/{term_id:int}/student/{student_id:int}")]
        [WebhostAuthorize(Roles = "Teachers,Administrators,Auditors")]
        public IHttpActionResult GetCommentsForTerm(int term_id, int student_id)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                Term term = db.Terms.Find(term_id);
                if (term == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new HttpError("Invalid Term Id")));

                Student student = db.Students.Find(student_id);
                if (student == null)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, new HttpError("Invalid Student Id")));

                List<int> student_comments = student.StudentComments.Where(com => com.CommentHeader.TermIndex == term_id).Select(com => com.id).ToList();

                List<StudentCommentInfo> output = new List<StudentCommentInfo>();
                
                foreach(int id in student_comments)
                {
                    output.Add(new RequestHandlers.StudentCommentInfo(id, true));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, output));
            }
        }
        #endregion
    }
}