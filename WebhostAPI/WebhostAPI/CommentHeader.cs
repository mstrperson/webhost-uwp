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
    
    public partial class CommentHeader
    {
        public CommentHeader()
        {
            this.StudentComments = new HashSet<StudentComment>();
        }
    
        public int id { get; set; }
        public int SectionIndex { get; set; }
        public int TermIndex { get; set; }
        public string HTML { get; set; }
        public byte[] RTF { get; set; }
    
        public virtual ICollection<StudentComment> StudentComments { get; set; }
        public virtual Term Term { get; set; }
        public virtual Section Section { get; set; }
    }
}
