using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebhostMySQLConnection;

namespace WebhostMySQLConnection.Web
{
    public class CourseRequestExport
    {
        /// <summary>
        /// Get a Blackbaud Formatted AcademicYear String.
        /// </summary>
        /// <param name="term">Term to draw from.</param>
        /// <returns></returns>
        protected static String AcademicYearStr(Term term)
        {
            return String.Format("{0}-{1}", term.AcademicYearID - 1, term.AcademicYearID);
        }

        /// <summary>
        /// If there are duplicates of a course request.  Trash them.
        /// </summary>
        /// <param name="termId"></param>
        public static void CleanDuplicateCourseRequests(int termId)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                List<CourseRequest> Okay = new List<CourseRequest>();
                List<CourseRequest> Duplicates = new List<CourseRequest>();
                foreach(CourseRequest cr in db.CourseRequests.Where(cr => cr.TermId == termId && !cr.RequestableCourse.Course.BlackBaudID.Equals("NONE")).ToList())
                {
                    if(Okay.Where(c => c.StudentId == cr.StudentId && c.RequestableCourse.CourseId == cr.RequestableCourse.CourseId).Count() > 0)
                    {
                        Duplicates.Add(cr);
                    }
                    else
                    {
                        Okay.Add(cr);
                    }
                }

                foreach(CourseRequest dup in Duplicates)
                {
                    foreach(APRequest req in dup.APRequests.ToList())
                    {
                        req.Sections.Clear();
                        db.APRequests.Remove(req);
                    }

                    db.CourseRequests.Remove(dup);
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// Export course requests for the indicated term for importing into Blackbaud.
        /// Output is a csv spreadsheet with collumns required by Blackbaud.
        /// This agorithm checks for multiple course requests within a discipline and determines the priorities based on those requests.
        /// </summary>
        /// <param name="termId"></param>
        /// <returns></returns>
        public static CSV ForBlackbaud(int termId)
        {
            CleanDuplicateCourseRequests(termId);
            CSV csv = new CSV();
            CSV ga = new CSV();

            using(WebhostEntities db = new WebhostEntities())
            {
                foreach(Student student in db.Students.Where(st => st.CourseRequests.Where(c => c.TermId == termId).Count() > 0).ToList())
                {
                    List<CourseRequest> thistermreqs = student.CourseRequests.Where(c => c.TermId == termId && !c.RequestableCourse.Course.BlackBaudID.Equals("NONE")).ToList();
                    
                    // English
                    List<CourseRequest> englishreq = thistermreqs.Where(c => c.RequestableCourse.Course.DepartmentID == 2).OrderBy(c => c.IsSecondary).ToList();
                    if(englishreq.Count == 1)
                    {
                        csv.Add(getRow(englishreq[0]));
                    }
                    else if (englishreq.Count > 1)
                    {
                        if(englishreq[1].IsSecondary)
                        {
                            csv.Add(getRow(englishreq[0], englishreq[1]));
                        }
                        else
                        {
                            csv.Add(getRow(englishreq[0]));
                            csv.Add(getRow(englishreq[1]));
                        }
                    }

                    // history
                    List<CourseRequest> historyreq = thistermreqs.Where(c => c.RequestableCourse.Course.DepartmentID == 3).OrderBy(c => c.IsSecondary).ToList();
                    if (historyreq.Count == 1)
                    {
                        csv.Add(getRow(historyreq[0]));
                    }
                    else if(historyreq.Count > 1)
                    {
                        if (historyreq[1].IsSecondary)
                        {
                            csv.Add(getRow(historyreq[0], historyreq[1]));
                        }
                        else
                        {
                            csv.Add(getRow(historyreq[0]));
                            csv.Add(getRow(historyreq[1]));
                        }
                    }

                    // World Lang
                    List<CourseRequest> wlreq = thistermreqs.Where(c => c.RequestableCourse.Course.DepartmentID == 5).OrderBy(c => c.IsSecondary).ToList();
                    if (wlreq.Count == 1)
                    {
                        csv.Add(getRow(wlreq[0]));
                    }
                    else if(wlreq.Count > 1)
                    {
                        if (wlreq[1].IsSecondary)
                        {
                            csv.Add(getRow(wlreq[0], wlreq[1]));
                        }
                        else
                        {
                            csv.Add(getRow(wlreq[0]));
                            csv.Add(getRow(wlreq[1]));
                        }
                    }

                    // Math
                    List<CourseRequest> mathreq = thistermreqs.Where(c => c.RequestableCourse.Course.DepartmentID == 4).OrderBy(c => c.IsSecondary).ToList();
                    if (mathreq.Count == 1)
                    {
                        csv.Add(getRow(mathreq[0]));
                    }
                    else if (mathreq.Count > 1)
                    {
                        if (mathreq[1].IsSecondary)
                        {
                            csv.Add(getRow(mathreq[0], mathreq[1]));
                        }
                        else
                        {
                            csv.Add(getRow(mathreq[0]));
                            csv.Add(getRow(mathreq[1]));
                        }
                    }

                    // Tech
                    List<CourseRequest> techreq = thistermreqs.Where(c => c.RequestableCourse.Course.DepartmentID == 1).OrderBy(c => c.IsSecondary).ToList();
                    if (techreq.Count == 1)
                    {
                        csv.Add(getRow(techreq[0]));
                    }
                    else if (techreq.Count > 1)
                    {
                        if (techreq[1].IsSecondary)
                        {
                            csv.Add(getRow(techreq[0], techreq[1]));
                        }
                        else
                        {
                            csv.Add(getRow(techreq[0]));
                            csv.Add(getRow(techreq[1]));
                        }
                    }

                    // Art
                    List<CourseRequest> artreq = thistermreqs.Where(c => c.RequestableCourse.Course.DepartmentID == 9).OrderBy(c => c.IsSecondary).ToList();
                    if (artreq.Count == 1)
                    {
                        csv.Add(getRow(artreq[0]));
                    }
                    else if (artreq.Count > 1)
                    {
                        if (artreq[1].IsSecondary)
                        {
                            csv.Add(getRow(artreq[0], artreq[1]));
                        }
                        else
                        {
                            csv.Add(getRow(artreq[0]));
                            csv.Add(getRow(artreq[1]));
                        }
                    }

                    // Science
                    List<CourseRequest> scireq = thistermreqs.Where(c => c.RequestableCourse.Course.DepartmentID == 0).OrderBy(c => c.IsSecondary).ToList();
                    if (scireq.Count == 1)
                    {
                        csv.Add(getRow(scireq[0]));
                    }
                    else if (scireq.Count > 1)
                    {
                        if (scireq[1].IsSecondary)
                        {
                            csv.Add(getRow(scireq[0], scireq[1]));
                        }
                        else
                        {
                            csv.Add(getRow(scireq[0]));
                            csv.Add(getRow(scireq[1]));
                        }

                        if(scireq.Count == 3)
                        {
                            ga.Add(getGARow(scireq[2]));
                        }
                    }
                }
            }

            ga.Save(System.Web.HttpContext.Current.Server.MapPath("~/Temp/BlackBaudGlobalAlternates.csv"));

            return csv;
        }

        protected static String[] Priorities = { "Low", "Standard", "High" };

        /// <summary>
        /// Construct a row for a primary course with an optional alternate course for Course Request in Blackbaud.
        /// </summary>
        /// <param name="primary">The primary course requested.</param>
        /// <param name="secondary">(Optional) The course requested as an alternate to the primary course.</param>
        /// <returns></returns>
        protected static Dictionary<String,String> getRow(CourseRequest primary, CourseRequest secondary = null)
        {
            Dictionary<String, string> row = new Dictionary<string, string>()
            {
                {"Student Course Requested", "Yes"},
                {"Student ID", String.Format("\"{0}\"", primary.Student.BlackbaudId)},
                {"Student Course Academic year", AcademicYearStr(primary.Term)},
                {"Student Course Start term", String.Format("{0} Trimester", primary.Term.Name)},
                {"Student Course Course ID", primary.RequestableCourse.Course.BlackBaudID},
                {"Student Course Session","Regular School"}
            };

            String priority = "";
            if (primary.RequestableCourse.Course.Name.Contains("AP"))
            {
                priority = Priorities[0];
            }
            else if (primary.RequestableCourse.Course.LengthInTerms > 1)
            {
                priority = Priorities[1];
            }
            else
            {
                priority = Priorities[2];
            }

            row.Add("Student Course Priority", priority);

            if(secondary != null)
            {
                row.Add("Student Course Alternate Alternate course ID", secondary.RequestableCourse.Course.BlackBaudID);
                row.Add("Student Course Alternate Alternate start term", String.Format("{0} Trimester", secondary.Term.Name));
                String altpriority = "";
                if (secondary.RequestableCourse.Course.Name.Contains("AP"))
                {
                    altpriority = Priorities[0];
                }
                else if (secondary.RequestableCourse.Course.LengthInTerms > 1 || secondary.RequestableCourse.Course.Department.Name.Equals("English"))
                {
                    altpriority = Priorities[1];
                }
                else
                {
                    altpriority = Priorities[2];
                }

                row.Add("Student Course Alternate Alternate priority", altpriority);
            }

            return row;
        }

        /// <summary>
        /// Construct a row for a Global Alternate course request in Blackbaud.
        /// </summary>
        /// <param name="primary">Global Alternate course requested.</param>
        /// <returns></returns>
        protected static Dictionary<String, String> getGARow(CourseRequest primary)
        {
            Dictionary<String, string> row = new Dictionary<string, string>()
            {
                {"Student Course Requested", "Yes"},
                {"Student ID", String.Format("\"{0}\"", primary.Student.BlackbaudId)},
                {"Student Course Academic year", AcademicYearStr(primary.Term)},
                {"Student Course Alternate Alternate start term", String.Format("{0} Trimester", primary.Term.Name)},
                {"Student Course Alternate Alternate course ID", primary.RequestableCourse.Course.BlackBaudID},
                {"Student Course Session","Regular School"},
                {"Student Course Alternate Alternate priority", Priorities[0]}
            };


            return row;
        }

        /// <summary>
        /// Gets Grade Level Names for graduation years relative to the given year.
        /// </summary>
        protected static Dictionary<int, String> gradeLevels(int currentYear)
        {
            Dictionary<int, string> years = new Dictionary<int, String>()
                {
                    {currentYear, "12th"}, {currentYear + 1, "11th"}, {currentYear+2, "10th"}, {currentYear+3, "9th"}
                };
            return years;
        }

        /// <summary>
        /// Get a human readable course request overview in a CSV formatted spreadsheet.
        /// </summary>
        /// <param name="termId">WebhostEntities.Term.id</param>
        /// <returns></returns>
        public static CSV ForExcel(int termId)
        {
            CSV csv = new CSV();

            using (WebhostEntities db = new WebhostEntities())
            {
                List<CourseRequest> thistermreqs =
                    db.CourseRequests.Where(c => c.TermId == termId && 
                        !c.RequestableCourse.Course.BlackBaudID.Equals("NONE")).OrderBy(c => c.RequestableCourse.Course.DepartmentID).ThenBy(c => c.RequestableCourse.Course.Name).ToList();

                Term term = db.Terms.Find(termId);

                int ptid = DateRange.GetCurrentOrLastTerm();

                foreach (CourseRequest cr in thistermreqs)
                {
                    Dictionary<String, String> row = new Dictionary<string, string>()
                    {
                        {"Student", String.Format("{0} {1}", cr.Student.FirstName, cr.Student.LastName)},
                        {"Grade", gradeLevels(term.AcademicYearID)[cr.Student.GraduationYear]},
                        {"Course", cr.RequestableCourse.Course.Name.Replace(",", "")},
                        {"Department", cr.RequestableCourse.Course.Department.Name},
                        {"Priority", cr.IsSecondary || cr.IsGlobalAlternate?"Alternate":""},
                        {"Advisor", String.Format("{0} {1}", cr.Student.Advisor.FirstName, cr.Student.Advisor.LastName)}
                    };

                    if(cr.RequestableCourse.Course.Name.Contains("AP"))
                    {
                        if (cr.APRequests.Count <= 0) row.Add("AP Form Submitted", "No");
                        else
                        {
                            row.Add("AP Form Submitted", "Yes");
                            APRequest req = cr.APRequests.FirstOrDefault();
                            foreach(Section section in req.Sections.ToList())
                            {
                                try
                                {
                                    row.Add(String.Format("{0} Course", section.Course.Department.Name), section.Course.Name);
                                    if (cr.Student.StudentComments.Where(com => com.CommentHeader.SectionIndex == section.id && com.CommentHeader.TermIndex == ptid).Count() > 0)
                                    {
                                        StudentComment comment = cr.Student.StudentComments.Where(com => com.CommentHeader.SectionIndex == section.id && com.CommentHeader.TermIndex == ptid).Single();
                                        row.Add(String.Format("{0} Grade", section.Course.Department.Name), comment.FinalGrade.Name);
                                    }
                                    else
                                    {
                                        row.Add(String.Format("{0} Grade", section.Course.Department.Name), "Not Submitted");
                                    }
                                }
                                catch(Exception e)
                                {
                                    State.log.WriteLine(e.Message);
                                }
                            }

                            row.Add("Teacher Signed", String.Format("{0} {1}", req.Teacher.FirstName, req.Teacher.LastName));
                            row.Add("Dept Head Signed", String.Format("{0} {1}", req.DepartmentHead.FirstName, req.DepartmentHead.LastName));
                            row.Add("Approval", req.GradeTableEntry.Name);
                        }
                    }

                    csv.Add(row);
                }
            }

            return csv;
        }

        public static String CourseRequestsByStudent(int termId, String SaveRootPath)
        {
            SaveRootPath = SaveRootPath.Replace("/", "\\"); // change uri to windows format.
            if(!SaveRootPath.EndsWith("\\"))
            {
                SaveRootPath += "\\";
            }
            String zipFile = SaveRootPath + "CourseRequests.zip";
            List<String> fileNames = new List<string>();
            using(WebhostEntities db = new WebhostEntities())
            {
                List<int> sids = new List<int>();
                foreach(CourseRequest cr in db.CourseRequests.Where(c => c.TermId == termId).ToList())
                {
                    if (!sids.Contains(cr.StudentId)) sids.Add(cr.StudentId);
                }

                foreach(Section sec in db.Sections.Where(s => s.Terms.Where(t => t.id == termId).Count() > 0).ToList())
                {
                    foreach(Student student in sec.Students.ToList())
                    {
                        if (!sids.Contains(student.ID)) sids.Add(student.ID);
                    }
                }

                foreach(int id in sids)
                {
                    Student student = db.Students.Where(s => s.ID == id).Single();
                    CSV csv = StudentCourseRequests(termId, id);
                    String fileName = SaveRootPath + String.Format("{0} {1} [{2}].csv", student.LastName, student.FirstName, student.GraduationYear);
                    csv.Save(fileName);
                    fileNames.Add(fileName);
                }
            }

            zipFile = MailControler.PackForDownloading(fileNames, zipFile);

            return zipFile;
        }

        /// <summary>
        /// Get a CSV of a particular Student's course requests along with their current courses.
        /// </summary>
        /// <param name="termId"></param>
        /// <param name="studentId"></param>
        /// <returns></returns>
        public static CSV StudentCourseRequests(int termId, int studentId)
        {
            CSV csv = new CSV();
            using(WebhostEntities db = new WebhostEntities())
            {
                Student student = db.Students.Where(s => s.ID == studentId).Single();
                foreach(Section section in student.Sections.Where(sec => sec.Terms.Where(t => t.id == termId).Count() > 0).ToList())
                {
                    csv.Add(new Dictionary<String, String>()
                        {
                            {"Block", section.Block.Name},
                            {"Course", section.Course.Name},
                            {"Full Year", section.Course.LengthInTerms > 1?"X":""}
                        });
                }

                foreach(CourseRequest request in student.CourseRequests.Where(cr => cr.TermId == termId).ToList())
                {
                    String priority = request.IsGlobalAlternate ? "Third Choice" : request.IsSecondary ? "Second Choice" : "First Choice";
                    csv.Add(new Dictionary<String, String>()
                        {
                            {"Block","Request"},
                            {"Course", request.RequestableCourse.Course.Name},
                            {priority, "X"}
                        });
                }
            }
            return csv;
        }

        /// <summary>
        /// Get a CSV Table showing students who have or have not completed their course requests for this term.
        /// </summary>
        /// <param name="termId"></param>
        /// <returns></returns>
        public static CSV CourseRequestsCompleted(int termId)
        {
            CSV csv = new CSV();
            using (WebhostEntities db = new WebhostEntities())
            {
                Term thisTerm = db.Terms.Find(termId);
                List<Student> relevantStudents = db.Students.Where(s => s.isActive 
                    && s.GraduationYear >= thisTerm.AcademicYearID 
                    && s.GraduationYear < thisTerm.AcademicYearID + 4).OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();

                foreach(Student student in relevantStudents)
                {

                    int classCount = student.Sections.Where(sec => sec.Terms.Where(t => t.id == thisTerm.id).Count() > 0).Count();
                    Dictionary<String, String> row = new Dictionary<string, string>()
                    {
                        {"LastName", student.LastName},
                        {"FirstName", student.FirstName},
                        {"Grade", gradeLevels(thisTerm.AcademicYearID)[student.GraduationYear]},
                        {"Advisor", String.Format("{0} {1}", student.Advisor.FirstName, student.Advisor.LastName)},
                        {"Submitted?", student.CourseRequests.Where(cr => cr.TermId == termId).Count() > 0?"Yes":"No"},
                        {String.Format("# of existing {0} course", thisTerm.Name), Convert.ToString(classCount)}
                    };

                    csv.Add(row);
                }

            }
            return csv;
        }

        /// <summary>
        /// Get an overview of numbers of students requesting courses.
        /// </summary>
        /// <param name="termId"></param>
        /// <returns></returns>
        public static CSV ForExcelByClass(int termId)
        {
            CSV csv = new CSV();

            using(WebhostEntities db = new WebhostEntities())
            {
                List < CourseRequest > thistermreqs = 
                    db.CourseRequests.Where(c => c.TermId == termId && !c.RequestableCourse.Course.BlackBaudID.Equals("NONE")).OrderBy(c => c.RequestableCourse.Course.DepartmentID).ThenBy(c => c.RequestableCourse.Course.Name).ToList();
                
                Dictionary<Course, List<int>> coursedata = new Dictionary<Course, List<int>>();
                foreach(CourseRequest cr in thistermreqs)
                {
                    if(!coursedata.ContainsKey(cr.RequestableCourse.Course))
                    {
                        List<int> counts = new List<int>() { 0, 0 };
                        coursedata.Add(cr.RequestableCourse.Course, counts);
                    }

                    if (cr.IsSecondary || cr.IsGlobalAlternate)
                        coursedata[cr.RequestableCourse.Course][1]++;
                    else
                        coursedata[cr.RequestableCourse.Course][0]++;
                }

                foreach(Course key in coursedata.Keys)
                {
                    Dictionary<String, String> row = new Dictionary<string, string>()
                    {
                        {"Course", key.Name},
                        {"First Choice", Convert.ToString(coursedata[key][0])},
                        {"Alternate", Convert.ToString(coursedata[key][1])},
                        {"Total Signups", Convert.ToString(coursedata[key][0] + coursedata[key][1])}
                    };
                    
                    csv.Add(row);
                }
            }

            return csv;
        }


        /// <summary>
        /// Get the Notes associated with students course requests for a given term.
        /// </summary>
        /// <param name="termId">WebhostEntities.Term.id</param>
        /// <returns></returns>
        public static String GetCourseRequestNotes(int termId)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                String notes = "";
                Term term = db.Terms.Find(termId);

                foreach(CourseRequestComment comment in db.CourseRequestComments.Where(c => c.TermId == termId).OrderBy(c => c.Student.GraduationYear).ThenBy(c => c.Student.LastName).ToList())
                {
                    notes += String.Format("{0}, {1} ({2} Grade):{3}{4}{3}{3}", 
                        comment.Student.LastName, comment.Student.FirstName, gradeLevels(term.AcademicYearID)[comment.Student.GraduationYear], Environment.NewLine, comment.Notes);
                }
                return notes;
            }
        }
    }
}
