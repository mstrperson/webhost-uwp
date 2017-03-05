using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.IO.Compression;
using System.IO;
using WebhostAPI.Filters;
using WebhostAPI.Models;
using WebhostMySQLConnection.EVOPublishing;

namespace WebhostAPI.Controllers
{
    public class PublishedCommentsController : ApiController
    {

        [Route("api/self/{term}/{year:int}/comments")]
        [WebhostAuthorize(Roles = "Students,Teachers")]
        public IHttpActionResult GetMyComments(string term, int year)
        {
            if (!(new List<String>() { "Fall", "Winter", "Spring" }).Contains(term))
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, new ArgumentOutOfRangeException(nameof(term))));
            
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                Term theTerm = null;
                try
                {
                    theTerm = db.Terms.Where(t => t.Name.Equals(term) && t.StartDate.Year == year).Single();
                }
                catch(Exception e)
                {  // Invalid term.
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, e));
                }
                
                WebhostUserPrinciple principal = (WebhostUserPrinciple)ActionContext.RequestContext.Principal;
                int id = ((WebhostIdentity)principal.Identity).User.Id;

                byte[] zipData = null;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        List<int> CommentIds = new List<int>();

                        if (((WebhostIdentity)(principal.Identity)).User.IsTeacher())
                        {
                            Faculty teacher = db.Faculties.Find(((WebhostIdentity)(principal.Identity)).User.Id);
                            foreach (Section section in teacher.Sections.Where(s => s.CommentHeaders.Where(ch => ch.TermIndex == theTerm.id).Count() > 0).ToList())
                            {
                                CommentHeader header = section.CommentHeaders.Where(ch => ch.TermIndex == theTerm.id).Single();
                                CommentIds.AddRange(header.StudentComments.Select(c => c.id).ToList());
                            }
                        }
                        else
                        {
                            Student student = db.Students.Find(((WebhostIdentity)(principal.Identity)).User.Id);
                            CommentIds.AddRange(student.StudentComments.Where(com => com.CommentHeader.TermIndex == theTerm.id).Select(com => com.id).ToList());
                        }

                        foreach (int cid in CommentIds)
                        {
                            CommentLetter letter = new CommentLetter(cid);
                            byte[] pdfData = letter.Publish().Save();
                            ZipArchiveEntry entry = archive.CreateEntry(CommentLetter.EncodeSafeFileName(letter.Title) + ".pdf");
                            using (Stream stream = entry.Open())
                            {
                                stream.Write(pdfData, 0, pdfData.Length);
                            }
                        }
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    zipData = new byte[ms.Length];
                    for (long i = 0; i < ms.Length; i++)
                        zipData[i] = (byte)ms.ReadByte();
                }

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(zipData);
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("inline") { FileName = "comments.zip" };
                response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/octet-stream");

                return ResponseMessage(response);
            }
        }        
    }
}
