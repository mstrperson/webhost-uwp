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
    
    public partial class Weekend
    {
        public Weekend()
        {
            this.WeekendDuties = new HashSet<WeekendDuty>();
            this.WeekendActivities = new HashSet<WeekendActivity>();
            this.WeekendDisciplines = new HashSet<WeekendDiscipline>();
            this.Students = new HashSet<Student>();
        }
    
        public int id { get; set; }
        public int DutyTeamIndex { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
        public string Notes { get; set; }
    
        public virtual DutyTeam DutyTeam { get; set; }
        public virtual ICollection<WeekendDuty> WeekendDuties { get; set; }
        public virtual ICollection<WeekendActivity> WeekendActivities { get; set; }
        public virtual ICollection<WeekendDiscipline> WeekendDisciplines { get; set; }
        public virtual ICollection<Student> Students { get; set; }
    }
}
