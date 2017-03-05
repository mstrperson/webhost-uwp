using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebhostMySQLConnection.Web
{
    public class AccessLevel
    {
        private static List<string> administrators
        {
            get
            {
                List<String> admins = new List<string>();
                admins.Add("jason");
                admins.Add("jeff");
                return admins;
            }
        }

        private static List<string> deanofstudents
        {
            get
            {
                List<String> admins = new List<string>();
                admins.AddRange(administrators);
                admins.Add("smcfall");
                admins.Add("cglaude");
                return admins;
            }
        }

        private static List<string> deanofacademics
        {
            get
            {
                List<String> admins = new List<string>();
                admins.AddRange(administrators);
                admins.Add("sdoenmez");
                admins.Add("psiegel");
                admins.Add("paul");
                admins.Add("jforeman");
                return admins;
            }
        }

        private static List<string> lsp
        {
            get
            {
                List<String> admins = new List<string>();
                admins.AddRange(administrators);
                admins.Add("jregan");
                return admins;
            }
        }

        private static List<string> health
        {
            get
            {
                List<String> admins = new List<string>();
                admins.AddRange(deanofstudents);
                admins.Add("rnewton");
                return admins;
            }
        }

        public static bool DutyTeamLeader(ADUser user)
        {
            if (user == null || !user.Authenticated || !user.IsTeacher) return false;

            if (DeanOfStudents(user)) return true;

            using (WebhostEntities db = new WebhostEntities())
            {
                Faculty faculty = db.Faculties.Where(f => f.ID == user.ID).Single();
                int year = DateRange.GetCurrentAcademicYear();
                return faculty.DutyTeamsLead.Where(d => d.AcademicYearID == year).Count() > 0;
            }
        }

        /// <summary>
        /// Does the User have administrative access to Attendance Records?
        /// </summary>
        /// <param name="User">Pass in user here.</param>
        /// <returns></returns>
        public static bool AttendanceAdmin(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return deanofstudents.Contains(user.UserName);
        }

        /// <summary>
        /// Does the User have administrative access to Attendance Records?
        /// </summary>
        /// <param name="User">Pass in user here.</param>
        /// <returns></returns>
        public static bool HealthCentre(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return health.Contains(user.UserName);
        }

        /// <summary>
        /// Is the designated User the Dean of Students?
        /// </summary>
        /// <param name="User">Pas in user here</param>
        /// <returns></returns>
        public static bool DeanOfStudents(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return deanofstudents.Contains(user.UserName);
        }

        public static bool DeanOfAcademics(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return deanofacademics.Contains(user.UserName);
        }
        public static bool LearningSkillsHead(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return lsp.Contains(user.UserName);
        }

        /// <summary>
        /// Is the designated User a member have Administrative Access to the Web Site?
        /// </summary>
        /// <param name="User">Pass in user</param>
        /// <returns>true if the current user is a member of "DUBLINSCHOOL\Web Administrators" Security group</returns>
        public static bool Administrator(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return administrators.Contains(user.UserName);
        }


        /// <summary>
        /// Is the designated User a member have Student Level Access to the Web Site?
        /// </summary>
        /// <param name="User">Pass in user</param>
        /// <returns>true if the current user is a member of "DUBLINSCHOOL\Students" Security group</returns>
        public static bool Student(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return user.IsStudent || administrators.Contains(user.UserName);
        }

        /// <summary>
        /// Is the designated User a member have Faculty Level Access to the Web Site?
        /// </summary>
        /// <param name="User">Pass in user</param>
        /// <returns>true if the current user is a member of "DUBLINSCHOOL\Faculty" Security group</returns>
        public static bool Faculty(ADUser user)
        {
            if (user == null || !user.Authenticated) return false;
            return user.IsTeacher;
        }
    }
}
