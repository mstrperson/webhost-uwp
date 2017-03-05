using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WebhostUniversalWindows.Controls
{
    public class SectionButton : Button
    {
        public SectionInfo Info { get; set; }

        public SectionButton(SectionInfo info)
        {
            this.Content = String.Format("[{0}] {1}", info.Block, info.Course);
            Info = info;
            Margin = new Thickness(5);
            Padding = new Thickness(5);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            HorizontalContentAlignment = HorizontalAlignment.Center;
        }
    }
}
