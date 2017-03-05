using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WebhostAPI.RequestHandlers
{
    /// <summary>
    /// Student User Information.
    /// </summary>
    [DataContract]
    public class StudentInfo : WebhostUserInfo
    {
        /// <summary>
        /// Student's Advisor.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public TeacherInfo Advisor { get; set; }

        /// <summary>
        /// Student's Graduation Year.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int GraduationYear { get; set; }
        
        /// <summary>
        /// Initialize StudentInfo given their Authorization Fingerprint.
        /// </summary>
        /// <param name="fingerprint"></param>
        /// <exception cref="AccessViolationException">Thrown when the provided fingerprint is invalid.</exception>
        public StudentInfo(byte[] fingerprint)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                Student student = null;
                foreach(Student st in db.Students.Where(s => s.isActive && s.Fingerprints.Count > 0).ToList())
                {
                    foreach(Fingerprint fp in st.Fingerprints.ToList())
                    {
                        if(fp.Value.SequenceEqual(fingerprint))
                        {
                            student = st;
                            break;
                        }
                    }
                }

                if (student == null) throw new AccessViolationException("Unrecognized Fingerprint.");
                
                Email = String.Format("{0}@dublinschool.org", student.UserName);
                FirstName = student.FirstName;
                LastName = student.LastName;
                Id = student.ID;
                GraduationYear = student.GraduationYear;
                Advisor = new TeacherInfo(student.AdvisorID, true);
                Groups = new List<string>();
                this.Groups.Add("Students");
            }
        }

        /// <summary>
        /// Get Student Info given the student Id.
        /// </summary>
        /// <param name="studentId">Valid Student Id</param>
        /// <param name="shortVersion">Omit some data.</param>
        /// <exception cref="ArgumentException">Thrown when the student Id is Invalid.</exception>
        public StudentInfo(int studentId, bool shortVersion = true)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                Student student = db.Students.Find(studentId);
                if (student == null) throw new ArgumentException("Invalid Student Id");
                UserName = student.UserName;
                if(!shortVersion)
                {
                    Email = String.Format("{0}@dublinschool.org", student.UserName);
                    Advisor = new TeacherInfo(student.AdvisorID, true);
                }
                FirstName = student.FirstName;
                LastName = student.LastName;
                Id = student.ID;
                GraduationYear = student.GraduationYear;
                Groups = new List<string>();
                this.Groups.Add("Students");
            }
        }
    }
}