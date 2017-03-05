using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebhostUniversalWindows.Comments
{
    /// <summary>
    /// Information about Student individual comments for a class.
    /// </summary>
    [DataContract]
    public class CommentInfo
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
        /// HTML content encoded into a Base64String for clean data transmission.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String HtmlContent { get; set; }

        /// <summary>
        /// 
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

        public override string ToString()
        {
            String json = JsonConvert.SerializeObject(this, typeof(CommentInfo), new JsonSerializerSettings());
            return json;
        }

        public static CommentInfo FromJson(string data)
        {
            CommentInfo info = (CommentInfo)JsonConvert.DeserializeObject(data, typeof(CommentInfo), new JsonSerializerSettings());
            return info;
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
