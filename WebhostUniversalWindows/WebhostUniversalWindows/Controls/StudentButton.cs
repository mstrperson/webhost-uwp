using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WebhostUniversalWindows.Controls
{
    public class StudentButton : Button
    {
        public StudentInfo Info { get; set; }

        public StudentButton(StudentInfo info)
        {
            this.Content = String.Format("{0} {1}", info.FirstName, info.LastName);
            Margin = new Thickness(5);
            Padding = new Thickness(5);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            Info = info;
        }
    }
}
