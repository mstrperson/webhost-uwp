using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using WebhostMySQLConnection.EVOPublishing;

namespace WebhostAPI.RequestHandlers
{
    /// <summary>
    /// Information about Student individual comments for a class.
    /// </summary>
    [DataContract]
    public class StudentCommentInfo
    {
        /// <summary>
        /// Grade information for this comment letter.
        /// </summary>
        [DataMember(IsRequired = true)]
        public GradeInfo Grades { get; set; }

        /// <summary>
        /// Webhost Database Id field.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int Id { get; set; }

        /// <summary>
        /// Comment.StudentId
        /// </summary>
        [DataMember(IsRequired = true)]
        public int StudentId { get; set; }

        /// <summary>
        /// Information about the student.
        /// when posting, only the Student ID number is required.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public StudentInfo Student { get; set; }

        /// <summary>
        /// HTML content of the individual comment in Base64String format for clean data transmission.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String HtmlContent { get; set; }

        /// <summary>
        /// If the data is stored in Rich Text Format.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public byte[] RtfContent { get; set; }

        /// <summary>
        /// Webhost Database ID number for the header paragraph that goes with this comment.
        /// </summary>
        [DataMember(IsRequired = true)]
        public int HeaderId { get; set; }

        /// <summary>
        /// Info on the Comment Header.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public CommentHeaderInfo Header { get; set; }

        /// <summary>
        /// Initialize a student comment.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeHeader"></param>
        public StudentCommentInfo(int id, bool includeHeader = false)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                StudentComment comment = db.StudentComments.Find(id);
                if (comment == null)
                    throw new ArgumentException("Invalid Student Comment.");

                Grades = new GradeInfo()
                {
                    EngagementGrade = comment.EngagementGrade.Name,
                    ExamGrade = comment.ExamGrade.Name,
                    FinalGrade = comment.FinalGrade.Name,
                    TrimesterGrade = comment.TermGrade.Name
                };

                Id = id;
                Student = new StudentInfo(comment.StudentID);
                HeaderId = comment.HeaderIndex;
                if (comment.RTF.Length <= 0)
                    HtmlContent = CommentLetter.ConvertToBase64String(comment.HTML);
                else
                    RtfContent = comment.RTF;

                if (includeHeader)
                    Header = new CommentHeaderInfo(HeaderId);
            }
        }
    }

    /// <summary>
    /// Info about grades for comments.
    /// </summary>
    [DataContract]
    public struct GradeInfo
    {
        /// <summary>
        /// The exam grade
        /// </summary>
        [DataMember(IsRequired = true)]
        public String ExamGrade { get; set; }

        /// <summary>
        /// Trimester Grade
        /// </summary>
        [DataMember(IsRequired = true)]
        public String TrimesterGrade { get; set; }

        /// <summary>
        /// Final Grade for the class.  Submit "Not Applicable" if needed.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String FinalGrade { get; set; }

        /// <summary>
        /// EMI grade for the class.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String EngagementGrade { get; set; }
    }
}