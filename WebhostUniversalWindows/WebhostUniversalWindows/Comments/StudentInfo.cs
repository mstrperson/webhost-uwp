using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebhostUniversalWindows.Comments;

namespace WebhostUniversalWindows
{
    public partial class StudentInfo
    {
        public async Task<CommentInfo> CurrentCommentAsync()
        {
            try
            {
                return (CommentInfo)await WebhostAPICall.GetObjectAsync(String.Format("api/self/sections/{0}/students/{1}/comment", Comments.CommentSettings.CurrentHeaderParagraph.SectionId, this.Id), typeof(CommentHeaderInfo));
            }
            catch (WebException)
            {
                return await CreateBlankCommentAsync();
            }
        }

        public async Task<CommentInfo> CreateBlankCommentAsync()
        {
            return (CommentInfo)await WebhostAPICall.GetObjectAsync(String.Format("api/self/sections/{0}/students/{1}/comment", Comments.CommentSettings.CurrentHeaderParagraph.SectionId, this.Id), typeof(CommentHeaderInfo), "PUT", typeof(String), "");
        }

        public async Task<HttpWebResponse> SaveCommentAsync(String data)
        {
            return await WebhostAPICall.SendAuthorizedApiRequestAsync(String.Format("api/self/sections/{0}/students/{1}/comment", Comments.CommentSettings.CurrentHeaderParagraph.SectionId, this.Id), "PUT", typeof(String), data);
        }
    }
}
