using EvoPdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebhostMySQLConnection.EVOPublishing
{
    public class NewStudentRegistrationLetter : Letterhead
    {
        public String FirstName { get; set; }
        public String Email { get; set; }
        public String Password { get; set; }

        public NewStudentRegistrationLetter() :base()
        {
            IncludeSignature = true;
        }

        public Document Publish()
        {
            String htmlContent = String.Format("<p>Dear {0},</p><p>Welcome to Dublin School!  I am the account manager and administrator." +
                "Throughout the school year, the Tech Office is your spot for computer questions.  " +
                "We can help you out with most computer issues if you stop by our office in the library computer lab.</p>" +
                "<p>To get you started, I have set up your new Dublin School Gmail accounts, and I " +
                "have set up a web page that will help you get started.  " +
                "You’ll need the information provided below to access the initial set up.  " +
                "To get started, you’ll need a device with an internet connection available.</p>" +
                "<p>In your web browser, log in to our website at <b><u><a href='https://webhost.dublinschool.org'>" +
                "https://webhost.dublinschool.org</a></u></b> and then navigate to <b><u>" +
                "<a href='https://webhost.dublinschool.org/PasswordInitialization.aspx'>" +
                "https://webhost.dublinschool.org/PasswordInitialization.aspx</a></u></b> to get started.  " +
                "This page will walk you through resetting your password and then logging in to your " +
                "Dublin School Gmail account.  We have already completed all of the Google side of your " +
                "account initialization.  However, you will want to create your own password—unless you " +
                "really like the one that my computer generated for you!</p>" +
                "<p>Also after setting your password, you will be able to “pre-register” any Wifi " +
                "devices you would like to be able to use at school (e.g. your laptop, phone, iPad, etc.).  " +
                "Instructions for this can be found at <b><u><a href='https://webhost.dublinschool.org/RequestDeviceRegistration.aspx'>" +
                "https://webhost.dublinschool.org/RequestDeviceRegistration.aspx</a></u></b>.</p>" +
                "<p>When you open the “Getting Started” web page, you’ll have to log in with your " +
                "new user name and the password that I generated.  Yours are:" +
                "<ul><li>User Name:  {1}</li><li>Password:  {2}</li></ul></p>" +
                "<p>Your Dublin School email address is:  {1}.</p>" +
                "<p>The web page will guide you from here!  If you have any trouble or questions, " +
                "feel free to call me in my office, or email me at the contact info at the bottom of this letter.</p>" +
                "<p>See you this fall!</p>" +
                "<br><br>" +
                "<div>Jason Cox<br>Technology Department Head<br>Assistant Director of IT<br>email: jason@dublinschool.org<br>phone: 603.536.1288</div>", FirstName, Email, Password);

            return PublishGenericLetter(htmlContent, true, 67);  // signed by me!
        }
    }
}
