using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebhostMySQLConnection.Web;

namespace WebhostMySQLConnection.EVOPublishing
{
    public class PublishRequest
    {
        protected class LogEntry
        {
            protected String Message;
            
            public void AppendMessage(String newLine)
            {
                Message += newLine;
            }

            public LogEntry()
            {
                Message = "";
            }

            new public String ToString()
            {
                return Message;
            }
        }

        /// <summary>
        /// Request a set of classes be published in bulk.
        /// </summary>
        /// <param name="user">requesting user</param>
        /// <param name="SectionIds">List of Section.id's</param>
        /// <param name="filename">file name given by the Server.MapPath() method.</param>
        /// <param name="TermId">default to the current term</param>
        public static void RequestByClass(ADUser user, List<int> SectionIds, String filename, int TermId = -1)
        {
            WebhostEventLog.Syslog.LogInformation("{0} has requested Comment Letters for {1} sections", user.Name, SectionIds.Count);
            if (TermId == -1) TermId = DateRange.GetCurrentOrLastTerm();

            LogEntry log = new LogEntry();

            XMLTree xml = new XMLTree()
            {
                TagName = "publishrequest",
                Attributes = new Dictionary<string, string>()
                {
                    {"username", user.UserName},
                    {"name", XMLTree.MakeXMLAttributeValueSafe(user.Name)},
                    {"id", user.ID.ToString()},
                    {"type", "class"},
                    {"termid", TermId.ToString()},
                    {"timestamp", DateTime.Now.Ticks.ToString()}
                }
            };

            using(WebhostEntities db = new WebhostEntities())
            {
                foreach(int sectionId in SectionIds)
                {
                    Section section = db.Sections.Where(sec => sec.id == sectionId).Single();
                    if(section.CommentHeaders.Where(c => c.TermIndex == TermId).Count() != 1)
                    {
                        throw new WebhostException(String.Format("No comment header for Term id={0} of {1} ({2})", TermId, section.Course.Name, sectionId));
                    }

                    CommentHeader header = section.CommentHeaders.Where(c => c.TermIndex == TermId).Single();

                    XMLTree sectionTree = new XMLTree()
                    {
                        TagName = "section",
                        Attributes = new Dictionary<string, string>()
                        {
                            {"headerid", header.id.ToString()}
                        }
                    };

                    foreach(int comid in header.StudentComments.Select(c => c.id).ToList())
                    {
                        sectionTree.ChildNodes.Add(new SimpleXMLTag()
                            {
                                TagName = "comment",
                                Value = comid.ToString()
                            });
                    }

                    xml.ChildTrees.Add(sectionTree);
                }
            }

            xml.Save(filename + ".pubreq");
        }

        /// <summary>
        /// Request a batch of Student's comments.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="StudentIds"></param>
        /// <param name="filename">file name given by the Server.MapPath() method.</param>
        /// <param name="TermId">default to the current term</param>
        public static void RequestByStudent(ADUser user, List<int> StudentIds, String filename, int TermId = -1)
        {
            if (TermId == -1) TermId = DateRange.GetCurrentOrLastTerm();
            XMLTree xml = new XMLTree()
            {
                TagName = "publishrequest",
                Attributes = new Dictionary<string, string>()
                {
                    {"username", user.UserName},
                    {"name", XMLTree.MakeXMLAttributeValueSafe(user.Name)},
                    {"id", user.ID.ToString()},
                    {"type", "student"},
                    {"termid", TermId.ToString()},
                    {"timestamp", DateTime.Now.Ticks.ToString()}
                }
            };

            using (WebhostEntities db = new WebhostEntities())
            {
                foreach (int sid in StudentIds)
                {
                    Student student = db.Students.Where(s => s.ID == sid).Single();
                    if (student.StudentComments.Where(com => com.CommentHeader.TermIndex == TermId).Count() <= 0)
                        throw new WebhostException(String.Format("No Comments for {0} {1} in term id={2}", student.FirstName, student.LastName, TermId));

                    XMLTree studentTree = new XMLTree()
                    {
                        TagName = "student",
                        Attributes = new Dictionary<string, string>()
                        {
                            {"studentid", sid.ToString()}
                        }
                    };

                    foreach(int comid in student.StudentComments.Select(c => c.id).ToList())
                    {
                        studentTree.ChildNodes.Add(new SimpleXMLTag()
                            {
                                TagName="comment",
                                Value = comid.ToString()
                            });
                    }

                    xml.ChildTrees.Add(studentTree);
                }
            }
            xml.Save(filename + ".pubreq");
        }

        public static void ExecuteRequest(String fileName)
        {
            StreamReader reader = new StreamReader(new FileStream(fileName, FileMode.Open));
            String xmlstr = reader.ReadToEnd();
            reader.Close();
            XMLTree xml = new XMLTree(xmlstr);

            String dir = Directory.GetParent(fileName).FullName;

            dir += String.Format("\\{0}", xml.Attributes["username"]);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!xml.Attributes.ContainsKey("type"))
                throw new XMLException("File root tag does not contain a type attribute.");

            if(xml.Attributes["type"].Equals("class"))
            {
                ExecuteClassRequest(xml, dir);    
            }
            else if(xml.Attributes["type"].Equals("student"))
            {
                ExecuteStudentRequest(xml, dir);
            }
            else
            {
                throw new XMLException("File has an invalid type attribute.");
            }

            File.Delete(fileName);
        }

        protected static void ExecuteStudentRequest(XMLTree xml, String saveDir)
        {
            int termId = Convert.ToInt32(xml.Attributes["termid"]);

            List<int> studentIds = new List<int>();
            foreach(XMLTree studentTree in xml.ChildTrees.Where(tree => tree.TagName.Equals("student")).ToList())
            {
                int studentId = Convert.ToInt32(studentTree.Attributes["studentid"]);
                studentIds.Add(studentId);
            }

            String fileName = CommentLetter.PublishTermByStudent(termId, studentIds, saveDir);

            MailControler.MailToUser("Comments Published Successfully",
                String.Format("Click the Link to download the comments you requested:{0}{1}",
                    Environment.NewLine,
                    fileName.Replace("W:", "https://webhost.dublinschool.org").Replace("\\", "/")), 
                String.Format("{0}@dublinschool.org", xml.Attributes["username"]), 
                xml.Attributes["name"],
                "noreply@dublinschool.org", "Comment Bot");

        }

        protected static void ExecuteClassRequest(XMLTree xml, String saveDir)
        {
            int termId = Convert.ToInt32(xml.Attributes["termid"]);

            List<int> studentIds = new List<int>();
            List<String> filenames = new List<string>();
            foreach (XMLTree studentTree in xml.ChildTrees.Where(tree => tree.TagName.Equals("section")).ToList())
            {
                int headerId = Convert.ToInt32(studentTree.Attributes["headerid"]);
                filenames.Add(CommentLetter.PublishClass(headerId, saveDir));
            }

            String zipFile = MailControler.PackForDownloading(filenames, String.Format("{0}\\classes_{1}.zip", saveDir, DateTime.Now.Ticks));

            MailControler.MailToUser("Comments Published Successfully",
                String.Format("Click the Link to download the comments you requested:{0}{1}", 
                    Environment.NewLine, 
                    zipFile.Replace("W:", "https://webhost.dublinschool.org").Replace("\\", "/")),
                String.Format("{0}@dublinschool.org", xml.Attributes["username"]),
                xml.Attributes["name"].Replace('_', ' '),
                "noreply@dublinschool.org", "Comment Bot");
        }
    }
}
