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
    
    public partial class ApiRequestHeader
    {
        public int id { get; set; }
        public int ApiId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    
        public virtual ExternalApi ExternalApi { get; set; }
    }
}
