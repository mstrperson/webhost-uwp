using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace WebhostAPI.Controllers
{
    public class UtilityController : ApiController
    {
        /// <summary>
        /// Simple json wrapper for a single string value.
        /// </summary>
        [DataContract]
        public class SingleValueString
        {
            /// <summary>
            /// The single string value.
            /// </summary>
            [DataMember(IsRequired =true)]
            public String value { get; set; }
        }

        /// <summary>
        /// Takes the raw data that is passed as a json object as follows,
        /// and returns it as a base64 encoded string.
        /// 
        /// Data should be of the form
        /// 
        /// { "value" : "Some string value.  Character Encoding does not matter." }
        /// 
        /// </summary>
        /// <param name="data">String to be encoded into base64</param>
        /// <returns></returns>
        [Route("api/utility/base64encode")]
        public HttpResponseMessage PostBase64String([FromBody] SingleValueString data)
        {
            String base64 = WebhostMySQLConnection.EVOPublishing.CommentLetter.ConvertToBase64String(data.value);
            HttpResponseMessage response =  Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(base64);
  
            return response;
        }
    }
}
