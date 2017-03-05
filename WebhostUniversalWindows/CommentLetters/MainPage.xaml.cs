using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WebhostUniversalWindows;
using WebhostUniversalWindows.Controls;
using WebhostUniversalWindows.Comments;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net;
using Syncfusion.UI.Xaml.RichTextBoxAdv;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CommentLetters
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Login loginControl = new Login();

        public event EventHandler StateChanged;

        private enum State
        {
            WaitingForAccess = 0,
            LoadedSections = 1,
            LoadedHeader = 2,
            LoadedStudent = 3
        }

        private State CurrentState;

        private SectionInfo CurrentSection;
        private StudentInfo CurrentStudent;

        public MainPage()
        {
            CurrentState = State.WaitingForAccess;
                        
            StateChanged += MainPage_StateChanged;

            this.InitializeComponent();
            if (!WebhostAPICall.HasCredentials)
            {
                loginControl.OnAuthenticationCompleted += LoginControl_OnAuthenticationCompleted;
                ClassSelectPanel.Children.Add(loginControl);
            }
            else
            {
                LoginControl_OnAuthenticationCompleted(this, new EventArgs());
            }

            //MainPage_StateChanged(this, new EventArgs());
            SaveBtn.IsEnabled = false;
        }

        private void MainPage_StateChanged(object sender, EventArgs e)
        {
            if((int)CurrentState > 1)
            {
                SaveBtn.IsEnabled = true;
                EditorBox.Visibility = Visibility.Visible;
            }
            else
            {
                SaveBtn.IsEnabled = false;
            }

            if((int)CurrentState > 1)
            {
                StudentSelectPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StudentSelectPanel.Visibility = Visibility.Collapsed;
            }

            if((int)CurrentState < 3)
            {
                StudentNameBox.Text = "";
            }
            if((int)CurrentState < 2)
            {
                ClassNameBox.Text = "";
                SaveBtn.IsEnabled = false;
            }
        }

        private async void LoginControl_OnAuthenticationCompleted(object sender, EventArgs e)
        {
            ClassSelectPanel.Children.Remove(loginControl);
            await UserInfo.GetCurrent();
            UserNameDisplay.Text = String.Format("{0} {1}", UserInfo.Current.FirstName, UserInfo.Current.LastName);
            List<SectionInfo> sections = await ((TeacherInfo)UserInfo.Current).CurrentSections();
            StudentLabel.Visibility = Visibility.Visible;
            foreach(SectionInfo section in sections)
            {
                SectionButton button = new SectionButton(section);
                button.Click += Button_Click;
                ClassSelectPanel.Children.Add(button);
            }

            CurrentState = State.LoadedSections;
            StateChanged(this, new EventArgs());
        }

        /// <summary>
        /// Selected a class.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            
            SectionButton button = (SectionButton)sender;
            ClassNameBox.Text = (String)button.Content;
            StudentSelectPanel.Children.Clear();
            CurrentSection = button.Info;

            foreach(var item in ClassSelectPanel.Children)
            {
                if(item is SectionButton)
                {
                    ((SectionButton)item).IsEnabled = true;
                }
            }

            foreach(StudentInfo info in button.Info.Students.OrderBy(inf => inf.LastName).ThenBy(inf => inf.FirstName).ToList())
            {
                StudentButton studentButton = new StudentButton(info);
                studentButton.Click += StudentButton_Click;
                StudentSelectPanel.Children.Add(studentButton);
            }

            try
            {
                CommentSettings.CurrentHeaderParagraph = await button.Info.CurrentCommentHeaderAsync();
            }
            // If something goes wrong alert the user.
            catch(WebException ex)
            {
                String responseBody;
                using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream()))
                {
                    responseBody = sr.ReadToEnd();
                }

                Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(responseBody);
                await dialog.ShowAsync();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(ms);
                writer.Write(CommentSettings.CurrentHeaderParagraph.Html);
                writer.Flush();
                ms.Position = 0;

                EditorBox.Load(ms, FormatType.Html);
            }


            button.IsEnabled = false;
            CurrentState = State.LoadedHeader;
            StateChanged(this, new EventArgs());
        }

        private async void StudentButton_Click(object sender, RoutedEventArgs e)
        {
            StudentButton btn = (StudentButton)sender;
            StudentNameBox.Text = (String)btn.Content;
            CurrentStudent = btn.Info;

            foreach(var item in StudentSelectPanel.Children)
            {
                if(item is StudentButton)
                {
                    ((StudentButton)item).IsEnabled = true;
                }
            }

            try
            {
                CommentSettings.CurrentStudentComment = await CurrentStudent.CurrentCommentAsync();
            }
            // If something goes wrong alert the user.
            catch (WebException ex)
            {
                String responseBody;
                using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream()))
                {
                    responseBody = sr.ReadToEnd();
                }

                Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(responseBody);
                await dialog.ShowAsync();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(ms);
                writer.Write(CommentSettings.CurrentStudentComment.HtmlContent);
                writer.Flush();
                ms.Position = 0;

                EditorBox.Load(ms, FormatType.Html);
            }

            btn.IsEnabled = false;
            CurrentState = State.LoadedStudent;
            StateChanged(this, new EventArgs());
        }

        private async void SaveHeaderAsync()
        {
            EditorBox.IsReadOnly = true;
            using (MemoryStream ms = new MemoryStream())
            {
                EditorBox.Save(ms, FormatType.Html);
                ms.Flush();
                ms.Position = 0;

                StreamReader reader = new StreamReader(ms);
                String content = reader.ReadToEnd();

                CommentSettings.CurrentHeaderParagraph.SetHtml(content);
            }

            HttpWebResponse response = await CurrentSection.SaveCommentHeaderAsync(CommentSettings.CurrentHeaderParagraph.HtmlContent);

            String message = "";
            switch(response.StatusCode)
            {
                case HttpStatusCode.OK: message = "Successfully Saved to Webhost."; break;
                default: message = String.Format("Something went wrong...{0}{1}", Environment.NewLine, response.StatusDescription); break;
            }

            EditorBox.IsReadOnly = false;
        }

        private async void SaveStudentCommentAsync()
        {
            EditorBox.IsReadOnly = true;
            using (MemoryStream ms = new MemoryStream())
            {
                EditorBox.Save(ms, FormatType.Html);
                ms.Flush();
                ms.Position = 0;

                StreamReader reader = new StreamReader(ms);
                String content = reader.ReadToEnd();

                CommentSettings.CurrentStudentComment.HtmlContent = content;
            }

            HttpWebResponse response = await CurrentStudent.SaveCommentAsync(CommentSettings.CurrentStudentComment.HtmlContent);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            switch(CurrentState)
            {
                case State.LoadedHeader:
                    SaveHeaderAsync();
                    break;
                default:

                    break;
            }
        }

        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            UserInfo.Logout();
            Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove("fingerprint");
            Application.Current.Exit();
        }
    }
}
