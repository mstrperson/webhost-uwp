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
    
    public partial class DutyTeam
    {
        public DutyTeam()
        {
            this.Weekends = new HashSet<Weekend>();
            this.Faculties = new HashSet<Faculty>();
        }
    
        public int id { get; set; }
        public int AcademicYear { get; set; }
        public string Name { get; set; }
        public int DTL { get; set; }
        public int AOD { get; set; }
    
        public virtual AcademicYear AcademicYear1 { get; set; }
        public virtual Faculty Faculty { get; set; }
        public virtual Faculty Faculty1 { get; set; }
        public virtual ICollection<Weekend> Weekends { get; set; }
        public virtual ICollection<Faculty> Faculties { get; set; }
    }
}
