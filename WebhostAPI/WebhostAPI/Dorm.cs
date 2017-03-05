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
    
    public partial class Dorm
    {
        public Dorm()
        {
            this.Faculties = new HashSet<Faculty>();
            this.Students = new HashSet<Student>();
        }
    
        public int id { get; set; }
        public int AcademicYearId { get; set; }
        public int DormHeadId { get; set; }
        public string Name { get; set; }
    
        public virtual AcademicYear AcademicYear { get; set; }
        public virtual Faculty Faculty { get; set; }
        public virtual ICollection<Faculty> Faculties { get; set; }
        public virtual ICollection<Student> Students { get; set; }
    }
}
