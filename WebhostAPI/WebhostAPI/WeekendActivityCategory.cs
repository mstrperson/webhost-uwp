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
    
    public partial class WeekendActivityCategory
    {
        public WeekendActivityCategory()
        {
            this.WeekendActivities = new HashSet<WeekendActivity>();
        }
    
        public int id { get; set; }
        public string CategoryName { get; set; }
        public int MaxPerWeekend { get; set; }
        public bool IsOffCampus { get; set; }
        public bool IsMandatory { get; set; }
    
        public virtual ICollection<WeekendActivity> WeekendActivities { get; set; }
    }
}
