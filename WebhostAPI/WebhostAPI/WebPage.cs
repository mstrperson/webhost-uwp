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
    
    public partial class WebPage
    {
        public WebPage()
        {
            this.Permissions = new HashSet<Permission>();
            this.WebPageTags = new HashSet<WebPageTag>();
        }
    
        public int id { get; set; }
        public string RawURL { get; set; }
        public string Name { get; set; }
    
        public virtual ICollection<Permission> Permissions { get; set; }
        public virtual ICollection<WebPageTag> WebPageTags { get; set; }
    }
}