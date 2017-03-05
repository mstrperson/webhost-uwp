//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebhostAPI
{
    using System;
    using System.Collections.Generic;
    
    public partial class AttendanceMarking
    {
        public int id { get; set; }
        public int SectionIndex { get; set; }
        public int StudentID { get; set; }
        public System.DateTime AttendanceDate { get; set; }
        public string Notes { get; set; }
        public int MarkingIndex { get; set; }
        public System.DateTime SubmissionTime { get; set; }
        public int SubmittedBy { get; set; }
    
        public virtual GradeTableEntry GradeTableEntry { get; set; }
        public virtual Section Section { get; set; }
        public virtual Student Student { get; set; }
        public virtual Faculty Faculty { get; set; }
    }
}
