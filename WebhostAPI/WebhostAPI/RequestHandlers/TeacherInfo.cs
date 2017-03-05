using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WebhostAPI.RequestHandlers
{
    /// <summary>
    /// Generic User Info.
    /// </summary>
    [DataContract]
    public abstract class WebhostUserInfo
    {
        /// <summary>
        /// User Name (without @dublinschool.org)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String UserName { get; set; }

        /// <summary>
        /// User's first name.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String FirstName { get; set; }

        /// <summary>
        /// User's last name.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String LastName { get; set; }

        /// <summary>
        /// Email address used for contacting the end user.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Email { get; set; }

        /// <summary>
        /// Groups that the user belongs to.
        /// This is from Database.ApiGroups
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
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

        /// <summary>
        /// Send an email to this user from the supplied sender.
        /// </summary>
        /// <param name="subject">Email Subject</param>
        /// <param name="message">Email message body</param>
        /// <param name="sender">Who is sending the email?</param>
        public void SendEmailToUser(String subject, String message, WebhostUserInfo sender)
        {
            WebhostMySQLConnection.Web.MailControler.MailToUser(
                subject, message, 
                Email, String.Format("{0} {1}", FirstName, LastName), 
                sender.Email, String.Format("{0} {1}", sender.FirstName, sender.LastName));
        }

        /// <summary>
        /// Generate a new Fingerprint of a given length.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GenerateNewFingerprint(int length = 256)
        {
            byte[] fingerprint = new byte[length];

            Random rand = new Random();
            rand.NextBytes(fingerprint);

            return fingerprint;
        }
    }

    /// <summary>
    /// Extension of base user class for Faculty.
    /// </summary>
    [DataContract]
    public class TeacherInfo : WebhostUserInfo
    {
        /// <summary>
        /// Initialize an instance of the abstract UserInfo class as a Faculty member given the ID number.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fullDetails"></param>
        public TeacherInfo(int id, bool fullDetails = false)
        {
            using (WebhostEntities db = new WebhostAPI.WebhostEntities())
            {
                Faculty owner = db.Faculties.Find(id);
                if (owner == null) throw new ArgumentException("Invalid Faculty Id.");

                UserName = owner.UserName;
                Id = owner.ID;

                if (fullDetails)
                {
                    FirstName = owner.FirstName;
                    LastName = owner.LastName;
                    Email = String.Format("{0}@dublinschool.org", UserName);
                    Groups = new List<string>();
                    foreach (ApiPermissionGroup group in owner.ApiPermissionGroups.ToList())
                    {
                        Groups.Add(group.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Get a Faculty's user details given their fingerprint.  Used for Authentication/Authorization.
        /// </summary>
        /// <param name="fingerprint"></param>
        /// <param name="fullDetails"></param>
        public TeacherInfo(byte[] fingerprint, bool fullDetails = false)
        {
            using (WebhostEntities db = new WebhostEntities())
            {
                Faculty owner = null;
                foreach (Faculty teacher in db.Faculties.ToList())
                {
                    if (teacher.CurrentFingerprint.Value.SequenceEqual(fingerprint))
                    {
                        if (teacher.CurrentFingerprint.IsDeactivated || teacher.CurrentFingerprint.IsDeleted)
                            throw new AccessViolationException("Unauthorized Use of Old Fingerprint.");
                        owner = teacher;
                        break;
                    }
                }

                if (owner == null)
                    throw new AccessViolationException("Unrecognized Fingerprint");

                UserName = owner.UserName;
                Id = owner.ID;

                if(fullDetails)
                {
                    FirstName = owner.FirstName;
                    LastName = owner.LastName;
                    Email = String.Format("{0}@dublinschool.org", UserName);
                    Groups = new List<string>();
                    foreach(ApiPermissionGroup group in owner.ApiPermissionGroups.ToList())
                    {
                        Groups.Add(group.Name);
                    }
                }
            }
        }
    }
}