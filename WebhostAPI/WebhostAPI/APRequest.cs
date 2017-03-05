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
    
    public partial class APRequest
    {
        public APRequest()
        {
            this.Sections = new HashSet<Section>();
        }
    
        public int id { get; set; }
        public int CourseRequestId { get; set; }
        public int ApprovalId { get; set; }
        public int TeacherSignedBy { get; set; }
        public int DeptHeadSignedBy { get; set; }
    
        public virtual CourseRequest CourseRequest { get; set; }
        public virtual Faculty Faculty { get; set; }
        public virtual GradeTableEntry GradeTableEntry { get; set; }
        public virtual Faculty Faculty1 { get; set; }
        public virtual ICollection<Section> Sections { get; set; }
    }
}
