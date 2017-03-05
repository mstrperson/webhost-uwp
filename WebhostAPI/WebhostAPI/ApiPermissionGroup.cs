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
    
    public partial class ApiPermissionGroup
    {
        public ApiPermissionGroup()
        {
            this.Faculties = new HashSet<Faculty>();
            this.ApiPermissions = new HashSet<ApiPermission>();
        }
    
        public int id { get; set; }
        public string Name { get; set; }
        public bool CanRequestMembership { get; set; }
    
        public virtual ICollection<Faculty> Faculties { get; set; }
        public virtual ICollection<ApiPermission> ApiPermissions { get; set; }
    }
}