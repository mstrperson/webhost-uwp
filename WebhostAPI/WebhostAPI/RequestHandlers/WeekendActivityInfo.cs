using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WebhostAPI.RequestHandlers
{
    /// <summary>
    /// Information about a Weekend Activity.
    /// </summary>
    [DataContract]
    public class WeekendActivityInfo
    {
        /// <summary>
        /// Webhost Database WeekendActivity.id
        /// </summary>
        [DataMember(IsRequired = false)]
        public int Id { get; set; }

        /// <summary>
        /// Activity Name.
        /// </summary>
        [DataMember(IsRequired =true)]
        public String Name { get; set; }

        /// <summary>
        /// (Optional) Activity description.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String Description { get; set; }

        /// <summary>
        /// Day and Time that the trip is scheduled for.
        /// </summary>
        [DataMember(IsRequired = true)]
        public DateTime DayAndTime { get; set; }

        /// <summary>
        /// Info about students who are signed up.
        /// </summary>
        [DataMember(IsRequired = false)]
        public List<StudentSignupInfo> Students { get; set; }

        /// <summary>
        /// Is the activity off campus?
        /// </summary>
        [DataMember(IsRequired = false)]
        public bool IsOffCampus { get; set; }

        /// <summary>
        /// Maximum number of student signups.
        /// </summary>
        [DataMember(IsRequired = false)]
        public int MaxSignups { get; set; }

        /// <summary>
        /// List of categories for this trip.
        /// </summary>
        [DataMember(IsRequired = false)]
        public List<String> Categories { get; set; }

        /// <summary>
        /// Get information about a given weekend activity.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="listStudents"></param>
        public WeekendActivityInfo(int id, bool listStudents = true)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                WeekendActivity activity = db.WeekendActivities.Find(id);
                if (activity == null)
                    throw new ArgumentException("Invalid Activity Id.");

                if (activity.IsDeleted)
                    throw new ArgumentException("That Activity has been marked deleted.");

                Id = id;
                Name = activity.Name;
                DayAndTime = activity.DateAndTime;
                IsOffCampus = activity.IsOffCampus;
                MaxSignups = activity.MaxSignups;
                Description = activity.Description;
                Categories = new List<string>();
                foreach(WeekendActivityCategory category in activity.WeekendActivityCategories.ToList())
                {
                    Categories.Add(category.CategoryName);
                }

                if (listStudents)
                {
                    Students = new List<RequestHandlers.StudentSignupInfo>();
                    foreach (StudentSignup signup in activity.StudentSignups.ToList())
                    {
                        Students.Add(new RequestHandlers.StudentSignupInfo(signup.ActivityId, signup.StudentId));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Information about a student's signup for an activity.
    /// </summary>
    [DataContract]
    public class StudentSignupInfo
    {
        /// <summary>
        /// What activity?
        /// </summary>
        [DataMember(IsRequired = true)]
        public int ActivityId { get; set; }

        /// <summary>
        /// Who is signing up?
        /// </summary>
        [DataMember(IsRequired = true)]
        public StudentInfo Student { get; set; }

        /// <summary>
        /// Did they remove themselves from the list?
        /// </summary>
        [DataMember(IsRequired = true)]
        public bool IsRescended { get; set; }

        /// <summary>
        /// Have they been banned?
        /// </summary>
        [DataMember(IsRequired = false)]
        public bool IsBanned { get; set; }

        /// <summary>
        /// When was this updated?
        /// </summary>
        [DataMember(IsRequired = true)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Initialize information about a student's signup.
        /// </summary>
        /// <param name="activity_id"></param>
        /// <param name="student_id"></param>
        public StudentSignupInfo(int activity_id, int student_id)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                StudentSignup signup = db.StudentSignups.Find(activity_id, student_id);
                if (signup == null)
                    throw new ArgumentException("Invalid Signup Id.");

                ActivityId = activity_id;
                Student = new RequestHandlers.StudentInfo(student_id);
                IsRescended = signup.IsRescended;
                IsBanned = signup.IsBanned;
                Timestamp = signup.TimeStamp;
            }
        }
    }
}