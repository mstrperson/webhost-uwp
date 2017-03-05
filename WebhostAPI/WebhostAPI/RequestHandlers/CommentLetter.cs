﻿using EvoPdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WebhostAPI;
using WebhostMySQLConnection.Web;

namespace WebhostMySQLConnection.EVOPublishing
{
    public class CommentLetter : Letterhead
    {
        public static String ConvertToBase64String(String HTML)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.WriteLine(HTML);
            writer.Flush();
            ms.Position = 0;
            byte[] buffer = new byte[ms.Length];
            ms.Read(buffer, 0, (int)ms.Length);
            writer.Close();
            return Convert.ToBase64String(buffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static String ConvertFromBase64String(String data)
        {
            try
            {
                MemoryStream ms = new MemoryStream();

                byte[] buffer = Convert.FromBase64String(data);
                ms.Write(buffer, 0, buffer.Length);
                ms.Position = 0;

                StreamReader reader = new StreamReader(ms);
                String message = reader.ReadToEnd();
                reader.Close();

                return message;
            }
            catch(FormatException)
            {
                return data;
            }
        }

        public static Dictionary<String, int> GetGradeTableEntryData()
        {
            Dictionary<String, int> data = new Dictionary<string, int>();
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                int year = DateRange.GetCurrentAcademicYear();
                List<GradeTableEntry> entries = db.GradeTableEntries.Where(gte => gte.GradeTable.AcademicYearID == year).ToList();

                foreach(GradeTableEntry entry in entries)
                {
                    data.Add(entry.Name, entry.id);
                }
            }

            return data;
        }

        private static List<String> DisallowedStyles = new List<string>()
        {
            "font-family",
            "font-size",
            "color",
            "border(-[^:]*)?",
            "margin(-[^:]*)?",
            "padding(-[^:]*)?",
            "position",
            "((max|min)-)?height",
            "((max|min)-)?width"
        };

        public static String CleanTags(String HTML, bool forPublish = false)
        {
            if (forPublish) HTML = HTML.Replace("{plus-sign}", "+");
            List<Regex> badStyles = new List<Regex>();
            foreach(String style in DisallowedStyles)
            {
                badStyles.Add(new Regex(style + "[^;}]*;?"));
            }

            foreach(Regex style in badStyles)
            {
                foreach(Match match in style.Matches(HTML))
                {
                    HTML = HTML.Replace(match.Value, "");
                }
            }

            return HTML;
        }


        public static String CleanTagsOld(String HTML, bool forPublish = false)
        {
            if (forPublish) HTML = HTML.Replace("{plus-sign}", "+");
            if (!HTML.StartsWith("<p"))
            {
                HTML = "<p>" + HTML;
            }
            Regex pTagPlus = new Regex("<p [^>]+>");
            Regex spanTag = new Regex("<span[^>]*>");
            String boldSpan = "font-weight: bold;";
            String italSpan = "font-style: italic;";
            String ulnSpan = "text-decoration: underline;";
            Regex brTag = new Regex("<br ?/?>");
            foreach (Match match in pTagPlus.Matches(HTML))
            {
                HTML = HTML.Replace(match.Value, "<p>");
            }
            foreach (Match match in spanTag.Matches(HTML))
            {
                bool bold = match.Value.Contains(boldSpan), ital = match.Value.Contains(italSpan), uln = match.Value.Contains(ulnSpan);

                String replace = "";
                if (bold)
                    replace += "<b>";
                if (ital)
                    replace += "<i>";
                if (uln)
                    replace += "<u>";

                int indexOfMatch = HTML.IndexOf(match.Value);

                HTML = HTML.Substring(0, indexOfMatch) + replace + HTML.Substring(indexOfMatch + match.Value.Length);

                int indexOfEnd = HTML.IndexOf("</span>", indexOfMatch);
                if (indexOfEnd == -1) indexOfEnd = HTML.Length;
                HTML = HTML.Substring(0, indexOfEnd) + (uln ? "</u>" : "") + (ital ? "</i>" : "") + (bold ? "</b>" : "") + (indexOfEnd + 7 <= HTML.Length ? HTML.Substring(indexOfEnd + 7) : "");
            }
            foreach (Match match in brTag.Matches(HTML))
            {
                HTML = HTML.Replace(match.Value, "</p><p>");
            }

            if (HTML.EndsWith("</p><p>"))
            {
                HTML = HTML.Substring(0, HTML.Length - 3);
            }
            else if (!HTML.EndsWith("</p>"))
            {
                HTML += "</p>";
            }

            return HTML;
        }

        protected static String GenerateHeaderTable(String Advisor, String Student, String Course, String Exam, String Trimester, String Effort, String Final)
        {
            return String.Format("<table style='width: 100%; border-top: 2px solid black; border-bottom: 2px solid black;'><tr><td>Student:  {0}</td><td>Advisor:  {1}</td><td colspan='2'>Course:  {2}</td></tr>" +
                                 "<tr><td>Exam Grade:  {3}</td><td>Trimester Grade:  {4}</td><td>Engagement Grade:  {5}</td><td>Final Grade:  {6}</td></tr></table>", 
                                 Student, Advisor, Course, Exam, Trimester, Effort, Final);
        }

        protected String HeaderTable { get; set; }
        protected String IndividualParagraph { get; set; }
        protected String Content { get; set; }

        public CommentLetter(int commentId) : base()
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                StudentComment comment = db.StudentComments.Find(commentId);

                String studentName = String.Format("{0} {1}", comment.Student.FirstName, comment.Student.LastName);
                String advisorName = String.Format("{0} {1}", comment.Student.Advisor.FirstName, comment.Student.Advisor.LastName);
                String courseName = comment.CommentHeader.Section.Course.Name;

                String title = String.Format("{0} {1} Comments for {2}", courseName, comment.CommentHeader.Term.Name, studentName);
                foreach(char ch in Path.GetInvalidFileNameChars())
                {
                    title = title.Replace(ch, ' ');
                }

                this.Title = title;

                HeaderTable = GenerateHeaderTable(advisorName, studentName, courseName, comment.ExamGrade.Name, comment.TermGrade.Name, comment.EngagementGrade.Name, comment.FinalGrade.Name);

                IndividualParagraph = comment.HTML;

                Content = comment.CommentHeader.HTML + comment.HTML + "<table style='vertical-align: top'><tr>";
                foreach(Faculty teacher in comment.CommentHeader.Section.Teachers.ToList())
                {
                    Content += String.Format("<td>{1} {2}<br/>{0}</td>", SignatureImage(teacher.ID), teacher.FirstName, teacher.LastName);
                }
                Content += "</tr></table>";
            }
        }

        public Document Publish()
        {
            try
            {
                return PublishGenericLetter(HeaderTable + Content, true);
            }
            catch(PdfDocumentException)
            {
                return PublishGenericLetter(HeaderTable + Content);
            }
        }

        public String ProofBody
        {
            get
            {
                return String.Format("<hr><article>{0}{1}</article>", HeaderTable, IndividualParagraph);
            }
        }

        /// <summary>
        /// Make sure the rootPath ends with a \\ or / depending on context.
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public String Publish(String rootPath)
        {
            String fileName = String.Format("{0}{1}.pdf", rootPath, EncodeSafeFileName(this.Title));
            this.Publish().Save(fileName);
            return fileName;
        }

        public static String PublishTermByStudent(int termId = -1, List<int> studentIds = null, String SaveDir = "")
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                if (db.Terms.Where(t => t.id == termId).Count() <= 0) termId = DateRange.GetCurrentOrLastTerm();

                Term term = db.Terms.Where(t => t.id == termId).Single();
                String packFileName = term.Name.ToLower() + "_term_comments.zip";
                List<String> termCommentFiles = new List<string>();

                if (studentIds == null)
                {
                    studentIds = ActiveStudents(termId);
                }

                foreach (int studentId in studentIds)
                {
                    Student student = db.Students.Where(s => s.ID == studentId).Single();
                    String studentFileName = String.Format("[{3}] {0}, {1} {2} Comments.zip", student.LastName, student.FirstName, term.Name, student.GraduationYear);
                    WebhostEventLog.CommentLog.LogInformation("Publishing {0}", studentFileName);
                    List<String> studentFiles = new List<String>();
                    List<int> commentIds = student.StudentComments.Where(com => com.CommentHeader.TermIndex == termId).Select(com => com.id).ToList();
                    foreach (int id in commentIds)
                    {
                        studentFiles.Add((new CommentLetter(id)).Publish(SaveDir));
                    }

                    if (SaveDir.Equals(""))
                        termCommentFiles.Add(MailControler.PackForDownloading(studentFiles, studentFileName, HttpContext.Current.Server));
                    else
                        termCommentFiles.Add(MailControler.PackForDownloading(studentFiles, String.Format("{0}\\{1}", SaveDir, studentFileName)));
                }

                if (SaveDir.Equals(""))
                    return MailControler.PackForDownloading(termCommentFiles, packFileName, HttpContext.Current.Server);
                else
                    return MailControler.PackForDownloading(termCommentFiles, String.Format("{0}\\{1}", SaveDir, packFileName));
            }
        }

        /// <summary>
        /// Get students flagged as currently active.
        /// </summary>
        /// <returns></returns>
        public static List<int> ActiveStudents()
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                return db.Students.Where(s => s.isActive).Select(s => s.ID).ToList();
            }
        }

        /// <summary>
        /// Get Students with classes active in the given Term.
        /// </summary>
        /// <param name="termid"></param>
        /// <returns></returns>
        public static List<int> ActiveStudents(int termid)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                if (db.Terms.Where(t => t.id == termid).Count() <= 0) termid = DateRange.GetCurrentOrLastTerm();

                List<int> ids = new List<int>();
                foreach (Student student in db.Students.ToList())
                {
                    foreach (Section section in student.Sections)
                    {
                        if (section.Terms.Where(t => t.id == termid).Count() > 0)
                        {
                            ids.Add(student.ID);
                            break;
                        }
                    }
                }

                return ids;
            }
        }

        public static String PublishClass(int headerId, String SaveDir = "")
        {
            List<int> commentIds = new List<int>();
            List<String> fileNames = new List<string>();
            String packFileName = "";
            using (WebhostEntities db = new WebhostEntities())
            {
                CommentHeader header = db.CommentHeaders.Find(headerId);
                packFileName = String.Format("{0} {1} comments", header.Section.Block.LongName, header.Section.Course.Name).ToLower();
                WebhostEventLog.CommentLog.LogInformation("Publishing {0}", packFileName);
                packFileName = packFileName.Replace(" ", "_");
                packFileName = packFileName.Replace("\"", "");
                Regex disalowedChars = new Regex(@"(\.|:|&|#|@|\*|~|\?|<|>|\||\^|( ( )+)|/)");
                foreach (Match match in disalowedChars.Matches(packFileName))
                {
                    packFileName = packFileName.Replace(match.Value, "");
                }

                foreach (int id in header.StudentComments.Select(c => c.id))
                {
                    fileNames.Add((new CommentLetter(id)).Publish(SaveDir));
                }
            }

            if (SaveDir.Equals(""))
            {
                return MailControler.PackForDownloading(fileNames, packFileName, HttpContext.Current.Server);
            }
            else
            {
                return MailControler.PackForDownloading(fileNames, String.Format("{0}\\{1}.zip", SaveDir, packFileName));
            }
        }
    }
}
