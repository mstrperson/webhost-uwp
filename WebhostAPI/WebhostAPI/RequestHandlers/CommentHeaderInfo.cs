using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using WebhostMySQLConnection.EVOPublishing;

namespace WebhostAPI.RequestHandlers
{
    /// <summary>
    /// Comment Header information packaged for JSON data transmission.
    /// </summary>
    [DataContract]
    public class CommentHeaderInfo
    {
        /// <summary>
        /// Webhost Database CommentHeader.id
        /// </summary>
        [DataMember(IsRequired = false)]
        public int Id { get; set; }

        /// <summary>
        /// CommentHeader.SectionIndex
        /// </summary>
        [DataMember(IsRequired = true)]
        public int SectionId { get; set; }

        /// <summary>
        /// CommentHeader.TermId
        /// </summary>
        [DataMember(IsRequired = true)]
        public int TermId { get; set; }

        /// <summary>
        /// Section title pulled from the SectionInfo object.
        /// e.g.  "[B-Block] AP Computer Science Principles"
        /// </summary>
        [DataMember(IsRequired = true)]
        public String SectionTitle { get; set; }

        /// <summary>
        /// What Term is this Header for?
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Term { get; set; }

        /// <summary>
        /// HTML content encoded into a Base64String for clean data transmission.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String HtmlContent { get; set; }

        /// <summary>
        /// If the comment is stored as Rich Text Format.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public byte[] RtfContnet { get; set; }

        /// <summary>
        /// List of Webhost Database Id numbers for student Comments
        /// </summary>
        [DataMember(IsRequired = false)]
        public List<int> StudentCommentIds { get; set; }

        /// <summary>
        /// List of all the data associated with student comments connected to this header.
        /// </summary>
        [DataMember(IsRequired = false)]
        public List<StudentCommentInfo> StudentComments { get; set; }


        /// <summary>
        /// Initialize Info object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeStudentCommentData"></param>
        public CommentHeaderInfo(int id, bool includeStudentCommentData = false)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                CommentHeader header = db.CommentHeaders.Find(id);
                if (header == null)
                    throw new ArgumentException("Invalid Comment Header Id.");

                Id = id;
                SectionId = header.SectionIndex;
                TermId = header.TermIndex;
                Term = string.Format("{0} {1}", header.Term.Name, header.Term.StartDate.Year);
                SectionTitle = String.Format("[{0}] {1}", header.Section.Block.LongName, header.Section.Course.Name);

                // Convert HTML to Base64String.
                if (header.RTF.Length <= 0)
                    HtmlContent = CommentLetter.ConvertToBase64String(header.HTML);
                else
                    RtfContnet = header.RTF;

                StudentCommentIds = header.StudentComments.Select(com => com.id).ToList();

                if(includeStudentCommentData)
                {
                    StudentComments = new List<StudentCommentInfo>();
                    foreach(int comId in StudentCommentIds)
                    {
                        StudentComments.Add(new StudentCommentInfo(comId));
                    }
                }
            }
        }
    }
}