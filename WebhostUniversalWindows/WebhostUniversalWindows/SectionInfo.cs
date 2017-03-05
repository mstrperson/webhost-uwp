using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WebhostUniversalWindows
{
    [DataContract]
    public partial class SectionInfo
    {
        [DataMember(IsRequired = true)]
        public int Id { get; set; }

        [DataMember(IsRequired = true)]
        public String Block { get; set; }

        [DataMember(IsRequired = true)]
        public String Course { get; set; }

        [DataMember(IsRequired = true)]
        public int CourseId { get; set; }

        [DataMember(IsRequired = true)]
        public List<int> StudentIds { get; set; }

        [DataMember(IsRequired = false)]
        public List<StudentInfo> Students { get; set; }

        [DataMember(IsRequired = true)]
        public List<int> TeacherIds { get; set; }

        [DataMember(IsRequired = false)]
        public List<TeacherInfo> Teachers { get; set; }

        
    }
}
