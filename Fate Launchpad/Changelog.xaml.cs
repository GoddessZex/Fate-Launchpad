using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace FateLaunchpad
{
    /// <summary>
    /// Interaction logic for Changelog.xaml
    /// </summary>
    public partial class Changelog : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (DwmSetWindowAttribute(handle, 19, new[] { 1 }, 4) != 0)
            {
                DwmSetWindowAttribute(handle, 20, new[] { 1 }, 4);
            }
        }

        private const string ChangelogUrl = "https://fatecheats.com/changelog/";

        public Changelog()
        {
            InitializeComponent();
        }

        private void WebBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            WebBrowser browser = (WebBrowser)sender;
            browser.Navigate(ChangelogUrl);

            // Disable caching
            browser.Navigated += (s, args) =>
            {
                browser.Refresh();
            };
        }
    }
}
