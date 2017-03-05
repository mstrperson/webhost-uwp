using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WebhostUniversalWindows.Attendance
{
    /// <summary>
    /// Info about a given Attendance Marking in the Webhost Database.
    /// </summary>
    [DataContract]
    public class AttendanceInfo
    {
        /// <summary>
        /// Webhost Database Id field for this attendance marking.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int Id { get; set; }

        /// <summary>
        /// Student Id for this attendance marking.
        /// </summary>
        [DataMember(IsRequired = true)]
        public int StudentId { get; set; }

        /// <summary>
        /// Section Id which this attendance marking is for.
        /// </summary>
        [DataMember(IsRequired = true)]
        public int SectionId { get; set; }

        /// <summary>
        /// Present, Late, Cut or Excused.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Marking { get; set; }

        /// <summary>
        /// The Date that this attendance is for.
        /// </summary>
        [DataMember(IsRequired = true)]
        public long DateBinary { get; set; }

        /// <summary>
        /// DateTime object for saving to the database.
        /// </summary>
        [IgnoreDataMember]
        public DateTime Date
        {
            set
            {
                DateBinary = value.ToBinary();
            }
            get
            {
                return DateTime.FromBinary(DateBinary);
            }
        }

        /// <summary>
        /// Additional Notes about this entry.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String Notes { get; set; }

        /// <summary>
        /// Who entered this attendance
        /// </summary>
        [DataMember(IsRequired = false)]
        public TeacherInfo EnteredBy { get; set; }

        /// <summary>
        /// <see cref="StudentInfo"/> for whom this attendance was submitted.
        /// </summary>
        [DataMember(IsRequired = false)]
        public StudentInfo Student { get; set; }

        /// <summary>
        /// <see cref="SectionInfo"/> for the Section that this was submitted for.
        /// </summary>
        [DataMember(IsRequired = false)]
        public SectionInfo Section { get; set; }
    }
}
