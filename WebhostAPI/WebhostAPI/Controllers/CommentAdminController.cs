using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebhostAPI.Filters;
using WebhostAPI.RequestHandlers;
using WebhostMySQLConnection.EVOPublishing;

namespace WebhostAPI.Controllers
{
    /// <summary>
    /// Comment Controlls for administrators.
    /// </summary>
    public class CommentAdminController : ApiController
    {
        /// <summary>
        /// Get comment header for a class for a particular term.
        /// </summary>
        /// <param name="section_id">Section.Index</param>
        /// <param name="term_id">Term.Index</param>
        /// <param name="includeComments">Include all student comments?</param>
        /// <returns></returns>
        [Route("api/section/{section_id:int}/comment_header/{term_id:int}")]
        [WebhostAuthorize(Roles = "Administrators,Auditors")]
        public IHttpActionResult GetCommentHeaderForTerm(int section_id, int term_id, [FromUri] bool includeComments = false)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                int header = -1;
                try
                {
                    header = db.CommentHeaders.Where(h => h.SectionIndex == section_id && h.TermIndex == term_id).Single().id;
                }
                catch (Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new CommentHeaderInfo(header, includeComments)));
            }
        }

        /// <summary>
        /// Forcibly update a teacher's comment header.
        /// Expects plaintext Base64 encoded HTML as the body of the request.
        /// </summary>
        /// <param name="section_id">Section.Index</param>
        /// <param name="term_id">Term.Index</param>
        /// <param name="content">Base 64 encoded content.</param>
        /// <returns></returns>
        [Route("api/section/{section_id:int}/comment_header/{term_id:int}")]
        [WebhostAuthorize(Roles = "Administrators,Auditors")]
        public IHttpActionResult PutUpdatedCommentHeader(int section_id, int term_id, [FromBody] String content)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                int header = -1;
                try
                {
                    header = db.CommentHeaders.Where(h => h.SectionIndex == section_id && h.TermIndex == term_id).Single().id;
                }
                catch (Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, e));
                }

                CommentHeader headerParagraph = db.CommentHeaders.Find(header);
                
                content = CommentLetter.ConvertFromBase64String(content);

                headerParagraph.HTML = CommentLetter.CleanTags(content);

                try
                {
                    db.SaveChanges();
                }
                catch(Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new CommentHeaderInfo(header)));
            }
        }

        /// <summary>
        /// Create a new comment header for a teacher.
        /// </summary>
        /// <param name="section_id"></param>
        /// <param name="term_id"></param>
        /// <returns></returns>
        [Route("api/section/{section_id:int}/comment_header/{term_id:int}")]
        [WebhostAuthorize(Roles = "Administrators,Auditors")]
        public async Task<IHttpActionResult> PostNewCommentHeader(int section_id, int term_id)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                if (db.CommentHeaders.Where(h => h.SectionIndex == section_id && h.TermIndex == term_id).Count() > 0)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, new ArgumentException("Comment Header already Exists.")));
                
                CommentHeader headerParagraph = new CommentHeader()
                {
                    id = db.CommentHeaders.OrderBy(h => h.id).ToList().Last().id + 1,
                    SectionIndex = section_id,
                    TermIndex = term_id
                };
                
                String content = await Request.Content.ReadAsStringAsync();

                content = CommentLetter.ConvertFromBase64String(content);

                headerParagraph.HTML = CommentLetter.CleanTags(content);

                db.CommentHeaders.Add(headerParagraph);

                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Conflict, e));
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new CommentHeaderInfo(headerParagraph.id)));
            }
        }

        /// <summary>
        /// Get Pdf Data for a comment letter.
        /// Responds with application/pdf formatted raw data.
        /// </summary>
        /// <param name="id">StudentComment.id</param>
        /// <returns></returns>
        [Route("api/archive/comments/{id:int}")]
        [WebhostAuthorize(Roles = "Teachers,Administrators,Auditors")]
        public IHttpActionResult GetPdfComment(int id)
        {
            CommentLetter comment = new CommentLetter(id);
            byte[] pdfData = comment.Publish().Save();

            HttpResponseMessage response =  Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(pdfData);
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline") { FileName = "comment.pdf" };
            response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/pdf");

            return ResponseMessage(response);
        }

    }
}
