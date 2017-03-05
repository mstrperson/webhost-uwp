using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WebhostUniversalWindows;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CommentLetters
{
    public sealed partial class Login : UserControl
    {
        public event EventHandler OnAuthenticationCompleted;
        public event EventHandler OnResponseSuccess;
        public event EventHandler OnResponseFailure;

        
        public Login()
        {
            this.InitializeComponent();
            this.OnResponseSuccess += LoginControl_OnAuthenticate;
            this.OnResponseFailure += LoginControl_OnAuthenticationFailed;
            this.Padding = new Thickness(10);
        }

        private async void LoginControl_OnAuthenticationFailed(object sender, EventArgs e)
        {
            // Pop up a message

            var messageDialog = new Windows.UI.Popups.MessageDialog("Authentication Failed.");
            await messageDialog.ShowAsync();
            passwordBox.Password = "";
        }

        private async void LoginControl_OnAuthenticate(object sender, EventArgs e)
        {
            WebhostAPICall.AuthenticationInfo response = (WebhostAPICall.AuthenticationInfo)sender;
            if (response.Fingerprint.Length > 0)
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog("Authentication Successful.");
                await messageDialog.ShowAsync();
                passwordBox.Password = "";

                // Continue on to the rest of the program from here.  
                // this line is a simplified check to make sure the event handler is not null and then invoke if it has a handler.  
                OnAuthenticationCompleted?.Invoke(response, e);
            }
            else
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog("Authentication returned a blank fingerprint.");
                await messageDialog.ShowAsync();
                passwordBox.Password = "";
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WebhostAPICall.AuthenticationInfo info = await WebhostAPICall.AuthenticateAsync(userNameInput.Text, passwordBox.Password);
                OnResponseSuccess(info, new EventArgs());
            }
            catch(UnauthorizedAccessException ex)
            {
                OnResponseFailure(ex, new EventArgs());
            }
        }
    }
}
