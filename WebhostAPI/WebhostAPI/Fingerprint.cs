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
    
    public partial class Fingerprint
    {
        public Fingerprint()
        {
            this.FacultyOwners = new HashSet<Faculty>();
            this.AvailableFaculty = new HashSet<Faculty>();
            this.StudentOwners = new HashSet<Student>();
        }
    
        public int Id { get; set; }
        public byte[] Value { get; set; }
        public bool IsDeactivated { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsInUse { get; set; }
    
        public virtual ICollection<Faculty> FacultyOwners { get; set; }
        public virtual ICollection<Faculty> AvailableFaculty { get; set; }
        public virtual ICollection<Student> StudentOwners { get; set; }
    }
}