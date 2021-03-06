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
    
    public partial class Faculty
    {
        public Faculty()
        {
            this.APRequests = new HashSet<APRequest>();
            this.APRequests1 = new HashSet<APRequest>();
            this.AttendanceMarkings = new HashSet<AttendanceMarking>();
            this.DepartmentsHeaded = new HashSet<Department>();
            this.DormsHeaded = new HashSet<Dorm>();
            this.DutyTeamsAOD = new HashSet<DutyTeam>();
            this.DutyTeamsLead = new HashSet<DutyTeam>();
            this.TimeStampedSignatures = new HashSet<TimeStampedSignature>();
            this.Students = new HashSet<Student>();
            this.GoogleCalendars = new HashSet<GoogleCalendar>();
            this.Dorms = new HashSet<Dorm>();
            this.DutyTeams = new HashSet<DutyTeam>();
            this.WeekendActivities = new HashSet<WeekendActivity>();
            this.ApiPermissionGroups = new HashSet<ApiPermissionGroup>();
            this.Fingerprints = new HashSet<Fingerprint>();
            this.Permissions = new HashSet<Permission>();
            this.Preferences = new HashSet<Preference>();
            this.Sections = new HashSet<Section>();
            this.WeekendDuties = new HashSet<WeekendDuty>();
        }
    
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public bool isActive { get; set; }
        public string PhoneNumber { get; set; }
        public byte[] SignatureData { get; set; }
        public int SchoologyRoleId { get; set; }
        public int CurrentFingerprintId { get; set; }
        public int SchoologyId { get; set; }
    
        public virtual ICollection<APRequest> APRequests { get; set; }
        public virtual ICollection<APRequest> APRequests1 { get; set; }
        public virtual ICollection<AttendanceMarking> AttendanceMarkings { get; set; }
        public virtual ICollection<Department> DepartmentsHeaded { get; set; }
        public virtual ICollection<Dorm> DormsHeaded { get; set; }
        public virtual ICollection<DutyTeam> DutyTeamsAOD { get; set; }
        public virtual ICollection<DutyTeam> DutyTeamsLead { get; set; }
        public virtual Fingerprint CurrentFingerprint { get; set; }
        public virtual ICollection<TimeStampedSignature> TimeStampedSignatures { get; set; }
        public virtual ICollection<Student> Students { get; set; }
        public virtual ICollection<GoogleCalendar> GoogleCalendars { get; set; }
        public virtual ICollection<Dorm> Dorms { get; set; }
        public virtual ICollection<DutyTeam> DutyTeams { get; set; }
        public virtual ICollection<WeekendActivity> WeekendActivities { get; set; }
        public virtual ICollection<ApiPermissionGroup> ApiPermissionGroups { get; set; }
        public virtual ICollection<Fingerprint> Fingerprints { get; set; }
        public virtual ICollection<Permission> Permissions { get; set; }
        public virtual ICollection<Preference> Preferences { get; set; }
        public virtual ICollection<Section> Sections { get; set; }
        public virtual ICollection<WeekendDuty> WeekendDuties { get; set; }
    }
}
