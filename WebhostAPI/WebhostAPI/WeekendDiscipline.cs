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
    
    public partial class WeekendDiscipline
    {
        public int WeekendId { get; set; }
        public int StudentId { get; set; }
        public bool Campused { get; set; }
        public int DetentionHours { get; set; }
    
        public virtual Student Student { get; set; }
        public virtual Weekend Weekend { get; set; }
    }
}