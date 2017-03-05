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
    
    public partial class StudentComment
    {
        public int id { get; set; }
        public int HeaderIndex { get; set; }
        public int StudentID { get; set; }
        public string HTML { get; set; }
        public int ExamGradeID { get; set; }
        public int TermGradeID { get; set; }
        public int EffortGradeID { get; set; }
        public int FinalGradeID { get; set; }
        public byte[] RTF { get; set; }
    
        public virtual CommentHeader CommentHeader { get; set; }
        public virtual GradeTableEntry EngagementGrade { get; set; }
        public virtual GradeTableEntry ExamGrade { get; set; }
        public virtual GradeTableEntry FinalGrade { get; set; }
        public virtual GradeTableEntry TermGrade { get; set; }
        public virtual Student Student { get; set; }
    }
}
