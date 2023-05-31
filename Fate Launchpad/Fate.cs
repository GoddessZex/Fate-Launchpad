using Microsoft.Win32;
using FateLaunchpad.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// Credits: Zex, Code

namespace FateLaunchpad
{
    public enum LauncherType
    {
        EpicGames = 0,
        Steam = 1,
        RockstarGames = 2,
        None = -1
    }

    internal class Fate
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        static void Debug(string doing, bool succeeded)
        {
            Console.WriteLine(string.Format("{0} succeeded: {1}", doing, succeeded));
        }

        static Random random = new Random();

        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZqwertyuiopasdfghjklzxcvbnm0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string FatePath()
        {
            string fatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fate");
            if (!Directory.Exists(fatePath))
            {
                Directory.CreateDirectory(fatePath);
            }
            return fatePath;
        }

        public static void EnsureFateDirExists()
        {
            if (!Directory.Exists(FatePath()))
                Directory.CreateDirectory(FatePath());

            Directory.CreateDirectory(Path.Combine(FatePath(), "Bin"));
        }

        public static async Task<string> GetDllPath()
        {
            string binFolder = Path.Combine(FatePath(), "Bin");
            string dllName = "Fate.dll";

            EnsureFateDirExists();

            MainWindow.StaticWindow.statusText.Content = "Please Wait";

            using (var client = new WebClient())
            {
                string url = "https://raw.githubusercontent.com/GoddessZex/Fate/main/Fate/Fate.dll";
                string filePath = Path.Combine(binFolder, dllName);
                await client.DownloadFileTaskAsync(new Uri(url), filePath);
            }

            await DownloadHeaderFiles("https://raw.githubusercontent.com/GoddessZex/Fate/main/Theme/Header");
            await DownloadFonts("https://raw.githubusercontent.com/GoddessZex/Fate/main/Theme/Fonts");
            await DownloadSprites("https://raw.githubusercontent.com/GoddessZex/Fate/main/Theme/Sprites");

            MainWindow.StaticWindow.statusText.Content = "Ready to inject";

            if (File.Exists(Path.Combine(binFolder, dllName)))
            {
                return Path.Combine(binFolder, dllName);
            }
            else
            {
                return ""; // return empty string if DLL has not been downloaded
            }
        }

        public static async Task DownloadHeaderFiles(string headerUrl)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string headerPath = Path.Combine(appDataPath, "Fate", "Theme", "Header");

            if (!Directory.Exists(headerPath))
                Directory.CreateDirectory(headerPath);

            await DownloadFileIfNotExists(headerUrl + "/Bar.gif", Path.Combine(headerPath, "Bar.gif"));
        }

        public static async Task DownloadFonts(string fontsUrl)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string fontsPath = Path.Combine(appDataPath, "Fate", "Theme", "Fonts");

            if (!Directory.Exists(fontsPath))
                Directory.CreateDirectory(fontsPath);

            await DownloadFileIfNotExists(fontsUrl + "/Roboto-Bold.ttf", Path.Combine(fontsPath, "Roboto-Bold.ttf"));
            await DownloadFileIfNotExists(fontsUrl + "/Roboto-BoldItalic.ttf", Path.Combine(fontsPath, "Roboto-BoldItalic.ttf"));
            await DownloadFileIfNotExists(fontsUrl + "/Roboto-Regular.ttf", Path.Combine(fontsPath, "Roboto-Regular.ttf"));
        }

        public static async Task DownloadSprites(string spritesUrl)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string spritesPath = Path.Combine(appDataPath, "Fate", "Theme", "Sprites");

            if (!Directory.Exists(spritesPath))
                Directory.CreateDirectory(spritesPath);

            await DownloadFileIfNotExists(spritesUrl + "/ArrowRight.png", Path.Combine(spritesPath, "ArrowRight.png"));
            await DownloadFileIfNotExists(spritesUrl + "/Arrows.png", Path.Combine(spritesPath, "Arrows.png"));
            await DownloadFileIfNotExists(spritesUrl + "/ToggleOff.png", Path.Combine(spritesPath, "ToggleOff.png"));
            await DownloadFileIfNotExists(spritesUrl + "/ToggleOn.png", Path.Combine(spritesPath, "ToggleOn.png"));
        }

        public static async Task DownloadFileIfNotExists(string url, string filePath)
        {
            if (!File.Exists(filePath))
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(new Uri(url), filePath);
                }
            }
        }

        public static bool Inject(string dllName, uint processId)
        {
            string temp = Path.Combine(Path.GetTempPath(), "Fate");

            if (!Directory.Exists(temp))
                Directory.CreateDirectory(temp);

            string tmp = Path.Combine(temp, RandomString(random.Next(6, 10)) + ".dll");
            File.Copy(dllName, tmp);

            dllName = tmp;

            {
                IntPtr p = OpenProcess(1082U, 1, processId);
                string dllFullPath = Path.GetFullPath(dllName);

                Debug("Process open", p != IntPtr.Zero);

                if (p != IntPtr.Zero)
                {
                    IntPtr modHandle = GetModuleHandle("kernel32.dll");
                    IntPtr pAddress = GetProcAddress(modHandle, "LoadLibraryA");

                    Debug("Get process address", pAddress != IntPtr.Zero);

                    if (pAddress != IntPtr.Zero)
                    {
                        IntPtr address = VirtualAllocEx(
                            p, (IntPtr)null,
                            (IntPtr)dllFullPath.Length,
                            12288U, 64U
                        );

                        Debug("Allocate memory", address != IntPtr.Zero);

                        if (address != IntPtr.Zero)
                        {
                            byte[] dllPathBytes = Encoding.UTF8.GetBytes(dllFullPath);

                            int writeMemResult = WriteProcessMemory(
                                p, address, dllPathBytes,
                                (uint)dllPathBytes.Length, 0
                            );

                            Debug("Write dll file path string", writeMemResult != 0);

                            if (writeMemResult != 0)
                            {
                                IntPtr createThreadResult = CreateRemoteThread(
                                    p, (IntPtr)null,
                                    IntPtr.Zero,
                                    pAddress, address,
                                    0U, (IntPtr)null
                                );

                                Debug("Create remote thread", createThreadResult != IntPtr.Zero);

                                if (createThreadResult != IntPtr.Zero)
                                {
                                    Console.WriteLine("Successfully injected DLL!!!");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Unable to inject");

            Console.WriteLine("win32 error code : " + Marshal.GetLastWin32Error());

            return false;
        }

        public static Task<bool> LaunchGTA(LauncherType type)
        {
            switch (type)
            {
                case LauncherType.EpicGames:
                    Process.Start("com.epicgames.launcher://apps/9d2d0eb64d5c44529cece33fe2a46482?action=launch&silent=true");
                    break;

                case LauncherType.Steam:
                    Process.Start("steam://run/271590");
                    break;

                case LauncherType.RockstarGames:
                    try
                    {
                        using (RegistryKey rockstar = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Rockstar Games\\Launcher"))
                        {
                            if (rockstar == null)
                                MessageBox.Show("Rockstar Games might not be installed");

                            string dir = (string)rockstar.GetValue("InstallFolder");

                            if (!string.IsNullOrEmpty(dir))
                                Process.Start(dir + "\\Launcher.exe", "-minmodeApp=gta5");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                    break;

                default:
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public static bool IsGtaRunning() =>
            Process.GetProcessesByName("GTA5").Any();

        public static uint GetProcessId() =>
            (uint)Process.GetProcessesByName("GTA5").First().Id;

        public static async Task WaitUntilGtaStarts()
        {
            while (!IsGtaRunning())
                await Task.Delay(200);
        }

        public static Task AsyncTimeout(Task task, int timeout)
        {
            return Task.WhenAny(task, Task.Delay(timeout * 1000));
        }
        public static void OpenChangelog()
        {
            Changelog chlog = new Changelog();
            chlog.Show();
        }
    }
}
