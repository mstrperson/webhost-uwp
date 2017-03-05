using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Management.Automation;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Security;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace WebhostMySQLConnection.Web
{
    [DataContract]
    public class ADUser
    {
        public static ADUser AttendanceBot
        {
            get
            {
                return new ADUser() { ID = 67, Authenticated = true, Name = "Attendance Bot", UserName = "jason" };
            }
        }

        [IgnoreDataMember]
        public String HomePage
        {
            get
            {
                if (AccessLevel.Administrator(this))
                    return "~/Admin.aspx";

                return "~/Home.aspx";
            }
        }

        [IgnoreDataMember]
        public String WeekendPage
        {
            get
            {
                if (AccessLevel.Administrator(this) || AccessLevel.DutyTeamLeader(this))
                    return "~/DTL.aspx";

                if (IsTeacher)
                    return "~/WeekendSignupTeacherView.aspx";

                return "~/WeekendSignups.aspx";
            }
        }

        [IgnoreDataMember]
        public String CommentLettersPage
        {
            get
            {
                if (IsTeacher)
                    return "~/CommentEditor.aspx";

                return "~/CommentViewer.aspx";
            }
        }

        [DataMember(Name="permissions")]
        public List<int> Permissions
        {
            get
            {
                using (WebhostEntities db = new WebhostEntities())
                {
                    if (IsStudent)
                    {
                        return db.Students.Where(s => s.ID == ID).Single().Permissions.Select(p => p.id).ToList();
                    }

                    return db.Faculties.Where(f => f.ID == ID).Single().Permissions.Select(p => p.id).ToList();
                }
            }
            protected set { }
        }

        [IgnoreDataMember]
        private List<String> _History;

        [IgnoreDataMember]
        public List<String> VisitedPages
        {
            get
            {
                return _History;
            }
            protected set { }
        }

        public void VisitedPage(String url)
        {
            _History.Add(url);
        }

        [DataMember(Name="is_student")]
        public bool IsStudent
        {
            get
            {
                using (WebhostEntities db = new WebhostEntities())
                    return (db.Students.Where(st => st.UserName.Equals(UserName)).Count() > 0);

            }
            protected set { }
        }

        [DataMember(Name="name")]
        public String Name
        {
            get
            {
                if (this.IsTeacher)
                {
                    using (WebhostEntities db = new WebhostEntities())
                    {
                        Faculty faculty = db.Faculties.Where(fac => fac.UserName.Equals(UserName)).Single();
                        return String.Format("{0} {1}", faculty.FirstName, faculty.LastName);
                    }
                }
                else if (this.IsStudent)
                {
                    using (WebhostEntities db = new WebhostEntities())
                    {
                        Student student = db.Students.Where(fac => fac.UserName.Equals(UserName)).Single();
                        return String.Format("{0} {1}", student.FirstName, student.LastName);
                    }
                }

                return "No Name";
            }
            protected set { }
        }

        [DataMember(Name="id")]
        public  int ID
        {
            get
            {
                if (this.IsTeacher)
                {
                    using (WebhostEntities db = new WebhostEntities())
                        return db.Faculties.Where(fac => fac.UserName.Equals(UserName)).Single().ID;
                }
                else if (this.IsStudent)
                {
                    using (WebhostEntities db = new WebhostEntities())
                        return db.Students.Where(fac => fac.UserName.Equals(UserName)).Single().ID;
                }

                return -1;
            }
            protected set { }
        }

        [DataMember(Name="is_teacher")]
        public bool IsTeacher
        {
            get
            {
                using (WebhostEntities db = new WebhostEntities())
                {
                    return (db.Faculties.Where(st => st.UserName.Equals(UserName)).Count() > 0);
                }
            }
            protected set { }
        }

        private ADUser()
        {
            // private default constructor.
        }

        public ADUser(string userName, SecureString password)
        {
            UserName = userName;
            Authenticated = true;
            _History = new List<string>();
            try
            {
                PSCredential cred = new PSCredential(UserName, password);
                DirectoryEntry entry = new DirectoryEntry("LDAP://192.168.76.3:389/DC=dublinschool,DC=org", UserName, cred.GetNetworkCredential().Password);
                Object obj = entry.NativeObject;
                WebhostEventLog.Syslog.LogInformation("Successfully Authenticated {0} via zoed.", userName);
            }
            catch (Exception f)
            {
                WebhostEventLog.Syslog.LogWarning("Could not contact zoed. {0}", f.Message);
                try
                {
                    PSCredential cred = new PSCredential(UserName, password);
                    DirectoryEntry entry = new DirectoryEntry("LDAP://192.168.76.4:389/DC=dublinschool,DC=org", UserName, cred.GetNetworkCredential().Password);
                    Object obj = entry.NativeObject;
                    WebhostEventLog.Syslog.LogInformation("Successfully Authenticated {0} via buddy.", userName);
                }
                catch (Exception e)
                {

                    Authenticated = false;
                    WebhostEventLog.Syslog.LogError("Failed to Authenticate on zoed and buddy {0}:  {1}", userName, e.Message);
                }
            }

            HttpContext.Current.Session[State.AuthUser] = this;
        }

        public ADUser(Fingerprint fp)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Fingerprint print = db.Fingerprints.Find(fp.Id);

                if(print.CurrentFaculty.Count == 1)
                {
                    Faculty faculty = print.CurrentFaculty.Single();
                    UserName = faculty.UserName;
                }
                else if(print.AvailableStudents.Count == 1)
                {
                    Student student = print.AvailableStudents.Single();
                    UserName = student.UserName;
                }
                else
                {
                    throw new System.IO.InvalidDataException("Invalid use of Fingerprint.");
                }

                Authenticated = true;
                _History = new List<string>();
            }
        }

        public void Logout()
        {
            HttpContext.Current.Session.Clear();
            HttpContext.Current.Response.Redirect("~/Login.aspx");
        }

        private String _uname;
        [DataMember(Name="user_name")]
        public  String UserName
        {
            get
            {
                if (HttpContext.Current != null)
                    return HttpContext.Current.Session["username"] == null ? "" : (String)HttpContext.Current.Session["username"];
                else
                    return _uname;
            }
            protected set
            {
                if (value.Contains("@"))
                    value = value.Split('@')[0];
                if (HttpContext.Current != null)
                    HttpContext.Current.Session["username"] = value;
                else
                    _uname = value;

            }
        }

        private bool _auth;
        private Fingerprint fp;
        [DataMember(Name="authenticated")]
        public bool Authenticated
        {
            get
            {
                if (HttpContext.Current != null)
                    return HttpContext.Current.Session["authd"] == null ? false : (bool)HttpContext.Current.Session["authd"];
                else
                    return _auth;
            }
            protected set
            {
                if (HttpContext.Current != null)
                    HttpContext.Current.Session["authd"] = value;
                else
                    _auth = value;
            }
        }
    }
}
