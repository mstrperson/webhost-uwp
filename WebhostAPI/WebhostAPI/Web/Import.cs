using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WebhostMySQLConnection.GoogleAPI;
using WebhostMySQLConnection.EVOPublishing;
using EvoPdf;

namespace WebhostMySQLConnection.Web
{
    public class Import
    {
        #region BlackBaud Field Name Constants

        private static String GradeSheet_AcademicYear = "Academic year";
        private static String GradeSheet_CourseInfo = "Class ID"; // this field contains both Blackbaud Course ID and Section Number (e.g. "329 - 1")
        private static String GradeSheet_StudentId = "Student ID";
        private static String GradeSheet_Term = "Class term enrolled";
        private static String GradeSheet_TrimesterGrade(int number) { return String.Format("T{0}G", number); }
        private static String GradeSheet_EffortGrade(int number) { return String.Format("T{0}E", number); }
        private static String GradeSheet_ExamGrade(int number) { return String.Format("T{0}X", number); }
        private static String GradeSheet_FinalGrade = "FG";

        private static String Dorm_LastName = "ApptsAppt_Lastname";
        private static String Dorm_FirstName = "ApptsAppt_Firstname";
        private static String Dorm_Dorm = "Dorm";
        private static List<String> DormNames = new List<string>()
        {
            "New",
            "Corner",
            "Hill",
            "Monadnock",
            "Wing",
            "Lehmann",
            "Hoyt"
        };

        private static String Course_BBCourseID = "CrsesCrse_CourseID";
        private static String Course_CourseName = "CrsesCrse_Coursename";
        private static String Course_Department = "CrsesCrse_Departments";
        private static String Course_LengthInTerms = "CrsesRestr_1_01_Lengthinterms";
        
        private static String Section_SectionNumber = "ClsssClss_Classsection";
        //private static String Section_AcademicYear = "ClsssClss_Academicyear";
        private static String Section_StartTerm = "ClsssClss_Startterm";
        private static String Section_CourseID = "ClsssClssCrse_CourseID";
        private static String Section_Block = "ClsssMtgs_1_01_Block";
        
        private static String Roster_SectionNumber = "Section";
        private static String Roster_BBCourseID = "Course ID";
        //private static String Roster_AcademicYear = "Academic year";
        private static String Roster_Term = "Term";
        private static String Roster_TeacherID = "Record ID";
        private static String Roster_StudentID = "Student ID";

        private static String Student_StudentID = "StsSt_StudentID";
        private static String Student_AdvisorID = "StsStAdv_1_01_RecordID";
        private static String Student_FirstName = "StsSt_Firstname";
        private static String Student_LastName = "StsSt_Lastname";
        private static String Student_GraduationYear = "StsEnrlls_1_01_Classof";
        private static String Student_CurrentGrade = "StsSt_Currentgrade";
        //private static String Student_Nickname = "StsSt_Nickname";

        private static String Faculty_LastName = "FacStfFac_Lastname";
        private static String Faculty_FirstName = "FacStfFac_Firstname";
        private static String Faculty_EmpID = "FacStfFac_RecordID";

        private static Dictionary<String, int> Grades = new Dictionary<string, int>() { { "Ninth", 9 }, { "Tenth", 10 }, { "Eleventh", 11 }, { "Twelfth", 12 } };

        #endregion  // Blackbaud Field Names

        #region Standard Values For Initialization
        public struct BlockPrimitive
        {
            public String LongName;
            public bool showInSchedule;
        }

        private static Dictionary<String, BlockPrimitive> StandardBlocks = new Dictionary<string, BlockPrimitive>() { {"Morning Meeting", new BlockPrimitive() { showInSchedule = false, LongName = "Morning Meeting" } },
                                                                                                  { "A", new BlockPrimitive() { showInSchedule = true, LongName = "A-Block" }},
                                                                                                  { "B", new BlockPrimitive() { showInSchedule = true, LongName = "B-Block" }},
                                                                                                  { "C", new BlockPrimitive() { showInSchedule = true, LongName = "C-Block" }},
                                                                                                  { "D", new BlockPrimitive() { showInSchedule = true, LongName = "D-Block" }},
                                                                                                  { "E", new BlockPrimitive() { showInSchedule = true, LongName = "E-Block" }},
                                                                                                  { "F", new BlockPrimitive() { showInSchedule = true, LongName = "F-Block" }},
                                                                                                  { "Independent Study", new BlockPrimitive() { showInSchedule = true, LongName = "Independent Study" }},
                                                                                                  { "Sports", new BlockPrimitive() { showInSchedule = true, LongName = "Sports" }},
                                                                                                  { "EAS", new BlockPrimitive() { showInSchedule = true, LongName = "EAS" }},
                                                                                                  { "Study Hall", new BlockPrimitive() { showInSchedule = true, LongName = "Study Hall" }},
                                                                                                  { "Morning Meds", new BlockPrimitive() { showInSchedule = false, LongName = "Morning Meds" }},
                                                                                                  { "Lunch Meds", new BlockPrimitive() { showInSchedule = false, LongName = "Lunch Meds" }},
                                                                                                  { "Dinner Meds", new BlockPrimitive() { showInSchedule = false, LongName = "Dinner Meds" }},
                                                                                                  { "Bedtime Meds", new BlockPrimitive() { showInSchedule = false, LongName = "Bedtime Meds" }},
                                                                                                  { "M-A", new BlockPrimitive() { showInSchedule = true, LongName = "Monday/Wednesday A-Block" } },
                                                                                                  { "M-B", new BlockPrimitive() { showInSchedule = true, LongName = "Monday/Wednesday B-Block" } },
                                                                                                  { "M-C",  new BlockPrimitive() { showInSchedule = true, LongName = "Monday/Wednesday C-Block" }  },
                                                                                                  { "M-D",  new BlockPrimitive() { showInSchedule = true, LongName = "Monday/Wednesday D-Block" }  },
                                                                                                  { "M-E",  new BlockPrimitive() { showInSchedule = true, LongName = "Monday/Wednesday E-Block" }  },
                                                                                                  { "M-F",  new BlockPrimitive() { showInSchedule = true, LongName = "Monday/Wednesday F-Block" }  },
                                                                                                  { "T-A",  new BlockPrimitive() { showInSchedule = true, LongName = "Tuesday/Thursday A-Block" }  },
                                                                                                  { "T-B", new BlockPrimitive() { showInSchedule = true, LongName = "Tuesday/Thursday B-Block" } },
                                                                                                  { "T-C", new BlockPrimitive() { showInSchedule = true, LongName = "Tuesday/Thursday C-Block" } },
                                                                                                  { "T-D", new BlockPrimitive() { showInSchedule = true, LongName = "Tuesday/Thursday D-Block" } },
                                                                                                  { "T-E", new BlockPrimitive() { showInSchedule = true, LongName = "Tuesday/Thursday E-Block" } },
                                                                                                  { "T-F", new BlockPrimitive() { showInSchedule = true, LongName = "Tuesday/Thursday F-Block" } },
                                                                                                  { "MON", new BlockPrimitive() { showInSchedule = true, LongName = "Monday Evening" }},
                                                                                                  { "TUE", new BlockPrimitive() { showInSchedule = true, LongName = "Tuesday Evening" }},
                                                                                                  { "WED", new BlockPrimitive() { showInSchedule = true, LongName = "Wednesday Evening" }},
                                                                                                  { "THU", new BlockPrimitive() { showInSchedule = true, LongName = "Thursday Evening" }}
                                                                                                };


        /// <summary>
        /// Update Standard Blocks for the School Year.  Adds the Long Name.
        /// </summary>
        public static void UpdateBlocks()
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                int year = DateRange.GetCurrentAcademicYear();
                int bid = db.Blocks.Count() > 0? db.Blocks.OrderBy(b => b.id).ToList().Last().id:0;
                foreach(AcademicYear ay in db.AcademicYears.Where(y => y.id >= year).ToList())
                {
                    foreach(String key in StandardBlocks.Keys)
                    {
                        if(ay.Blocks.Where(b => b.Name.Equals(key)).Count() > 0)
                        {
                            Block block = ay.Blocks.Where(b => b.Name.Equals(key)).Single();
                            block.LongName = StandardBlocks[key].LongName;
                        }
                        else
                        {
                            Block block = new Block()
                            {
                                id = ++bid,
                                Name = key,
                                ShowInSchedule = StandardBlocks[key].showInSchedule,
                                AcademicYearID = ay.id,
                                LongName = StandardBlocks[key].LongName
                            };

                            db.Blocks.Add(block);
                        }
                    }
                }

                db.SaveChanges();
            }
        }

        // Grade Tables
        private static Dictionary<String, int> AttendanceMarkings = new Dictionary<String, int> { { "Excused", 0 }, { "Cut", -1 }, { "Present", 1 }, { "Late", 1 } };
        private static Dictionary<String, int> StandardAF = new Dictionary<string, int>() { {"Pass",1}, {"Fail",0}, { "A", 400 }, { "A-", 367 }, { "B+", 333 }, { "B", 300 }, { "B-", 267 }, { "C+", 233 }, { "C", 200 }, { "C-", 167 }, { "D+", 133 }, { "D", 100 }, { "D-", 67 }, { "F", 0 }, {"Not Applicable", Int32.MaxValue} };
        private static Dictionary<String, int> EffortGrades = new Dictionary<string, int>() { { "Exceeds Expectations", 1 }, { "Meets Expectations", 0 }, { "Inconsistent with Expectations", -1 } };
        private static Dictionary<String, int> APApprovals = new Dictionary<string, int>() { { "Approved", 1 }, { "Needs Meeting", 0 }, { "Not Recommended", -1 } };
        private static Dictionary<String, int> CreditTypes = new Dictionary<string, int>() { { "English", 1 }, { "History", 1 }, { "Science", 1 }, { "Mathematics", 1 }, { "World Language", 1 }, { "Arts", 1 }, { "Technology", 1 } };
        private static Dictionary<String, int> CreditValues = new Dictionary<string, int>() { { "One Year", 9 }, { "One Trimester", 3 }, { "Two Trimester", 6 }, { "1/3 Trimester", 1 }, { "No Credit", 0 } };

        private static Dictionary<String, Dictionary<string, int>> Tables = new Dictionary<string, Dictionary<string, int>>() { { "Attendance", AttendanceMarkings }, { "Standard A-F Scale", StandardAF }, { "Effort Grades", EffortGrades }, { "AP Approval", APApprovals }, { "Credit Types", CreditTypes }, { "Credit Values", CreditValues } };

        // Duty Teams
        private static List<String> DutyTeams = new List<string>() { "Truth", "Courage", "Moxie", "Shamrock" };

        private static List<String> TermNames = new List<string>() { "Summer", "Fall", "Winter", "Spring" };

        #endregion //Standard values

        #region Helper Methods

        /// <summary>
        /// Get students flagged as currently active.
        /// </summary>
        /// <returns></returns>
        public static List<int> ActiveStudents()
        {
            using(WebhostEntities db = new WebhostEntities())
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
            using(WebhostEntities db = new WebhostEntities())
            {
                if (db.Terms.Where(t => t.id == termid).Count() <= 0) termid = GetCurrentOrLastTerm();

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

        /// <summary>
        /// Get Faculty flagged as currently Active
        /// </summary>
        /// <returns></returns>
        public static List<int> ActiveTeachers()
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                return db.Faculties.Where(s => s.isActive).Select(s => s.ID).ToList();
            }
        }

        /// <summary>
        /// Get Faculty with active classes in the given term.
        /// </summary>
        /// <param name="termid"></param>
        /// <returns></returns>
        public static List<int> ActiveTeachers(int termid)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                if (db.Terms.Where(t => t.id == termid).Count() <= 0) termid = GetCurrentOrLastTerm();

                List<int> ids = new List<int>();
                foreach(Faculty faculty in db.Faculties.ToList())
                {
                    if (faculty.Sections.Count <= 0) continue;

                    foreach (Section section in faculty.Sections)
                    {
                        if (section.Terms.Where(t => t.id == termid).Count() > 0)
                        {
                            ids.Add(faculty.ID);
                            break;
                        }
                    }
                }

                return ids;
            }
        }

        public static String GetNewUniqueUserName(String FirstName, String LastName, bool isStudent = true, List<String> newUserNames = null)
        {
            string last = LastName.ToLower();
            if (last.Contains("-"))
                last = last.Substring(0, last.IndexOf('-'));

            string first = FirstName.ToLower();

            Regex notAlpha = new Regex("[^a-z]");
            foreach(Match match in notAlpha.Matches(last))
            {
                last = last.Replace(match.Value, "");
            }


            foreach(Match match in notAlpha.Matches(first))
            {
                first = first.Replace(match.Value, "");
            }
                
            string finit = first.Substring(0,1);
            string uname = finit + (isStudent ? "_" : "") + last;

            using (WebhostEntities db = new WebhostEntities())
            {
                List<String> existingUserNames = new List<string>();
                if (newUserNames != null)
                    existingUserNames.AddRange(newUserNames);
                if (isStudent)
                {
                    existingUserNames.AddRange(db.Students.Select(s => s.UserName).ToList());
                    int len = 1;
                    while (existingUserNames.Contains(uname) && len <= FirstName.Length)
                    {
                        finit = first.Substring(0, ++len);
                        uname = finit + "_" + last;
                    }
                }
                else
                {
                    existingUserNames.AddRange(db.Faculties.Select(s => s.UserName).ToList());
                    int len = 1;
                    while (existingUserNames.Contains(uname) && len <= FirstName.Length)
                    {
                        finit = first.Substring(0, ++len);
                        uname = finit + last;
                    }
                }
            }
            return uname;
        }

        /// <summary>
        /// Convert Grade Level to Academic Year.
        /// Relative to the current year.  
        /// 
        /// If you're using this in the summer, make sure that 
        /// Student grade level promotions have been done in 
        /// Blackbaud before importing.
        /// </summary>
        /// <param name="grade"></param>
        /// <returns></returns>
        public static int CurrentGradeLevelToGradYear(int grade)
        {
            int year = GetCurrentAcademicYear();
            year += 3 - (grade - 9);
            return year;
        }

        /// <summary>
        /// Gets the current Academic Year.
        /// Year break is 7 June, since Graduation is in the First Week of June at the latest.
        /// By design, this is also the UniqueID for the Current Academic year Object in the Database.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentAcademicYear()
        {
            return DateTime.Today.Year + (DateTime.Today.Month > 6 || (DateTime.Today.Month == 6 && DateTime.Today.Day > 7) ? 1 : 0);
        }

        /// <summary>
        /// Gets the currently active term (or the term that just ended, if no term is active.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentOrLastTerm()
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                foreach(Term term in db.Terms)
                {
                    if ((new DateRange(term.StartDate, term.EndDate)).Contains(DateTime.Today)) 
                        return term.id;
                }
                                 
                // Find the Last Term that Ended.
                return db.Terms.Where(t => t.EndDate < DateTime.Today).OrderBy(t => t.EndDate).ToList().Last().id;
            }
        }

        /// <summary>
        /// Look up a term by name and year.
        /// 
        /// Throws exception if no matches, or multiple matches.
        /// </summary>
        /// <param name="name">Term Name as stored in the Database.</param>
        /// <param name="year">Academic Year index (graduation year)</param>
        /// <returns>Term.id</returns>
        /// <exception cref="ImportException">When no matches, or many matches.</exception>
        public static int GetTermByName(String name, int year)
        {
            if(name.Contains("Trimester"))
            {
                name = name.Split(' ')[0];
            }
            using(WebhostEntities db = new WebhostEntities())
            {
                var matches = db.Terms.Where(t => t.Name.Equals(name) && t.AcademicYearID == year);

                if (matches.Count() == 1) return matches.Single().id;

                else if(matches.Count() == 0)
                {
                    log.WriteLine("Cannot find match for {0} {1}", name, year);
                    throw new ImportException(String.Format("No Term exists corresponding to {0} {1}", name, year));
                }
                else
                {
                    log.WriteLine("Multiple Matches for {0} {1}.  Something is wrong....", name, year);
                    throw new ImportException(String.Format("Multiple Matches for {0} {1}.  Something is wrong....", name, year));
                }
            }
        }

        /// <summary>
        /// Get the next term in this Academic Year.
        /// 
        /// If no such term, throws WebhostException.
        /// </summary>
        /// <param name="termId"></param>
        /// <returns></returns>
        /// <exception cref="WebhostException">This is the last term</exception>
        public static int GetNextTerm(int termId)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Term term = db.Terms.Where(t => t.id == termId).Single();
                var after = db.Terms.Where(t => t.AcademicYearID == term.AcademicYearID && t.StartDate > term.EndDate).OrderBy(t => t.StartDate);
                if (after.Count() > 0) return after.ToList()[0].id;

                throw new WebhostException(String.Format("There is no term after {0} in {1}", term.Name, term.AcademicYearID));
            }
        }

        /// <summary>
        /// Get the previous term in this Academic Year.
        /// 
        /// If no such term, throws WebhostException.
        /// </summary>
        /// <param name="termId"></param>
        /// <returns></returns>
        /// <exception cref="WebhostException">This is the first term</exception>
        public static int GetPreviousTerm(int termId)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Term term = db.Terms.Where(t => t.id == termId).Single();
                var after = db.Terms.Where(t => t.AcademicYearID == term.AcademicYearID && t.EndDate < term.StartDate).OrderByDescending(t => t.StartDate);
                if (after.Count() > 0) return after.ToList()[0].id;

                throw new WebhostException(String.Format("There is no term after {0} in {1}", term.Name, term.AcademicYearID));
            }
        }

        /// <summary>
        /// Look up block by name and year.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static int GetBlockByName(String name, int year)
        {
            if (name.Length == 1 || name.Contains('-'))
                name = name.ToUpper();

            if (name.Equals("SP")) name = "Sports";

            Regex lab = new Regex("[A-Z]-[a-z]");
            if (lab.IsMatch(name))
                name = "Independent Study";

            if (name.Equals("")) name = "Independent Study";
            using(WebhostEntities db = new WebhostEntities())
            {
                var matches = db.Blocks.Where(b => b.AcademicYearID == year && b.Name.Equals(name));
                if (matches.Count() == 1) return matches.Single().id;
                else if(matches.Count() == 0)
                {
                    log.WriteLine("No Block named {0} in {1}", name, year);
                    throw new WebhostException(String.Format("No Block named {0} in {1}", name, year));
                }
                else
                {
                    log.WriteLine("Too Many Blocks named {0} in {1}", name, year);
                    throw new WebhostException(String.Format("Too Many Blocks named {0} in {1}", name, year));
                }
            }
        }

        private static Log _log;

        /// <summary>
        /// Separate Log File for imports.
        /// </summary>
        private static Log log
        {
            get
            {
                if (HttpContext.Current == null)
                {
                    if(_log == null)
                        _log = new Log("import_log");

                    return _log;
                }
                if (HttpContext.Current.Session["ImportLog"] == null)
                    HttpContext.Current.Session["ImportLog"] = new Log("import_log", HttpContext.Current.Server);
                return (Log)HttpContext.Current.Session["ImportLog"];
            }
        }
        #endregion  // Helper Methods
        
        #region Academic Year Init

        /// <summary>
        /// This Method is called by the CreateAcademicYear method and should not be used alone.
        /// All Terms need to be connected to an AcademicYear!
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="dates">DateRange for the Start and End of the term.</param>
        /// <param name="comments">Date for Comments to be Published</param>
        /// <returns>id of the newly created Term for use in the CreateAcademicYear method.</returns>
        /// <exception cref="ImportException">Throws an ImportException if the date range overlaps with an existing Term</exception>
        private static int CreateTerm(String name, DateRange dates, DateTime comments)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                log.WriteLine("Creating Term {0}, {1}", name, dates);
                // Check for overlaping terms.
                int update = -1;

                foreach(Term existingTerm in db.Terms)
                {
                    DateRange range = new DateRange(existingTerm.StartDate, existingTerm.EndDate);
                    if (range.Intersects(dates))
                    {
                        log.WriteLine("Cannot Create Term.\r\nNew Term Date Range {0} overlaps with existing Term id={2} ranging {1}.", dates, range, existingTerm.id);
                        update = existingTerm.id;
                    }
                }

                if(update > -1)
                {
                    Term term = db.Terms.Where(t => t.id == update).Single();

                    term.Name = name;
                    term.StartDate = dates.Start;
                    term.EndDate = dates.End;
                    term.CommentsDate = comments;

                    db.SaveChanges();
                    log.WriteLine("Saved Updated Term.  Throwing exception back to Academic Year.");
                    throw new ImportException(String.Format("Updated term id={0}", term.id));
                }

                Term newTerm = new Term()
                {
                    id = db.Terms.Count() > 0 ? db.Terms.OrderBy(t => t.id).ToList().Last().id + 1 : 0,
                    Name = name,
                    StartDate = dates.Start,
                    EndDate = dates.End,
                    CommentsDate = comments
                };

                db.Terms.Add(newTerm);
                db.SaveChanges();
                log.WriteLine("Saved New Term to Database.");
                return newTerm.id;
            }
        }

        /// <summary>
        /// Create new AcademicYears and all associated Terms.
        /// 
        /// CSV spreadsheet may contain data for multiple years!
        /// 
        /// Requires CSV Data in the following format:
        /// 
        /// requiredFields = new List<String>() {
        ///            "graduation date", 
        ///            "Fall start date", "Fall end date", "Fall comments", 
        ///            "Winter start date", "Winter end date", "Winter comments", 
        ///            "Spring start date", "Spring end date", "Spring comments", 
        ///            "Summer start date", "Summer end date", "Summer comments"};
        /// 
        /// </summary>
        /// <param name="data"></param>
        public static void CreateAcademicYear(CSV data)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                log.WriteLine("Beginning Academic Year Import.");
                List<String> requiredFields = new List<String>() {
                    "graduation date", 
                    "Fall start date", "Fall end date", "Fall comments", 
                    "Winter start date", "Winter end date", "Winter comments", 
                    "Spring start date", "Spring end date", "Spring comments", 
                    "Summer start date", "Summer end date", "Summer comments"};

                foreach(Dictionary<String,String> row in data.Data)
                {
                    foreach(String reqField in requiredFields)
                    {
                        if (!row.ContainsKey(reqField))
                        {
                            log.WriteLine("Missing field {0} from AcademicYear Import CSV.\r\nAborting.", reqField);
                            throw new ImportException(String.Format("Missing field {0} from AcademicYear Import CSV.", reqField)); 
                        }
                    }

                    DateTime gradDate = DateRange.GetDateTimeFromString(row["graduation date"]);
                    bool update = false;
                    if (db.AcademicYears.Where(y => y.id == gradDate.Year).Count() > 0)
                    {

                        log.WriteLine("Academic Year {0} already Exists.\r\nUpdating.", gradDate.Year);
                        update = true;
                    }

                    AcademicYear year = new AcademicYear()
                    {
                        id = gradDate.Year
                    };

                    if(update)
                    {
                        year = db.AcademicYears.Where(y => y.id == gradDate.Year).Single();
                    }

                    // Setup Terms!

                    foreach(String termName in TermNames)
                    {
                        try
                        {
                            log.WriteLine("Attempting to create {0} term.", termName);
                            int id = CreateTerm(termName, 
                                                new DateRange(DateRange.GetDateTimeFromString(row[termName + " start date"]), 
                                                              DateRange.GetDateTimeFromString(row[termName + " end date"])), 
                                                DateRange.GetDateTimeFromString(row[termName + " comments"]));

                            Term term = db.Terms.Where(t => t.id == id).Single();
                            year.Terms.Add(term);

                        }
                        catch (ImportException e)
                        {
                            // Capture the already existing Term!
                            log.WriteLine("Caught exception:  " + e.Message);
                            Regex idex = new Regex(@"id=\d+");
                            if (idex.IsMatch(e.Message))
                            {
                                String str = idex.Match(e.Message).ToString();
                                str = str.Substring(3);
                                int id = Convert.ToInt32(str);
                                Term term = db.Terms.Where(t => t.id == id).Single();
                                year.Terms.Add(term);
                                log.WriteLine("Captured already created term:  {0}", id);
                            }

                            else
                            {
                                // Failed to capture term.
                                log.WriteLine("Aborting.");
                                throw e;
                            }
                        }
                    }

                    // Finished Creating Terms

                    if (!update) db.AcademicYears.Add(year);
                    db.SaveChanges();
                    log.WriteLine("Academic Year {0} Saved.", year.id);
                    InitializeBlocks(year.id);
                }
            }
        }

        /// <summary>
        /// Copy Permissions from one academic year to the next.
        /// </summary>
        /// <param name="AcademicYearId"></param>
        public static void CopyPermissions(int AcademicYearId)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                int id = db.Permissions.OrderBy(p => p.id).ToList().Last().id;
                log.WriteLine("Copying Permissions from {0} to {1}", AcademicYearId - 1, AcademicYearId);
                foreach(Permission perm in db.Permissions.Where(p => p.AcademicYear == AcademicYearId-1).ToList())
                {
                    log.WriteLine("Examining permission:  {0}", perm.Name);
                    if(db.Permissions.Where(p => p.AcademicYear == AcademicYearId && p.Name.Equals(perm.Name)).Count() <=0)
                    {
                        Permission copy = new Permission()
                        {
                            id = ++id,
                            AcademicYear = AcademicYearId,
                            Name = perm.Name,
                            Description = perm.Description,
                            WebPages = perm.WebPages,
                            Students = perm.Students,
                            Faculties = perm.Faculties
                        };

                        log.WriteLine("Copied {0} to {1}", copy.Name, copy.AcademicYear);
                        log.WriteLine("There are {0} students and {1} faculty associated with this permission.", copy.Students.Count, copy.Faculties.Count);
                        db.Permissions.Add(copy);
                    }
                }
                log.WriteLine("Copy Complete.");
                db.SaveChanges();
            }
        }

        /// <summary>
        /// To Change the Blocks Created for the new academic year, edit the static field StandardBlocks.
        /// This method is called after the completion of the AcademicYear Initialization.  However, it may be called independently.
        /// </summary>
        /// <param name="AcademicYearId"></param>
        public static void InitializeBlocks(int AcademicYearId)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                log.WriteLine("Initializing Blocks for {0}", AcademicYearId);
                AcademicYear year = db.AcademicYears.Where(y => y.id == AcademicYearId).Single();
                int lastId = db.Blocks.Count() > 0 ? db.Blocks.OrderBy(b => b.id).ToList().Last().id : -1;
                foreach(string blockName in StandardBlocks.Keys)
                {
                    if(year.Blocks.Where(b => b.Name.Equals(blockName)).Count() > 0)
                    {
                        log.WriteLine("Block already exists.");
                        continue;
                    }

                    Block block = new Block()
                    {
                        id = ++lastId,
                        Name = blockName,
                        AcademicYearID = AcademicYearId,
                        ShowInSchedule = StandardBlocks[blockName].showInSchedule,
                        LongName=StandardBlocks[blockName].LongName
                    };
                    log.WriteLine("Created [{0}]", block.LongName);
                    db.Blocks.Add(block);
                }

                db.SaveChanges();
                log.WriteLine("New Blocks Saved to Database.");
            }
        }
        
        public static void InitializeGradeTablesMarkings(int AcademicYearId)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                int gradeTableId = db.GradeTables.Count() > 0 ? db.GradeTables.OrderBy(gt => gt.id).ToList().Last().id : 0;
                int entryId = db.GradeTableEntries.Count() > 0 ? db.GradeTableEntries.OrderBy(gte => gte.id).ToList().Last().id : 0;

                AcademicYear year = db.AcademicYears.Where(y => y.id == AcademicYearId).Single();

                foreach(String tableName in Tables.Keys)
                {
                    if (year.GradeTables.Where(t => t.Name.Equals(tableName)).Count() > 0) continue;

                    GradeTable table = new GradeTable()
                    {
                        id = ++gradeTableId,
                        Name = tableName,
                        AcademicYearID = AcademicYearId
                    };

                    db.GradeTables.Add(table);

                    foreach(String entryName in Tables[tableName].Keys)
                    {
                        GradeTableEntry entry = new GradeTableEntry()
                        {
                            id = ++entryId,
                            Name = entryName,
                            Value = Tables[tableName][entryName],
                            GradeTableID = gradeTableId
                        };

                        db.GradeTableEntries.Add(entry);
                    }
                }

                db.SaveChanges();
            }
        }
        #endregion  // Academic Year Init

        #region User Imports

        public static void DutyTeamRosters(CSV csv)
        {
            int year = DateRange.GetCurrentAcademicYear();
            using(WebhostEntities db = new WebhostEntities())
            {
                int tid = db.DutyTeams.Count() > 0? db.DutyTeams.OrderBy(t => t.id).ToList().Last().id:0;
                foreach(String teamName in DutyTeams)
                {
                    if(db.DutyTeams.Where(t => t.AcademicYearID == year && t.Name.Equals(teamName)).Count() <= 0)
                    {
                        DutyTeam team = new DutyTeam()
                        {
                            id = ++tid,
                            Name = teamName,
                            AcademicYearID = year,
                            AOD = 67,
                            DTL = 67
                        };

                        db.DutyTeams.Add(team);
                    }
                }
                List<DutyTeam> teams = db.DutyTeams.Where(t => t.AcademicYearID == year).ToList();

                foreach(DutyTeam team in teams)
                {
                    team.Members.Clear();
                }

                foreach(Dictionary<String, String> row in csv.Data)
                {
                    String firstName = row["firstName"];
                    String lastName = row["lastName"];
                    String teamName = row["team"];
                    bool isAOD = row.ContainsKey("AOD") && row["AOD"].Equals("X");
                    bool isDTL = row.ContainsKey("DTL") && row["DTL"].Equals("X");
                    Faculty member = db.Faculties.Where(f => f.FirstName.Equals(firstName) && f.LastName.Equals(lastName)).Single();
                    foreach(DutyTeam team in teams)
                    {
                        if(team.Name.Equals(teamName))
                        {
                            team.Members.Add(member);
                            if(isDTL)
                            {
                                team.DTL = member.ID;
                            }
                            if(isAOD)
                            {
                                team.AOD = member.ID;
                            }
                        }
                    }
                }

                db.SaveChanges();
            }
        }

        public struct DormStudyHall
        {
            public Dorm dorm;
            public Section studyHall;
        }

        public static void DormRosters(CSV csv)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                int year = DateRange.GetCurrentAcademicYear();
                Dictionary<String, DormStudyHall> Dorms = new Dictionary<string, DormStudyHall>();

                List<Section> studyHalls = db.Sections.Where(sec => sec.Block.Name.Equals("Study Hall") && sec.Course.AcademicYearID == year).ToList();
                List<Dorm> dorms = db.Dorms.Where(d => d.AcademicYearId == year).ToList();

                foreach(String name in DormNames)
                {
                    Dorm dorm = new Dorm();
                    Section studyHall = new Section();
                    if (dorms.Where(d => d.Name.Contains(name)).Count() > 0)
                        dorm = dorms.Where(d => d.Name.Contains(name)).Single();
                    else
                    {
                        log.WriteLine("No Dorm matches {0}", name);
                        throw new ImportException("No dorm matches " + name);
                    }

                    if (studyHalls.Where(sec => sec.Course.Name.Contains(name)).Count() == 1)
                        studyHall = studyHalls.Where(sec => sec.Course.Name.Contains(name)).Single();
                    else
                    {
                        log.WriteLine("found {0} matches for {1} dorm study hall.", studyHalls.Where(sec => sec.Course.Name.Contains(name)).Count());
                        throw new ImportException("Could not match Study Hall for " + name);
                    }

                    dorm.Students.Clear();
                    studyHall.Students.Clear();

                    Dorms.Add(name, new DormStudyHall() { dorm = dorm, studyHall = studyHall });
                }

                List<String> requiredFields = new List<string>() { Dorm_LastName, Dorm_FirstName, Dorm_Dorm };

                foreach(Dictionary<String,String> row in csv.Data)
                {
                    foreach(String fld in requiredFields)
                        if(!row.Keys.Contains(fld))
                        {
                            log.WriteLine("Row is missing field {0}", fld);
                            throw new ImportException("Missing Field from import CSV.");
                        }

                    string fn = row[Dorm_FirstName];
                    string ln = row[Dorm_LastName];

                    List<Student> matches = db.Students.Where(s => s.isActive && s.FirstName.Equals(fn) && s.LastName.Equals(ln)).ToList();

                    if(matches.Count <= 0)
                    {
                        log.WriteLine("No student matches {0} {1}", row[Dorm_FirstName], row[Dorm_LastName]);
                        continue;
                    }
                    else if(matches.Count > 1)
                    {
                        log.WriteLine("Non-unique student name! {0} {1}.  Must be added manually.", row[Dorm_FirstName], row[Dorm_LastName]);
                        continue;
                    }

                    Student student = matches.Single();

                    Dorms[row[Dorm_Dorm]].dorm.Students.Add(student);
                    Dorms[row[Dorm_Dorm]].studyHall.Students.Add(student);
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// Create a new Faculty account along with Google Things.
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="passwd"></param>
        public static void NewFaculty(int employeeId, String firstName, String lastName, String primaryGroup = "Faculty", List<String> AdditionalGroups = null)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                if (db.Faculties.Where(f => f.ID == employeeId).Count() > 0)
                    throw new ImportException("Faculty with that ID Already Exists!");

                Faculty newFaculty = new Faculty()
                {
                    ID = employeeId,
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = GetNewUniqueUserName(firstName, lastName, false),
                    isActive = true,
                    SignatureData = new byte[0],
                    PhoneNumber = ""
                };
                db.Faculties.Add(newFaculty);

                String pwd = AccountManagement.AccountManagement.GenerateRandomPassword();
                bool adsuccess = true, googlesuccess = true;
                if(!AccountManagement.AccountManagement.ADUserExists(newFaculty.UserName))
                {
                    try
                    {
                        AccountManagement.AccountManagement.CreateADUser(new AccountManagement.AccountManagement.ADUserTemplate()
                            {
                                FirstName = newFaculty.FirstName,
                                LastName = newFaculty.LastName,
                                Password = pwd,
                                PrimaryGroup = primaryGroup,
                                AdditionalGroups = AdditionalGroups == null ? new List<String>() : AdditionalGroups,
                                UserName = newFaculty.UserName,
                                Description = ""
                            });
                    }
                    catch(Exception e)
                    {
                        String message = e.Message;
                        while(e.InnerException != null)
                        {
                            e = e.InnerException;
                            message += Environment.NewLine + e.Message;
                        }

                        log.WriteLine(message);
                        adsuccess = false;
                    }
                }
                else
                {
                    adsuccess = false;
                }

                try
                {
                    using (GoogleDirectoryCall call = new GoogleDirectoryCall())
                    {
                        call.CreateEmail(newFaculty.FirstName, newFaculty.LastName, newFaculty.UserName, pwd, primaryGroup.ToLower());
                        log.WriteLine("Created new faculty email.");
                        try
                        {
                            call.AddUserToGroup(newFaculty.UserName, "faculty@dublinschool.org");
                            call.AddUserToGroup(newFaculty.UserName, "all-school@dublinschool.org");
                        }
                        catch (Exception e)
                        {
                            log.WriteLine("Failed to add user to group: {0}", e.Message);
                        }
                    }
                }
                catch (GoogleAPICall.GoogleAPIException e)
                {
                    log.WriteLine(e.Message);
                    googlesuccess = false;
                }

                if(adsuccess || googlesuccess)
                {
                    String Message = String.Format("Created new accounts in: {0}{1} for {2}, password:  {3}", adsuccess ? " AD" : "", googlesuccess ? " GMail" : "", newFaculty.UserName, pwd);
                    NewFacultyAccountLetter letter = new NewFacultyAccountLetter()
                    {
                        FirstName = newFaculty.FirstName,
                        Email = String.Format("{0}@dublinschool.org", newFaculty.UserName),
                        Password = pwd,
                        Title = string.Format("Welcome, {0} {1}", newFaculty.FirstName, newFaculty.LastName)
                    };

                    Document doc = letter.Publish();
                    String path = String.Format("C:\\Temp\\{0}.pdf", newFaculty.UserName);
                    if(System.Web.HttpContext.Current != null)
                    {
                        path = HttpContext.Current.Server.MapPath(String.Format("~/Temp/letters/{0}.pdf", newFaculty.UserName));
                    }

                    doc.Save(path);

                    MailControler.MailToWebmaster("New User Account", Message);
                    
                }

                db.SaveChanges();
                
                using(GoogleCalendarCall call = new GoogleCalendarCall())
                {
                    call.UpdateCalendarsForUser(newFaculty.ID, true);
                }
            }

        }

        public static void Faculty(CSV csv)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                List<String> newUserNames = new List<string>();
                List<String> requiredKeys = new List<string>() { Faculty_EmpID, Faculty_FirstName, Faculty_LastName };
                foreach (Dictionary<string, string> row in csv.Data)
                {
                    foreach (String reqf in requiredKeys)
                        if (!row.Keys.Contains(reqf)) throw new ImportException(String.Format("Missing required field {0} in Faculty Import", reqf));

                    int id = Convert.ToInt32(row[Faculty_EmpID]);
                    Faculty newFaculty = new Faculty();
                    bool update = false;
                    if (update = (db.Faculties.Where(fac => fac.ID == id).Count() > 0))
                        newFaculty = db.Faculties.Where(fac => fac.ID == id).Single();

                    newFaculty.FirstName = row[Faculty_FirstName];
                    newFaculty.LastName = row[Faculty_LastName];
                    if (!update)
                    {
                        newFaculty.ID = id;
                        newFaculty.isActive = true;
                        newFaculty.UserName = GetNewUniqueUserName(newFaculty.FirstName, newFaculty.LastName, false, newUserNames);
                        newUserNames.Add(newFaculty.UserName);
                        newFaculty.SignatureData = new byte[0];
                        newFaculty.PhoneNumber = "";
                        db.Faculties.Add(newFaculty);
                        try
                        {
                            using (GoogleDirectoryCall call = new GoogleDirectoryCall())
                            {
                                using (GoogleCalendarCall ccall = new GoogleCalendarCall())
                                {
                                    call.CreateEmail(newFaculty.FirstName, newFaculty.LastName, newFaculty.UserName, AccountManagement.AccountManagement.GenerateRandomPassword(), "faculty");
                                    log.WriteLine("Created new faculty email.");
                                    try
                                    {
                                        call.AddUserToGroup(newFaculty.UserName, "faculty@dublinschool.org");
                                        call.AddUserToGroup(newFaculty.UserName, "all-school@dublinschool.org");
                                    }
                                    catch (Exception e)
                                    {
                                        log.WriteLine("Failed to add user to group: {0}", e.Message);
                                    }

                                    foreach (GoogleCalendar gcal in db.GoogleCalendars.Where(c => !c.FacultyPermission.Equals("none")).ToList())
                                    {
                                        try
                                        {
                                            ccall.AddUserToCalendar(newFaculty.UserName, gcal.CalendarId, gcal.FacultyPermission);
                                        }
                                        catch (Exception e)
                                        {
                                            log.WriteLine("Failed to add user to {1}: {0}", e.Message, gcal.CalendarName);
                                        }
                                    }
                                }
                            }
                        }
                        catch (GoogleAPICall.GoogleAPIException e)
                        {
                            log.WriteLine(e.Message);
                        }
                    }

                    if(row.ContainsKey("Phone"))
                    {
                        newFaculty.PhoneNumber = row["Phone"];
                    } 
                    
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// Automatically Generates New Student Letters to be mailed.
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="LetterRootPath"></param>
        /// <returns></returns>
        public static CSV SetNewStudentPasswords(CSV csv, String LetterRootPath = "~/Temp/")
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                CSV output = new CSV();

                if (LetterRootPath.StartsWith("~"))
                    LetterRootPath = HttpContext.Current.Server.MapPath(LetterRootPath);

                if (!Directory.Exists(LetterRootPath + "letters")) Directory.CreateDirectory(LetterRootPath + "letters");


                foreach(Dictionary<String, String> row in csv.Data)
                {
                    int id = Convert.ToInt32(row[Student_StudentID]);
                    Student student = db.Students.Where(s => s.ID == id).Single();
                    log.WriteLine("Setting Password for {0} {1} ({2})", student.FirstName, student.LastName, student.GraduationYear);
                    String newPassword = AccountManagement.AccountManagement.GenerateRandomPassword();
                    bool adsuccess = true, googlesuccess = true;
                    try
                    {
                        AccountManagement.PasswordReset.ForceChangeADPassword(student.UserName, newPassword);
                        AccountManagement.PasswordReset.ForceChangeGooglePassword(student.UserName, newPassword);
                    }
                    catch(AccountManagement.PasswordReset.PasswordException pwe)
                    {
                        log.WriteLine(pwe.Message);
                        try
                        {
                            AccountManagement.AccountManagement.CreateADUser(new AccountManagement.AccountManagement.ADUserTemplate()
                                {
                                    FirstName = student.FirstName,
                                    LastName = student.LastName,
                                    Description = Convert.ToString(student.GraduationYear),
                                    Password = newPassword,
                                    PrimaryGroup = "Students",
                                    AdditionalGroups = new List<string>(),
                                    UserName = student.UserName
                                });
                        }
                        catch (Exception fe)
                        {
                            log.WriteLine(fe.Message);
                            adsuccess = false;
                        }
                    }
                    catch(GoogleAPICall.GoogleAPIException gae)
                    {
                        log.WriteLine(gae.Message);
                        googlesuccess = false;
                    }

                    output.Add(new Dictionary<string, string>()
                        {
                            {"firstName", student.FirstName},
                            {"lastName", student.LastName},
                            {"userName", student.UserName},
                            {"password", newPassword},
                            {"primaryGroup","Students"},
                            {"description", Convert.ToString(student.GraduationYear)},
                            {"Email", string.Format("{0}@dublinschool.org", student.UserName)},
                            {"AD Failed", adsuccess?"":"X"},
                            {"Google Failed", googlesuccess?"":"X"}
                        });


                    NewStudentRegistrationLetter letter = new NewStudentRegistrationLetter()
                    {
                        Title = "Welcome Letter",
                        FirstName = student.FirstName,
                        Email = string.Format("{0}@dublinschool.org", student.UserName),
                        Password = newPassword
                    };


                    letter.Publish().Save(string.Format("{0}letters/{1} {2} Welcome.pdf", LetterRootPath, student.FirstName, student.LastName));
                }

                return output;
            }
        }

        protected static int ConvertGradeNameToGraduationYear(String name)
        {
            List<String> grades = new List<string>() { "Twelfth", "Eleventh", "Tenth", "Ninth" };

            int year = DateRange.GetCurrentAcademicYear() + grades.IndexOf(name);

            return year;
        }

        public static void Students(CSV csv, String LetterRootPath = "~/Temp/")
        {

            if (LetterRootPath.StartsWith("~"))
                LetterRootPath = HttpContext.Current.Server.MapPath(LetterRootPath);

            if (!Directory.Exists(LetterRootPath + "letters")) Directory.CreateDirectory(LetterRootPath + "letters");

            using (WebhostEntities db = new WebhostEntities())
            {
                List<String> newUserNames = new List<string>();
                log.WriteLine("Beginning Student Import.");
                List<String> requiredKeys = new List<string>() { Student_AdvisorID, Student_CurrentGrade, Student_FirstName, Student_LastName, Student_StudentID };
                foreach (Dictionary<string, string> row in csv.Data)
                {
                    foreach (String reqf in requiredKeys)
                        if (!row.Keys.Contains(reqf))
                        {
                            log.WriteLine("Missing required field {0} in Student Import\r\nAborting.", reqf); 
                            throw new ImportException(String.Format("Missing required field {0} in Student Import", reqf));
                        }

                    int id = Convert.ToInt32(row[Student_StudentID]);
                    Student newStudent = new Student();
                    bool update = false;
                    if (update = (db.Students.Where(fac => fac.ID == id).Count() > 0))  // yay C
                    {
                        newStudent = db.Students.Where(fac => fac.ID == id).Single();
                        log.WriteLine("Student already exists.  Updating {0} {1}", newStudent.FirstName, newStudent.LastName);
                    }

                    newStudent.BlackbaudId = row[Student_StudentID];
                    newStudent.FirstName = row[Student_FirstName];
                    newStudent.LastName = row[Student_LastName];
                    newStudent.GraduationYear = ConvertGradeNameToGraduationYear(row[Student_CurrentGrade]);
                    newStudent.AcademicLevel = 0;
                    int adId = row[Student_AdvisorID].Equals("") ? 67 : Convert.ToInt32(row[Student_AdvisorID]);
                    if (db.Faculties.Where(f => f.ID == adId).Count() > 0)
                        newStudent.AdvisorID = adId;
                    else
                    {
                        log.WriteLine("Invalid Advisor Id {0}.  Assigning Jason!", adId);
                        newStudent.AdvisorID = 67;
                    }
                    if (!update)
                    {
                        newStudent.UserName = GetNewUniqueUserName(newStudent.FirstName, newStudent.LastName, true, newUserNames);
                        newStudent.ID = id;
                        newStudent.isActive = true;
                        db.Students.Add(newStudent);
                        newUserNames.Add(newStudent.UserName);
                        String pwd = AccountManagement.AccountManagement.GenerateRandomPassword();
                        if(!AccountManagement.AccountManagement.ADUserExists(newStudent.UserName))
                        {
                            AccountManagement.AccountManagement.CreateADUser(new AccountManagement.AccountManagement.ADUserTemplate()
                                {
                                    FirstName = newStudent.FirstName,
                                    LastName = newStudent.LastName,
                                    Description = Convert.ToString(newStudent.GraduationYear),
                                    UserName = newStudent.UserName,
                                    AdditionalGroups = new List<string>() { "FW-Internet" },
                                    PrimaryGroup = "Students",
                                    Password = pwd
                                });
                            log.WriteLine("Created new AD User {0}, {1}", newStudent.UserName, pwd);
                        }
                        try
                        {
                            using (GoogleDirectoryCall call = new GoogleDirectoryCall())
                            {
                                using (GoogleCalendarCall ccall = new GoogleCalendarCall())
                                {
                                    call.CreateEmail(newStudent.FirstName, newStudent.LastName, newStudent.UserName, pwd, "students");
                                    log.WriteLine("Created new student email.");
                                    try
                                    {
                                        call.AddUserToGroup(newStudent.UserName, "all-school@dublinschool.org");
                                    }
                                    catch (Exception e)
                                    {
                                        log.WriteLine("Failed to add user to group: {0}", e.Message);
                                    }

                                    foreach (GoogleCalendar gcal in db.GoogleCalendars.Where(c => !c.StudentPermission.Equals("none")).ToList())
                                    {
                                        try
                                        {
                                            ccall.AddUserToCalendar(newStudent.UserName, gcal.CalendarId, gcal.StudentPermission);
                                        }
                                        catch (Exception e)
                                        {
                                            log.WriteLine("Failed to add user to {1}: {0}", e.Message, gcal.CalendarName);
                                        }
                                    }
                                }
                            }
                        }
                        catch (GoogleAPICall.GoogleAPIException e)
                        {
                            log.WriteLine(e.Message);
                        }

                        log.WriteLine("Created new Student:  {0} {1}", newStudent.FirstName, newStudent.LastName);


                        NewStudentRegistrationLetter letter = new NewStudentRegistrationLetter()
                        {
                            Title = "Welcome Letter",
                            FirstName = newStudent.FirstName,
                            Email = string.Format("{0}@dublinschool.org", newStudent.UserName),
                            Password = pwd
                        };

                        letter.Publish().Save(string.Format("{0}letters/{1} {2} Welcome.pdf", LetterRootPath, newStudent.FirstName, newStudent.LastName));
                        WebhostEventLog.Syslog.LogInformation("Created pdf welcome letter for {0} {1}.", newStudent.FirstName, newStudent.LastName);
                    } 
                    
                }

                db.SaveChanges();
                log.WriteLine("New Students Saved to Database.");
            }
        }

        public static String SchoologyExportPack(int year, int termId, HttpServerUtility Server)
        {
            List<String> fileNames = new List<string>();

            Dictionary<String, CSV> files = new Dictionary<string, CSV>()
            {
                { "~/Temp/schoology_usrs.csv", SchoologyExport.Users(year) },
                { "~/Temp/full_year_classes.csv", SchoologyExport.FullYearCourses(termId) },
                { "~/Temp/two_term_classes.csv", SchoologyExport.StartingTwoTermCourses(termId) },
                { "~/Temp/trimester_electives.csv", SchoologyExport.TrimesterElectives(termId) },
                { "~/Temp/advisors.csv", SchoologyExport.AdvisorList(termId) },
                { "~/Temp/enrollment.csv", SchoologyExport.Enrollment(termId) }
            };

            foreach(String fileName in files.Keys)
            {
                files[fileName].Save(Server.MapPath(fileName));
                fileNames.Add(fileName);
            }

            return MailControler.PackForDownloading(fileNames, "Schoology_Export", Server);
        }

        #endregion  // User Imports

        #region Schedule Imports

        public static void RequestableCourses(CSV csv)
        {
            int year = GetCurrentAcademicYear();
            int termId = DateRange.GetNextTerm();
            using (WebhostEntities db = new WebhostEntities())
            {
                Term theTerm = db.Terms.Where(t => t.id == termId).Single();

                log.WriteLine("Beginning RequestableCourse Import for {1} {0}", year, theTerm.Name);

                WebhostEventLog.Syslog.LogInformation("Beginning RequestableCourse Import for {1} {0}", year, theTerm.Name);
                int id = db.Courses.OrderBy(c => c.id).ToList().Last().id;
                int reqId = db.RequestableCourses.Count() > 0 ? db.RequestableCourses.OrderBy(rc => rc.id).ToList().Last().id: -1;

                List<String> requiredFields = new List<string>() { Course_BBCourseID, Course_CourseName, Course_Department, Course_LengthInTerms };
                foreach (Dictionary<String, String> row in csv.Data)
                {
                    foreach (String field in requiredFields)
                    {
                        if (!row.ContainsKey(field))
                        {
                            log.WriteLine("Course Row does not contain required field {0}", field);
                            WebhostEventLog.Syslog.LogInformation("Course Row does not contain required field {0}", field);
                            throw new ImportException(String.Format("Course Row does not contain required field {0}", field));
                        }
                    }

                    String bbid = row[Course_BBCourseID];

                    // if the requestable course already exists, skip this one.  This method is not for updating courses specifically.
                    if (theTerm.AcademicYear.Courses.Where(rc => rc.BlackBaudID.Equals(bbid)).Count() > 0)
                    {
                        Course extc = theTerm.AcademicYear.Courses.Where(c => c.BlackBaudID.Equals(bbid)).Single();
                        if (theTerm.RequestableCourses.Where(rc => rc.CourseId == extc.id).Count() > 0)
                            continue;
                    }

                    // Assert that course exists.
                    Course course = new Course();
                    bool update = false;
                    if (db.Courses.Where(c => c.AcademicYearID == year && c.BlackBaudID.Equals(bbid)).Count() > 0)
                    {
                        log.WriteLine("Updating Existing Course {0}", course.Name);
                        WebhostEventLog.Syslog.LogInformation("Updating Existing Course {0}", course.Name);
                        update = true;
                        course = db.Courses.Where(c => c.AcademicYearID == year && c.BlackBaudID.Equals(bbid)).Single();
                    }
                    else
                    {
                        course.id = ++id;
                        course.AcademicYearID = year;
                        course.BlackBaudID = row[Course_BBCourseID];
                        course.SchoologyId = -1;
                    }
                    course.Name = row[Course_CourseName];
                    course.LengthInTerms = Convert.ToInt32(row[Course_LengthInTerms]);
                    String dpt = row[Course_Department];
                    course.DepartmentID = db.Departments.Where(d => d.Name.Equals(dpt)).Single().id;
                    course.goesToSchoology = !(course.Name.Contains("Study") && !course.Name.Contains("Ind")) && !course.Name.Contains("Evening");

                    if (!update)
                    {
                        log.WriteLine("Inserting new course {0} into database.", course.Name);
                        WebhostEventLog.Syslog.LogInformation("Inserting new course {0} into database.", course.Name);
                        db.Courses.Add(course);
                    }

                    // Add new Requestable course.
                    RequestableCourse requestable = new RequestableCourse()
                    {
                        CourseId = course.id,
                        TermId = termId,
                        id = ++reqId
                    };

                    db.RequestableCourses.Add(requestable);
                }
                db.SaveChanges();
                log.WriteLine("Saved Changes to Database.");
                WebhostEventLog.Syslog.LogInformation("Saved Changes to Database.");
            }
        }

        /// <summary>
        /// Import new courses.
        /// 
        /// Fields required are:
        ///     Blacbaud Course ID
        ///     Department
        ///     Length in Terms
        ///     Course Name
        /// 
        /// </summary>
        /// <param name="csv"></param>
        public static void Courses(CSV csv)
        {
            int year = GetCurrentAcademicYear();
            log.WriteLine("Beginning Course Import for {0}", year);
            using(WebhostEntities db = new WebhostEntities())
            {
                int id = db.Courses.OrderBy(c => c.id).ToList().Last().id;
                List<String> requiredFields = new List<string>() { Course_BBCourseID, Course_CourseName, Course_Department, Course_LengthInTerms };
                foreach(Dictionary<String,String> row in csv.Data)
                {
                    foreach(String field in requiredFields)
                    {
                        if(!row.ContainsKey(field))
                        {
                            log.WriteLine("Course Row does not contain required field {0}", field);
                            throw new ImportException(String.Format("Course Row does not contain required field {0}", field));
                        }
                    }

                    String bbid = row[Course_BBCourseID];
                    Course course = new Course();
                    bool update = false;
                    if(db.Courses.Where(c => c.AcademicYearID == year && c.BlackBaudID.Equals(bbid)).Count() > 0)
                    {
                        log.WriteLine("Updating Existing Course {0}", course.Name);
                        update = true;
                        course = db.Courses.Where(c => c.AcademicYearID == year && c.BlackBaudID.Equals(bbid)).Single();
                    }
                    else
                    {
                        course.id = ++id;
                        course.AcademicYearID = year;
                        course.BlackBaudID = row[Course_BBCourseID];
                        course.SchoologyId = -1;
                    }

                    course.Name = row[Course_CourseName];
                    course.LengthInTerms = Convert.ToInt32(row[Course_LengthInTerms]);
                    String dpt = row[Course_Department];
                    if (dpt.Equals("<None>")) dpt = "Other";
                    course.DepartmentID = db.Departments.Where(d => d.Name.Equals(dpt)).Single().id;
                    course.goesToSchoology = !(course.Name.Contains("Study") && !course.Name.Contains("Ind")) && !course.Name.Contains("Evening");

                    if (!update)
                    {
                        log.WriteLine("Inserting new course {0} into database.", course.Name);
                        db.Courses.Add(course);
                    }
                }
                db.SaveChanges();
                log.WriteLine("Saved Changes to Database.");
            }
        }

        /// <summary>
        /// Import Sections from Blackbaud Data
        /// </summary>
        /// <param name="csv"></param>
        public static void Sections(CSV csv)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                int year = GetCurrentAcademicYear();
                int sectionId = db.Sections.OrderBy(s => s.id).ToList().Last().id;
                List<String> requiredFields = new List<string>() { Section_Block, Section_CourseID, Section_SectionNumber, Section_StartTerm };
                foreach(Dictionary<String,String> row in csv.Data)
                {
                    foreach(string field in requiredFields)
                    {
                        if(!row.Keys.Contains(field))
                        {
                            log.WriteLine("Missing required field {0}", field);
                            throw new ImportException(String.Format("Missing required field {0}", field));
                        }
                    }

                    // Look up Course.
                    String bbcid = row[Section_CourseID];
                    if(db.Courses.Where(c => c.BlackBaudID.Equals(bbcid) && c.AcademicYearID == year).Count() <= 0)
                    {
                        log.WriteLine("Cannot find Course with BlackBaudID {0} for {1}", bbcid, year);
                        throw new ImportException(String.Format("Cannot find Course with BlackBaudID {0} for {1}", bbcid, year));
                    }
                    else if (db.Courses.Where(c => c.BlackBaudID.Equals(bbcid) && c.AcademicYearID == year).Count() > 1)
                    {
                        log.WriteLine("Too Many Courses with BlackBaudID {0} for {1}!", bbcid, year);
                        throw new ImportException(String.Format("Too Many Courses with BlackBaudID {0} for {1}", bbcid, year));
                    }
                    
                    Course course = db.Courses.Where(c => c.BlackBaudID.Equals(bbcid) && c.AcademicYearID == year).Single();

                    log.WriteLine("Got Course [{0}] {1}", course.BlackBaudID, course.Name);

                    int sectionNumber = Convert.ToInt32(row[Section_SectionNumber]);
                    log.WriteLine("Section Number: {0}", sectionNumber);
                    int startTermId = GetTermByName(row[Section_StartTerm], year);
                    Term startTerm = db.Terms.Where(t => t.id == startTermId).Single();
                    bool update = false;
                    Section section = new Section();
                    // Check for existing section.
                    if(course.Sections.Where(s => s.Terms.Contains(startTerm) && s.SectionNumber == sectionNumber).Count() > 0)
                    {
                        log.WriteLine("Section Exists already.");
                        if (course.Sections.Where(s => s.Terms.Contains(startTerm) && s.SectionNumber == sectionNumber).Count() > 1)
                        {
                            log.WriteLine("I found too many sections of {0} with section number {1} starting in {2} {3}.", course.Name, sectionNumber, startTerm.Name, year);
                            throw new ImportException(String.Format("I found too many sections of {0} with section number {1} starting in {2} {3}.", course.Name, sectionNumber, startTerm.Name, year));
                        }
                        section = course.Sections.Where(s => s.Terms.Contains(startTerm) && s.SectionNumber == sectionNumber).Single();
                        update = true;
                        section.Terms.Clear();
                        log.WriteLine("Updating Section [{0}] {1}", section.Block.LongName, section.Course.Name);
                    }
                    else
                    {
                        log.WriteLine("I need to create a new Section");
                    }
                    try
                    {
                        section.BlockIndex = GetBlockByName(row[Section_Block], year);
                    }
                    catch(WebhostException we)
                    {
                        log.WriteLine(we.Message);
                        section.BlockIndex = GetBlockByName("", year);
                    }

                    for(int i = 0; i < course.LengthInTerms; i++)
                    {
                        Term term = db.Terms.Where(t => t.id == startTermId).Single();
                        log.WriteLine("Adding {0} to section terms.", term.Name);
                        section.Terms.Add(term);
                        
                        try
                        {
                            startTermId = GetNextTerm(startTermId);
                        }
                        catch(WebhostException e)
                        {
                            log.WriteLine(e.Message);
                            break;
                        }
                    }

                    if(!update)
                    {
                        section.id = ++sectionId;
                        section.getsComments = true;
                        section.CourseIndex = course.id;
                        section.SectionNumber = sectionNumber;
                        section.FromBlackbaud = true;
                        db.Sections.Add(section);
                        log.WriteLine("Created New Section [{0}-Block] {1}", section.BlockIndex, section.CourseIndex);
                    }
                }

                db.SaveChanges();
                log.WriteLine("Sections Saved to Database.");
            }
        }

        /// <summary>
        /// Clear the given terms sections of all Students and Teachers.
        /// </summary>
        /// <param name="id">TermId</param>
        public static void ClearRosters(int id)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                int termId = id;
               
                Term term = db.Terms.Where(t => t.id == termId).Single();
                List<Section> termSections = new List<Section>();
                foreach(Section section in db.Sections.Where(sec => sec.FromBlackbaud).ToList())
                {
                    if(section.Terms.Contains(term))
                    {
                        termSections.Add(section);
                    }
                    else
                    {
                        continue;
                    }
                }

                foreach(Section section in termSections)
                {
                    section.Students.Clear();
                    section.Teachers.Clear();
                    log.WriteLine("Cleared [{0}] {1} of students and teachers.", section.Block.LongName, section.Course.Name);
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// Import Student Rosters.
        /// 
        /// IMPORTANT!!!  This method does NOT clear rosters before importing.
        /// 
        /// </summary>
        /// <param name="csv"></param>
        public static void StudentSchedules(CSV csv)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                foreach (Student student in db.Students.ToList())
                    student.isActive = false;

                List<String> requiredFields = new List<string>() { Roster_BBCourseID, Roster_SectionNumber, Roster_StudentID, Roster_Term };
                int year = GetCurrentAcademicYear();
                int secid = db.Sections.OrderBy(sec =>sec.id).ToList().Last().id;
                foreach (Dictionary<String, String> row in csv.Data)
                {
                    foreach (String field in requiredFields)
                    {
                        if (!row.Keys.Contains(field))
                        {
                            log.WriteLine("Missing required field {0}", field);
                            throw new ImportException(String.Format("Missing required field {0}", field));
                        }
                    }

                    if(row[Roster_Term].Equals(""))
                    {
                        log.WriteLine("Empty Term in row:");
                        foreach(String key in row.Keys)
                        {
                            log.WriteLine("{0}:  {1}", key, row[key]);
                        }

                        continue;
                    }

                    int termId = GetTermByName(row[Roster_Term], year);
                    Term term = db.Terms.Where(t => t.id == termId).Single();
                    int sectionNumber = Convert.ToInt32(row[Roster_SectionNumber]);
                    bool createdFlag = false;
                    String bbcid = row[Roster_BBCourseID];

                    List<Section> possibleSections = db.Sections.Where(s => s.Course.BlackBaudID.Equals(bbcid) && s.Course.AcademicYearID == year && s.SectionNumber == sectionNumber).ToList();
                    
                    if(possibleSections.Count > 1)
                    {
                        List<Section> temp = new List<Section>();
                        foreach(Section sec in possibleSections)
                        {
                            if (sec.Terms.Contains(term))
                                temp.Add(sec);
                        }

                        possibleSections = temp;
                    }

                    if (possibleSections.Count() == 0)
                    {
                        log.WriteLine("Unable to locate section corresponding to {0}-{1} in {2} {3}--Creating", bbcid, sectionNumber, term.Name, year);
                        Section newSection = new Section()
                        {
                            id = ++secid,
                            BlockIndex = GetBlockByName("Independent Study", year),
                            getsComments = false,
                            CourseIndex = db.Courses.Where(c => c.BlackBaudID.Equals(bbcid) && c.AcademicYearID == year).Single().id,
                            SchoologyId = -1,
                            SectionNumber = sectionNumber
                        };
                        createdFlag = true;
                        db.Sections.Add(newSection);
                        possibleSections = (new List<Section>() { newSection });
                        //throw new ImportException(String.Format("Unable to locate section corresponding to {0}-{1} in {2} {3}", bbcid, sectionNumber, term.Name, year));
                    }
                    else if (possibleSections.Count() > 1)
                    {
                        log.WriteLine("Too many sections corresponding to {0}-{1} in {2} {3}", bbcid, sectionNumber, term.Name, year);
                        Section first = possibleSections.First();

                        for (int i = 1; i < possibleSections.Count; i++ )
                        {
                            Section sec = possibleSections[i];
                            foreach(Student std in sec.Students.ToList())
                            {
                                if (!first.Students.Contains(std))
                                {
                                    first.Students.Add(std);
                                }
                            }

                            foreach(Faculty faculty in sec.Teachers.ToList())
                            {
                                if(!first.Teachers.Contains(faculty))
                                {
                                    first.Teachers.Add(faculty);
                                }
                            }

                            sec.SectionNumber = -1;
                         
                        }
                        log.WriteLine("Merged broken sections...");
                        possibleSections = new List<Section>() { first };
                    }

                    Section section = possibleSections.Single();

                    int id = Convert.ToInt32(row[Roster_StudentID]);
                    if (db.Students.Where(f => f.ID == id).Count() <= 0)
                    {
                        log.WriteLine("No student with ID {0}", id);
                        throw new ImportException(String.Format("No student with ID {0}", id));
                    }

                    Student student = db.Students.Where(f => f.ID == id).Single();
                    if (!section.Students.Contains(student))
                    {
                        section.Students.Add(student);
                        student.isActive = true;
                        log.WriteLine("Added {0} {1} to [{2}] {3}", student.FirstName, student.LastName, (createdFlag ? db.Blocks.Where(b => b.id == section.BlockIndex).ToList().Single().Name : section.Block.LongName), section.Course.Name);
                    }
                }

                db.SaveChanges();
                log.WriteLine("Saved Roster Changes to Database.");
            }
        }

        /// <summary>
        /// Import Teacher rosters.
        /// 
        /// IMPORTANT!!!  This method does NOT clear schedules before importing!
        /// </summary>
        /// <param name="csv"></param>
        public static void TeacherSchedules(CSV csv)
        {
            using(WebhostEntities db = new WebhostEntities())
            {
                List<String> requiredFields = new List<string>() { Roster_BBCourseID, Roster_SectionNumber, Roster_TeacherID, Roster_Term };
                int year = GetCurrentAcademicYear();
                int secid = db.Sections.OrderBy(sec => sec.id).ToList().Last().id;
                foreach(Dictionary<String,String> row in csv.Data)
                {
                    foreach(String field in requiredFields)
                    {
                        if(!row.Keys.Contains(field))
                        {
                            log.WriteLine("Missing required field {0}", field);
                            throw new ImportException(String.Format("Missing required field {0}", field));
                        }
                    }

                    int termId = GetTermByName(row[Roster_Term], year);
                    Term term = db.Terms.Where(t => t.id == termId).Single();
                    int sectionNumber = Convert.ToInt32(row[Roster_SectionNumber]);
                    String bbcid = row[Roster_BBCourseID];
                    bool createdFlag = false;
                    List<Section> possibleSections = db.Sections.Where(s => s.Course.BlackBaudID.Equals(bbcid) && s.Course.AcademicYearID == year && s.SectionNumber == sectionNumber).ToList();
                    
                    if (possibleSections.Count > 1)
                    {
                        List<Section> temp = new List<Section>();
                        foreach (Section sec in possibleSections)
                        {
                            if (sec.Terms.Contains(term))
                                temp.Add(sec);
                        }

                        possibleSections = temp;
                    }
                    
                    if(possibleSections.Count() == 0)
                    {
                        log.WriteLine("Unable to locate section corresponding to {0}-{1} in {2} {3}--Creating", bbcid, sectionNumber, term.Name, year);
                        Section newSection = new Section()
                        {
                            id = ++secid,
                            BlockIndex = GetBlockByName("Independent Study", year),
                            getsComments = false,
                            CourseIndex = db.Courses.Where(c => c.BlackBaudID.Equals(bbcid) && c.AcademicYearID == year).Single().id,
                            SchoologyId = -1,
                            SectionNumber = sectionNumber
                        };
                        createdFlag = true;
                        db.Sections.Add(newSection);
                        possibleSections = (new List<Section>() { newSection });
                        //throw new ImportException(String.Format("Unable to locate section corresponding to {0}-{1} in {2} {3}", bbcid, sectionNumber, term.Name, year));
                    }
                    else if (possibleSections.Count() > 1)
                    {
                        log.WriteLine("Too many sections corresponding to {0}-{1} in {2} {3}", bbcid, sectionNumber, term.Name, year);
                        log.WriteLine(String.Format("Too many sections corresponding to {0}-{1} in {2} {3}", bbcid, sectionNumber, term.Name, year));
                        int best = -1, mostStudents = -1, index = 0;
                        foreach(Section sect in possibleSections)
                        {
                            if(sect.Students.Count > mostStudents)
                            {
                                best = index;
                                mostStudents = sect.Students.Count;
                            }
                            index++;
                        }

                        List<Section> bad = possibleSections;

                        possibleSections = new List<Section>() { possibleSections[best] };

                        foreach(Section sect in bad)
                        {
                            if (!possibleSections.Contains(sect))
                            {
                                sect.Students.Clear();
                                sect.Teachers.Clear();
                                sect.Terms.Clear();
                                db.Sections.Remove(sect);
                            }
                        }
                    }

                    Section section = possibleSections.Single();

                    int id = Convert.ToInt32(row[Roster_TeacherID]);
                    if(db.Faculties.Where(f => f.ID == id).Count() <= 0)
                    {
                        log.WriteLine("No faculty with EmployeeID {0}", id);
                        throw new ImportException(String.Format("No faculty with EmployeeID {0}", id));
                    }

                    Faculty teacher = db.Faculties.Where(f => f.ID == id).Single();
                    if (!section.Teachers.Contains(teacher))
                    {
                        section.Teachers.Add(teacher);
                        log.WriteLine("Added {0} {1} to [{2}] {3}", teacher.FirstName, teacher.LastName, (createdFlag ? db.Blocks.Where(b => b.id == section.BlockIndex).ToList().Single().Name : section.Block.LongName), createdFlag ? db.Courses.Where(c => c.id == section.CourseIndex).ToList().Single().Name : section.Course.Name);
                    }
                }

                db.SaveChanges();
                log.WriteLine("Saved Roster Changes to Database.");
            }
        }
        #endregion  // Schedule Imports

        #region Account Creation Exports
        private static Random rand = new Random();

        public static String RandomWindowsPassword()
        {
            String lowers = "abcdefghijklmnopqrstuvwxyz";
            String uppers = lowers.ToUpper();
            String numbers = "1234567890";
            String symbols = "!@#$%^&*()_+-=.?<>";

            String pwd = "";

            List<string> pick3 = new List<string>() { lowers, uppers, numbers, symbols };
            
            for(int i = 0; i < 3; i++)
            {
                String set = pick3[rand.Next(pick3.Count)];
                pick3.Remove(set);
                for (int j = 0; j < 3; j++)
                {
                    char ch = set[rand.Next(set.Length)];
                    set.Replace(Convert.ToString(ch), "");
                    pwd += ch;
                }
            }

            return pwd;
        }

        /// <summary>
        /// Get a CSV for importing new user accounts into both ActiveDirectory and Gmail
        /// 
        /// gmail_users.csv:
        /// email address,first name,last name,password
        /// 
        /// ad_users.csv:
        /// firstName,lastName,userName,password,primaryGroup,description
        /// 
        /// </summary>
        /// <param name="StudentIds"></param>
        /// <returns></returns>
        public static List<CSV> GetNewAccountsCSVs(List<int> StudentIds)
        {
            CSV gmailCsv = new CSV();
            CSV adCsv = new CSV();
            using(WebhostEntities db = new WebhostEntities())
            {
                foreach(int id in StudentIds)
                {
                    if (db.Students.Where(s => s.ID == id).Count() <= 0) continue;
                    Dictionary<String, String> gmailUser = new Dictionary<string, string>();
                    Dictionary<String, String> adUser = new Dictionary<string, string>();
                    Student student = db.Students.Where(s => s.ID == id).Single();

                    String passwd = RandomWindowsPassword();

                    gmailUser.Add("email address", String.Format("{0}@dublinschool.org", student.UserName));
                    gmailUser.Add("first name", student.FirstName);
                    gmailUser.Add("last name", student.LastName);
                    gmailUser.Add("password", passwd);

                    gmailCsv.Add(gmailUser);

                    adUser.Add("firstName", student.FirstName);
                    adUser.Add("lastName", student.LastName);
                    adUser.Add("userName", student.UserName);
                    adUser.Add("password", passwd);
                    adUser.Add("primaryGroup", "Students");
                    adUser.Add("description", Convert.ToString(student.GraduationYear));

                    adCsv.Add(adUser);
                }

            }

            return new List<CSV>() { gmailCsv, adCsv };
        }

        #endregion

        #region Grades Import

        public static int ImportGrades(CSV csv, int termId, Log log = null)
        {
            int errors = 0;
            if(log == null)
            {
                log = Log.CommandLineLog;
            }

            List<String> termNames = new List<string>() { "Fall", "Winter", "Spring" };

            using (WebhostEntities db = new WebhostEntities())
            {
                int newHeaderId = db.CommentHeaders.OrderBy(h => h.id).ToList().Last().id;
                int newCommentId = db.StudentComments.OrderBy(s => s.id).ToList().Last().id;

                Term term = db.Terms.Where(t => t.id == termId).Single();

                GradeTable grades = term.AcademicYear.GradeTables.Where(gt => gt.Name.Equals("Standard A-F Scale")).Single();
                GradeTable effortGrades = term.AcademicYear.GradeTables.Where(gt => gt.Name.Equals("Effort Grades")).Single();

                int termNumber = termNames.IndexOf(term.Name) + 1;

                String effortHeader = GradeSheet_EffortGrade(termNumber);
                String trimesterHeader = GradeSheet_TrimesterGrade(termNumber);
                String examHeader = GradeSheet_ExamGrade(termNumber);

                foreach(Dictionary<string, string> row in csv.Data)
                {
                    String[] courseInfo = row[GradeSheet_CourseInfo].Replace(" ", "").Split('-');
                    int sectionNumber = Convert.ToInt32(courseInfo[1]);
                    Section section;
                    try
                    {
                        section = term.Sections.Where(sec => sec.Course.BlackBaudID.Equals(courseInfo[0]) && sec.SectionNumber == sectionNumber).Single();
                    }
                    catch (Exception e)
                    {
                        log.WriteLine("Could not identify section {0}", row[GradeSheet_CourseInfo]);
                        log.WriteLine(e.Message);
                        errors++;
                        continue;
                    }

                    // check that student is in the class
                    int studentId = Convert.ToInt32(row[GradeSheet_StudentId]);
                    if(section.Students.Where(s => s.ID == studentId).Count() <= 0)
                    {
                        log.WriteLine("Student {0} is not enrolled in {1}", studentId, row[GradeSheet_CourseInfo]);
                        errors++;
                        continue;
                    }

                    // check grades.
                    if (row[GradeSheet_FinalGrade].Equals(""))
                        row[GradeSheet_FinalGrade] = "Not Applicable";
                    if (row[effortHeader].Equals(""))
                        row[effortHeader] = "Not Applicable";
                    if (row[trimesterHeader].Equals(""))
                        row[trimesterHeader] = "Not Applicable";
                    if (row[examHeader].Equals(""))
                        row[examHeader] = "Not Applicable";

                    // Log invalid grade entries.
                    if(grades.GradeTableEntries.Where(g => g.Name.Equals(row[GradeSheet_FinalGrade])).Count() <= 0)
                    {
                        log.WriteLine("Invalid Final Grade {0} in {1}, {2}", row[GradeSheet_FinalGrade], row[GradeSheet_CourseInfo], studentId);
                        errors++;
                        row[GradeSheet_FinalGrade] = "Not Applicable";
                    }
                    if (effortGrades.GradeTableEntries.Where(g => g.Name.Equals(row[effortHeader])).Count() <= 0)
                    {
                        log.WriteLine("Invalid Effort Grade {0} in {1}, {2}", row[effortHeader], row[GradeSheet_CourseInfo], studentId);
                        errors++;
                        row[effortHeader] = "Not Applicable";
                    }
                    if (grades.GradeTableEntries.Where(g => g.Name.Equals(row[trimesterHeader])).Count() <= 0)
                    {
                        log.WriteLine("Invalid Term Grade {0} in {1}, {2}", row[trimesterHeader], row[GradeSheet_CourseInfo], studentId);
                        errors++;
                        row[trimesterHeader] = "Not Applicable";
                    }
                    if (grades.GradeTableEntries.Where(g => g.Name.Equals(row[examHeader])).Count() <= 0)
                    {
                        log.WriteLine("Invalid Exam Grade {0} in {1}, {2}", row[examHeader], row[GradeSheet_CourseInfo], studentId);
                        errors++;
                        row[examHeader] = "Not Applicable";
                    }

                    // Get 
                    int fgid = grades.GradeTableEntries.Where(g => g.Name.Equals(row[GradeSheet_FinalGrade])).Single().id;
                    int efid = effortGrades.GradeTableEntries.Where(g => g.Name.Equals(row[effortHeader])).Single().id;
                    int tgid = grades.GradeTableEntries.Where(g => g.Name.Equals(row[trimesterHeader])).Single().id;
                    int exid = grades.GradeTableEntries.Where(g => g.Name.Equals(row[examHeader])).Single().id;


                }
            }

            return errors;
        }

        #endregion
    }

    public class ImportException : WebhostException
    {
        public ImportException(String Message, Exception innerException = null) : base(Message, innerException)
        {
            
        }
    }

}
