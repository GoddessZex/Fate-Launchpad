using FateLaunchpad.Properties;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace FateLaunchpad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class DLLItem
    {
        public string Path { get; set; }
        public bool IsChecked { get; set; }

        public DLLItem(string path, bool isChecked = false)
        {
            Path = path;
            IsChecked = isChecked;
        }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Fate.EnsureFateDirExists();

            InitializeComponent();

            StaticWindow = this;

            timer = new Timer(timerTick, injectBtn, 500, Timeout.Infinite);
        }

        public static MainWindow StaticWindow;
        public bool advancedMode = false;
        public string fateDll;

        public List<string> dlls = new List<string>();

        public Timer timer;

        public static string SettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fate", "Launchpad.txt");
        public static Settings settings = Settings.ReadFile(SettingsFile);

        private async void timerTick(object _)
        {
            while (true)
            {
                bool gtaRunning = Fate.IsGtaRunning();

                Visibility visibility = gtaRunning ?
                        Visibility.Visible :
                        Visibility.Hidden;

                injectBtn.Dispatcher.Invoke(
                    () => {
                        injectBtn.Visibility = visibility;
                        autoInject.IsEnabled = !gtaRunning;
                    },
                    DispatcherPriority.Normal
                );

                await Task.Delay(1000); // Wait for 1 second before checking again
            }
        }

        private void UseSettings(Settings settings)
        {
            advancedMode = settings.AdvancedMode;
            SetAdvancedMode(advancedMode);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        public void SetProgress(int prog)
        {
            progressBar.Visibility = (prog == 0 || prog == 100) ?
                Visibility.Hidden : Visibility.Hidden;
        }

        /*private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (launcherSelect.SelectedIndex == -1)
            {
                MessageBox.Show("pwease choose a launcher tanks");
                return;
            }

            await Fate.LaunchGTA((LauncherType)launcherSelect.SelectedIndex);

            if (autoInject.IsChecked.Value)
            {
                await Fate.AsyncTimeout(Fate.WaitUntilGtaStarts(), 60);
                await Fate.AsyncTimeout(Fate.WaitUntilGtaStarts(), 5);  // for some reason GTA5.exe closes and restarts itself so we must check twice

                if (Fate.IsGtaRunning())
                {
                    //Console.WriteLine("gta is now running; waiting 15 seconds before injecting Fate");

                    await Task.Delay(10000); // wait for 15 seconds before injecting the DLL

                    uint pid = Fate.GetProcessId();
                    string dllPath = await Fate.GetDllPath(); // check fate dll and download if required

                    if (Fate.Inject(dllPath, pid))
                    {
                        statusText.Content = $"Injected Fate into PID {pid}";
                        MessageBox.Show("You're a [Fate User] now! :D");
                    }
                    else
                    {
                        statusText.Content = $"Failed to inject Fate into PID {pid}";
                    }
                }
            }
        }*/

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (launcherSelect.SelectedIndex == -1)
            {
                MessageBox.Show("Please choose a launcher. Tanks!");
                return;
            }

            await Fate.LaunchGTA((LauncherType)launcherSelect.SelectedIndex);

            if (autoInject.IsChecked.Value)
            {
                await Fate.AsyncTimeout(Fate.WaitUntilGtaStarts(), 60);
                await Fate.AsyncTimeout(Fate.WaitUntilGtaStarts(), 5); // For some reason GTA5.exe closes and restarts itself, so we must check twice

                if (Fate.IsGtaRunning())
                {
                    await Task.Delay(15000); // Wait for 15 seconds before injecting the DLL

                    uint pid = Fate.GetProcessId();

                    if (advancedMode)
                    {
                        if (dllCheckboxes.SelectedItems.Count == 0)
                        {
                            MessageBox.Show("Please select a DLL for injection.", "Fate Launchpad");
                            return;
                        }

                        foreach (DLLItem selectedDll in dllCheckboxes.SelectedItems)
                        {
                            if (selectedDll.IsChecked)
                            {
                                string dllPath = selectedDll.Path;

                                if (Fate.Inject(dllPath, pid))
                                {
                                    statusText.Content = $"Injected {Path.GetFileName(dllPath)} into PID {pid}";
                                }
                                else
                                {
                                    statusText.Content = $"Failed to inject {Path.GetFileName(dllPath)} into PID {pid}";
                                    MessageBox.Show("No DLL was injected. You may need to restart the launchpad as an admin.", "Fate Launchpad");
                                }
                            }
                        }
                    }
                    else
                    {
                        string dllPath = await Fate.GetDllPath(); // Check Fate DLL and download if required

                        if (Fate.Inject(dllPath, pid))
                        {
                            statusText.Content = $"Injected Fate into PID {pid}";
                            MessageBox.Show("You're a [Fate User] now! :D");
                        }
                        else
                        {
                            statusText.Content = $"Failed to inject Fate into PID {pid}";
                            MessageBox.Show("No DLL was injected. You may need to restart the launchpad as an admin.", "Fate Launchpad");
                        }
                    }
                }
            }
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            launcherSelect.SelectedIndex = 1;
            SetAdvancedMode(false);

            fateDll = await Fate.GetDllPath();
            LoadPathsFromLaunchpadFile(); // Load the DLL paths from the Launchpad.txt file

            UseSettings(settings);
        }

        private async void autoInject_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SetAdvancedMode(bool am)
        {
            advancedOptions.Width = am ?
                new GridLength(1, GridUnitType.Star) :
                new GridLength(0, GridUnitType.Pixel);

            mainOptions.Width = am ?
                new GridLength(241, GridUnitType.Pixel) :
                new GridLength(1, GridUnitType.Star);

            Width = am ? 650 : 263;

            if (am)
            {
                injectBtn.Content = "Inject";
            }
            else
            {
                injectBtn.Content = "Inject Fate";
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            advancedMode = !advancedMode;

            settings.AdvancedMode = advancedMode;
            settings.Save(SettingsFile);

            SetAdvancedMode(advancedMode);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Process.Start(Fate.FatePath());
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            string p = await Fate.GetDllPath();
            string fn = Path.GetFileName(p);
            string[] spl = fn.Split('.');
            string name = string.Join(".", spl.Take(spl.Length - 1));
            string versionUrl = "https://raw.githubusercontent.com/GoddessZex/Fate/main/version.txt";
            WebClient wc = new WebClient();
            string version = await wc.DownloadStringTaskAsync(versionUrl);
            statusText.Content = $"You are now using {name} {version}";
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            Fate.OpenChangelog();
        }

        private async void injectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (advancedMode)
            {
                uint pid = Fate.GetProcessId();

                if (dllCheckboxes.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a DLL for injection.", "Fate Launchpad");
                    return;
                }

                if (dllCheckboxes.SelectedItem is DLLItem selectedDll)
                {
                    string dllPath = selectedDll.Path;

                    if (Fate.Inject(dllPath, pid))
                    {
                        statusText.Content = $"Injected {Path.GetFileName(dllPath)} into PID {pid}";
                    }
                    else
                    {
                        statusText.Content = $"Failed to inject {Path.GetFileName(dllPath)} into PID {pid}";
                        MessageBox.Show("No DLL was injected, you may need restart the launchpad as Admin.", "Fate Launchpad");
                    }
                }
            }
            else
            {
                uint pid = Fate.GetProcessId();
                string dllPath = await Fate.GetDllPath(); // check fate dll and download if required

                if (Fate.Inject(dllPath, pid))
                {
                    statusText.Content = $"Injected Fate into PID {pid}";
                    MessageBox.Show("You're a [Fate User] now! :D");
                }
                else
                {
                    statusText.Content = $"Failed to inject Fate into PID {pid}";
                    MessageBox.Show("No DLL was injected, you may need restart the launchpad as Admin.", "Fate Launchpad");
                }
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fg = new OpenFileDialog();
            fg.Multiselect = true;
            fg.Filter = "DLL Files|*.dll";

            if (fg.ShowDialog().Value)
            {
                foreach (var file in fg.FileNames)
                {
                    DLLItem item = new DLLItem(file, true); // Set the IsChecked property to true initially
                    dllCheckboxes.Items.Add(item);
                }

                SavePathsToLaunchpadFile(); // Save the updated DLL paths to the Launchpad.txt file
            }
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            var selectedItems = dllCheckboxes.SelectedItems;

            if (dllCheckboxes.SelectedIndex != -1)
            {
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    dllCheckboxes.Items.Remove(selectedItems[i]);
                }

                SavePathsToLaunchpadFile(); // Save the updated DLL paths to the Launchpad.txt file
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }
        }

        private void SavePathsToLaunchpadFile()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fate", "Launchpad.txt");

            JObject json = new JObject();
            json["AdvancedMode"] = settings.AdvancedMode; // Save the current value of the advanced mode

            List<string> paths = new List<string>();

            foreach (DLLItem item in dllCheckboxes.Items)
            {
                if (item.IsChecked)
                {
                    paths.Add(item.Path);
                }
            }

            json["DLLPaths"] = JArray.FromObject(paths);

            File.WriteAllText(filePath, json.ToString());
        }

        private void LoadPathsFromLaunchpadFile()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fate", "Launchpad.txt");

            JObject json = new JObject();

            if (File.Exists(filePath))
            {
                string jsonText = File.ReadAllText(filePath);

                try
                {
                    json = JObject.Parse(jsonText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing Launchpad.txt: {ex.Message}");
                }
            }

            JArray dllPaths = (JArray)json["DLLPaths"];

            if (dllPaths != null)
            {
                foreach (string path in dllPaths)
                {
                    if (File.Exists(path))
                    {
                        DLLItem item = new DLLItem(path, true); // Set the IsChecked property to true for the loaded DLLs
                        dllCheckboxes.Items.Add(item);
                    }
                }
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is DLLItem item)
            {
                item.IsChecked = checkBox.IsChecked ?? false; // Update the IsChecked property of the DLLItem object

                if (checkBox.IsChecked == true)
                {
                    dllCheckboxes.SelectedItems.Add(item);
                }
                else
                {
                    dllCheckboxes.SelectedItems.Remove(item);
                }

                SavePathsToLaunchpadFile(); // Save the updated DLL paths to the Launchpad.txt file
            }
        }
    }
}
