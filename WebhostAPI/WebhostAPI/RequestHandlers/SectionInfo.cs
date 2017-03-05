using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WebhostAPI.RequestHandlers
{
    /// <summary>
    /// Information about Sections that is relevant to be passed over the API.
    /// </summary>
    [DataContract]
    public class SectionInfo
    {
        /// <summary>
        /// Webhost database Id field.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int Id { get; set; }

        /// <summary>
        /// Section.Block.LongName
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Block { get; set; }

        /// <summary>
        /// Section.Course.Name
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Course { get; set; }
        
        /// <summary>
        /// Section.CourseIndex
        /// </summary>
        [DataMember(IsRequired = true)]
        public int CourseId { get; set; }

        /// <summary>
        /// List of Student Id's of students enrolled in this class.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<int> StudentIds { get; set; }

        /// <summary>
        /// (Optional) List of detailed student information about members
        /// of this class.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<StudentInfo> Students { get; set; }

        /// <summary>
        /// List of employee Ids for teachers of this class.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<int> TeacherIds { get; set; }


        /// <summary>
        /// (Optional) List of detailed employee information about
        /// teachers of this class.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<TeacherInfo> Teachers { get; set; }
        
        /// <summary>
        /// Initialize a SectionInfo object given the Section.Index
        /// </summary>
        /// <param name="id">Section.Index</param>
        /// <param name="listDetailedRosters">Include detailed student and teacher info?</param>
        public SectionInfo(int id, bool listDetailedRosters = false)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(id);
                if (section == null)
                    throw new ArgumentException("Invalid Section Id.");

                Id = id;
                CourseId = section.CourseIndex;
                Course = section.Course.Name;
                Block = section.Block.LongName;
                StudentIds = section.Students.Select(s => s.ID).ToList();
                TeacherIds = section.Teachers.Select(t => t.ID).ToList();
                if(listDetailedRosters)
                {
                    Students = new List<StudentInfo>();
                    Teachers = new List<TeacherInfo>();
                    foreach(int sid in StudentIds)
                    {
                        Students.Add(new StudentInfo(sid));
                    }

                    foreach(int tid in TeacherIds)
                    {
                        Teachers.Add(new TeacherInfo(tid));
                    }
                }
            }
        }
    }
}