using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebhostMySQLConnection.EVOPublishing
{
    public class CommentReviewPDF : Letterhead
    {
        protected List<CommentLetter> Letters;
        protected String HeaderHTML;
        protected String TitleBar;

        public CommentReviewPDF(int sectionId, int termId)
        {
            Letters = new List<CommentLetter>();
            using(WebhostEntities db = new WebhostEntities())
            {
                Section section = db.Sections.Find(sectionId);
                if(section == null)
                {
                    WebhostEventLog.CommentLog.LogError("Failed to locate section {0} for comment review.", sectionId);
                    throw new ArgumentException(String.Format("Invalide Section Id {0}", sectionId));
                }

                Title = String.Format("Comment Proofs [{0}] {1}", section.Block.LongName, section.Course.Name);

                CommentHeader Header;
                try
                {
                    Header = section.CommentHeaders.Where(h => h.TermIndex == termId).Single();
                }
                catch(Exception e)
                {
                    WebhostEventLog.CommentLog.LogError(String.Format("Failed to locate Header Paragraph:  {0}", e.Message));
                    throw new ArgumentException("Failed to locate Header Paragraph", e);
                }

                HeaderHTML = Header.HTML;

                foreach(int id in Header.StudentComments.Select(c => c.id).ToList())
                {
                    Letters.Add(new CommentLetter(id));
                }
            }
        }

        public void Publish(String path)
        {
            String Body = "";
            foreach(CommentLetter letter in Letters)
            {
                Body += letter.ProofBody;
            }

            if (!AssertPath(path))
                throw new FormatException(String.Format("Invalid file path:  {0}", path));

            this.PublishGenericLetter(HeaderHTML + Body).Save(path);
        }
    }
}
