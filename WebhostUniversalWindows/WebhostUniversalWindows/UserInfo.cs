using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace WebhostUniversalWindows
{
    /// <summary>
    /// Generic User Info.
    /// </summary>
    [DataContract]
    public abstract class UserInfo
    {
        private static UserInfo _Current = null;
        public async static Task<UserInfo> GetCurrent(bool isTeacher = true)
        {
            if (_Current == null && isTeacher)
                _Current = (TeacherInfo)(await WebhostAPICall.GetObjectAsync("api/self", typeof(TeacherInfo)));
            else if (_Current == null)
                _Current = (StudentInfo)await WebhostAPICall.GetObjectAsync("api/self", typeof(StudentInfo));

            return _Current;
        }

        public static void Logout()
        {
            _Current = null;
        }

        public static UserInfo Current
        {
            get { return _Current; }
        }
        
        /// <summary>
        /// User Name (without @dublinschool.org)
        /// </summary>
        [DataMember(IsRequired = false)]
        public String UserName { get; set; }

        /// <summary>
        /// User's first name.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String FirstName { get; set; }

        /// <summary>
        /// User's last name.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String LastName { get; set; }

        /// <summary>
        /// Email address used for contacting the end user.
        /// </summary>
        [DataMember(IsRequired = false)]
        public String Email { get; set; }

        /// <summary>
        /// Groups that the user belongs to.
        /// This is from Database.ApiGroups
        /// </summary>
        [DataMember(IsRequired = false)]
        public List<String> Groups { get; set; }

        /// <summary>
        /// User's Webhost Database ID field.
        /// </summary>
        [DataMember(IsRequired = true)]
        public int Id { get; set; }

        /// <summary>
        /// Is this user a Teacher?
        /// </summary>
        /// <returns></returns>
        public bool IsTeacher()
        {
            return this is TeacherInfo;
        }

        /// <summary>
        /// Is this user a Student?
        /// </summary>
        /// <returns></returns>
        public bool IsStudent()
        {
            return this is StudentInfo;
        }

    }

    /// <summary>
    /// Extension of base user class for Faculty.
    /// </summary>
    [DataContract]
    public class TeacherInfo : UserInfo
    {
        public async Task<List<StudentInfo>> Advisees()
        {
            return (List<StudentInfo>)await WebhostAPICall.GetObjectAsync("api/self/advisees", typeof(List<StudentInfo>));
        }

        public async Task<List<SectionInfo>> CurrentSections()
        {
            return (List<SectionInfo>)await WebhostAPICall.GetObjectAsync("api/self/sections/current?detailed=true", typeof(List<SectionInfo>));
        }
    }

    [DataContract]
    public partial class StudentInfo : UserInfo
    {
        /// <summary>
        /// Student's Advisor.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public TeacherInfo Advisor { get; set; }

        /// <summary>
        /// Student's Graduation Year.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int GraduationYear { get; set; }
               
    }
}
