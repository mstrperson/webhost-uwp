using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WebhostUniversalWindows.Comments;

namespace WebhostUniversalWindows
{
    public partial class SectionInfo
    {
        public async Task<CommentHeaderInfo> CurrentCommentHeaderAsync()
        {
            try
            {
                return (CommentHeaderInfo)await WebhostAPICall.GetObjectAsync(String.Format("api/self/sections/{0}/comment_header", this.Id), typeof(CommentHeaderInfo));
            }
            catch(WebException)
            {
                return await CreateBlankHeaderAsync();
            }
        }

        public async Task<CommentHeaderInfo> CreateBlankHeaderAsync()
        {
            return (CommentHeaderInfo)await WebhostAPICall.GetObjectAsync(String.Format("api/self/sections/{0}/comment_header", this.Id), typeof(CommentHeaderInfo), "PUT", typeof(String), "");
        }

        public async Task<HttpWebResponse> SaveCommentHeaderAsync(String data)
        {
            return await WebhostAPICall.SendAuthorizedApiRequestAsync(String.Format("api/self/sections/{0}/comment_header", this.Id), "PUT", typeof(String), data);
        }
    }
}
