using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Web;
using WebhostMySQLConnection.EVOPublishing;
using WebhostMySQLConnection.Web;

namespace WebhostAPI.RequestHandlers
{
    /// <summary>
    /// Authentication method.  This Datatype should never need to be instantiated on its own.
    /// It is meant to be constructed by the json deserializer using data passed in the body of
    /// and encrypted HttpWebRequest.
    /// </summary>
    [DataContract]
    public class AuthenticationInfo
    {
        /// <summary>
        /// If the EncodedCredential provided is not formatted correctly, this will return a 500 Internal Server Error.
        /// 
        /// When this object is instantiated via the json deserializer,
        /// The EncodedCredential is processed and throws InvalidOperationException based
        /// on bad results:
        /// "Invalid Credential" if the credential string is malformed.
        /// "403" if the account is locked out.
        /// "429" if too many requests in a short period of time.
        ///     This also triggers an email to the Webmaster.
        /// "418" if the password is incorrect 5 times
        ///     this also triggers an account lockout which must be manually reset.
        /// "401" if there is any sort of database error.
        /// 
        /// If there are no errors, then this automatically populates the "Fingerprint" field
        /// so that this object can be returned to the requestor.
        /// 
        /// EncodedCredential should be the following format encoded into base64: 
        /// "username:you;passwd:###########"
        /// 
        /// where the ######### is your password encoded into base64.
        /// 
        /// </summary>
        [DataMember(IsRequired = true)]
        public String EncodedCredential
        {
            get
            {
                return "";
            }
            set
            {
                String[] info = CommentLetter.ConvertFromBase64String(value).Split(';');
                if (info.Length != 2)
                    throw new InvalidOperationException("Invalid Credential");

                String username = info[0].Split(':')[1];
                String pwd = CommentLetter.ConvertFromBase64String(info[1].Split(':')[1]);
                pwd = pwd.Replace("\r\n", "");

                using (SecureString sstr = new SecureString())
                {
                    foreach (char ch in pwd)
                    {
                        sstr.AppendChar(ch);
                    }

                    sstr.MakeReadOnly();

                    if (username.Contains("@"))
                    {
                        username = username.Split('@')[0];
                    }

                    using (WebhostEntities db = new WebhostEntities())
                    {
                        Variable lockedOut;
                        String lockvn = String.Format("locked_out:{0}", username);
                        if (db.Variables.Where(v => v.Name.Equals(lockvn)).Count() > 0)
                        {
                            lockedOut = db.Variables.Where(v => v.Name.Equals(lockvn)).Single();
                            if (lockedOut.Value.Equals("true"))
                            {
                                // Send Forbidden status code.
                                throw new InvalidOperationException("403");
                            }
                        }
                        else
                        {
                            lockedOut = new Variable()
                            {
                                id = db.Variables.OrderBy(v => v.id).ToList().Last().id + 1,
                                Name = String.Format("locked_out:{0}", username),
                                Value = "false"
                            };
                            db.Variables.Add(lockedOut);
                            db.SaveChanges();
                        }
                        String authtimevn = String.Format("auth_time:{0}", username);
                        String spamvn = String.Format("spam_count:{0}", username);
                        if (db.Variables.Where(v => v.Name.Equals(authtimevn)).Count() > 0)
                        {
                            Variable auth_time = db.Variables.Where(v => v.Name.Equals(authtimevn)).Single();
                            Variable spam_count = db.Variables.Where(v => v.Name.Equals(spamvn)).Single();
                            if ((DateTime.Now - DateTime.FromBinary(Convert.ToInt64(auth_time.Value))).TotalSeconds < 5)
                            {
                                auth_time.Value = Convert.ToString(DateTime.Now.ToBinary());

                                int scount = Convert.ToInt32(spam_count.Value);
                                spam_count.Value = Convert.ToString(++scount);

                                db.SaveChanges();

                                if (scount > 5)
                                {
                                    MailControler.MailToWebmaster("Login API Spam", String.Format("{0} has been sending authentication requests very quickly...", username));

                                }

                                throw new InvalidOperationException("429");
                            }

                            auth_time.Value = Convert.ToString(DateTime.Now.ToBinary());
                            db.SaveChanges();
                        }
                        else
                        {
                            Variable new_auth_time = new Variable()
                            {
                                id = db.Variables.OrderBy(v => v.id).ToList().Last().id + 1,
                                Name = String.Format("auth_time:{0}", username),
                                Value = Convert.ToString(DateTime.Now.ToBinary())
                            };

                            Variable new_spam_count = new Variable()
                            {
                                id = new_auth_time.id + 1,
                                Name = String.Format("spam_count:{0}", username),
                                Value = "0"
                            };

                            db.Variables.Add(new_auth_time);
                            db.Variables.Add(new_spam_count);
                            db.SaveChanges();
                        }

                        Variable failCount;
                        string fcvn = String.Format("auth_fail_count:{0}", username);
                        if (db.Variables.Where(v => v.Name.Equals(fcvn)).Count() > 0)
                        {
                            failCount = db.Variables.Where(v => v.Name.Equals(fcvn)).Single();
                        }
                        else
                        {
                            failCount = new Variable()
                            {
                                id = db.Variables.OrderBy(v => v.id).ToList().Last().id + 1,
                                Name = String.Format("auth_fail_count:{0}", username),
                                Value = "0"
                            };
                        }

                        int count = Convert.ToInt32(failCount.Value);
                        if (count > 5)
                        {
                            MailControler.MailToWebmaster("I'm a Teapot", String.Format("There have been too many failed password attempts by {0} against the Login API.  You have to manually fix this.", username));
                            lockedOut.Value = "true";
                            db.SaveChanges();
                            // If you fail password more than 5 times, you get nonsense until that is fixed.
                            throw new InvalidOperationException("418");
                        }
                        ADUser user = (new ADUser(username, sstr));
                        if (user.Authenticated)
                        {
                            byte[] fingerprint = { };

                            if (user.IsStudent)
                            {
                                Student student = db.Students.Find(user.ID);
                                if (student.Fingerprints.Where(f => !f.IsDeactivated && !f.IsDeleted).Count() > 0)
                                {
                                    fingerprint = student.Fingerprints.Where(f => !f.IsDeactivated && !f.IsDeleted).ToList().First().Value;
                                }
                                else
                                {
                                    Fingerprint fp = new Fingerprint()
                                    {
                                        Id = db.Fingerprints.OrderBy(f => f.Id).ToList().Last().Id + 1,
                                        IsDeleted = false,
                                        IsDeactivated = false,
                                        IsInUse = true,
                                        Value = WebhostUserInfo.GenerateNewFingerprint()
                                    };

                                    db.Fingerprints.Add(fp);

                                    student.Fingerprints.Add(fp);
                                    db.SaveChanges();

                                    fingerprint = fp.Value;
                                }
                            }
                            else
                            {
                                Faculty faculty = db.Faculties.Find(user.ID);
                                if (faculty.CurrentFingerprint.Id > 0 && !faculty.CurrentFingerprint.IsDeactivated && !faculty.CurrentFingerprint.IsDeleted)
                                {
                                    fingerprint = faculty.CurrentFingerprint.Value;
                                }
                                else if (faculty.Fingerprints.Where(f => !f.IsDeactivated && !f.IsDeleted).Count() > 0)
                                {
                                    fingerprint = faculty.Fingerprints.Where(f => !f.IsDeactivated && !f.IsDeleted).ToList().First().Value;
                                }
                                else
                                {
                                    Fingerprint fp = new Fingerprint()
                                    {
                                        Id = db.Fingerprints.OrderBy(f => f.Id).ToList().Last().Id + 1,
                                        IsDeleted = false,
                                        IsDeactivated = false,
                                        IsInUse = true,
                                        Value = WebhostUserInfo.GenerateNewFingerprint()
                                    };

                                    db.Fingerprints.Add(fp);

                                    faculty.CurrentFingerprintId = fp.Id;
                                    faculty.Fingerprints.Add(fp);
                                    db.SaveChanges();

                                    fingerprint = fp.Value;
                                }
                            }

                            failCount.Value = "0";
                            db.SaveChanges();
                            Fingerprint = Convert.ToBase64String(fingerprint);
                        }
                        else
                        {
                            failCount.Value = Convert.ToString(++count);
                            db.SaveChanges();

                            throw new InvalidOperationException("401");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Base64Encoded Unique fingerprint data used for authentication.
        /// 
        /// This is generated from the Encoded Credentials and should not be passed in a post request.
        /// Any data passed in this field will be ignored (or will potentially cause your request to be rejected.)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Fingerprint { get; set; }
        
    }
}