using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WebhostMySQLConnection;

namespace WebhostAPI.RequestHandlers
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

        /// <summary>
        /// Generate this given the <see cref="AttendanceMarking.id"/>.
        /// </summary>
        /// <param name="id"><see cref="AttendanceMarking.id"/></param>
        /// <param name="fullDetails">Fills in all of the non-mandatory fields if true.  Otherwise, only provides the mandatory data.</param>
        public AttendanceInfo(int id, bool fullDetails = false)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                AttendanceMarking marking = db.AttendanceMarkings.Find(id);
                Id = id;
                StudentId = marking.StudentID;
                SectionId = marking.SectionIndex;
                Marking = marking.GradeTableEntry.Name;
                Date = marking.AttendanceDate;
                Notes = marking.Notes;
                EnteredBy = new TeacherInfo(marking.SubmittedBy);
                if(fullDetails)
                {
                    Student = new StudentInfo(StudentId);
                    Section = new SectionInfo(SectionId);
                }
            }
        }

        /// <summary>
        /// Look up the <see cref="GradeTableEntry.id"/> given the text value of <see cref="GradeTableEntry.Name"/>.
        /// </summary>
        /// <param name="name"><see cref="GradeTableEntry.Name"/></param>
        /// <returns><see cref="GradeTableEntry.id"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided Name is invalid.</exception>
        public static int LookUpAttendanceMarking(String name)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                int year = DateRange.GetCurrentAcademicYear();
                GradeTable table = db.GradeTables.Where(t => t.AcademicYearID == year && t.Name.Equals("Attendance")).Single();

                if (table.GradeTableEntries.Where(g => g.Name.Equals(name)).Count() <= 0)
                    throw new ArgumentOutOfRangeException(nameof(name));

                return table.GradeTableEntries.Where(g => g.Name.Equals(name)).Single().id;
            }
        }
    }
}