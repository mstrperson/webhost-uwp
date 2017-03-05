using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WebhostUniversalWindows.Comments
{
    /// <summary>
    /// Comment Header information packaged for JSON data transmission.
    /// </summary>
    [DataContract]
    public class CommentHeaderInfo
    {
        /// <summary>
        /// Webhost Database CommentHeader.id
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int Id { get; set; }

        /// <summary>
        /// CommentHeader.SectionIndex
        /// </summary>
        [DataMember(IsRequired = true)]
        public int SectionId { get; set; }

        /// <summary>
        /// CommentHeader.TermId
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int TermId { get; set; }

        /// <summary>
        /// Section title pulled from the SectionInfo object.
        /// e.g.  "[B-Block] AP Computer Science Principles"
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String SectionTitle { get; set; }

        /// <summary>
        /// What Term is this Header for?
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Term { get; set; }

        /// <summary>
        /// Get HtmlContent appropriate for placing into the SfRichEditBoxAdv.
        /// </summary>
        [IgnoreDataMember]
        public String Html
        {
            get
            {
                return String.Format("<html><body>{0}</body></html>", WebhostAPICall.ConvertFromBase64String(HtmlContent));
            }
        }
        
        /// <summary>
        /// Put the content of the SfRichEditBoxAdv back into HtmlContent for transfering to Webhost.
        /// </summary>
        /// <param name="value"></param>
        public void SetHtml(String value)
        {
            String stripped = value.Replace("<html><body>", "").Replace("</html></body>", "");
            HtmlContent = WebhostAPICall.ConvertToBase64String(stripped);
        }

        /// <summary>
        /// HTML content encoded into a Base64String for clean data transmission.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String HtmlContent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public byte[] RtfContent { get; set; }

        /// <summary>
        /// List of Webhost Database Id numbers for student Comments
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<int> StudentCommentIds { get; set; }

        /// <summary>
        /// List of all the data associated with student comments connected to this header.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<CommentInfo> StudentComments { get; set; }

        /// <summary>
        /// Print as JSON.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            String json = JsonConvert.SerializeObject(this, typeof(CommentHeaderInfo), new JsonSerializerSettings());
            return json;
        }

        /// <summary>
        /// Get an instance from JSON data.
        /// </summary>
        /// <param name="data">JSON representation of an instance.</param>
        /// <returns></returns>
        public static CommentHeaderInfo FromJson(string data)
        {
            CommentHeaderInfo info = (CommentHeaderInfo)JsonConvert.DeserializeObject(data, typeof(CommentHeaderInfo), new JsonSerializerSettings());
            return info;
        }
    }
}