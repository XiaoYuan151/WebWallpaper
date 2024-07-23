using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace WebWallpaper
{
    public partial class Form1 : Form
    {
        private IntPtr programHandle;
        private bool suppressTextChangedEvent = false;

        public Form1()
        {
            InitializeComponent();
            programHandle = Win32Func.FindWindow("Progman", null);
            IntPtr result = IntPtr.Zero;
            Win32Func.SendMessageTimeout(programHandle, 0x52c, IntPtr.Zero, IntPtr.Zero, 0, 2, ref result);

            Win32Func.EnumWindows((hwnd, lParam) =>
            {
                IntPtr defView = Win32Func.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    IntPtr workerW = Win32Func.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    Win32Func.ShowWindow(workerW, 0);
                }
                return true;
            }, IntPtr.Zero);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Win32Func.SetParent(this.Handle, programHandle);
            this.WindowState = FormWindowState.Maximized;
            this.SetBounds(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            using (var regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (regKey.GetValue("WebWallpaper")?.ToString() != Application.ExecutablePath)
                {
                    if (MessageBox.Show("Do you want to add this application to the AutoRun registry key? (Suggest Add)", "Message", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                    {
                        regKey.SetValue("WebWallpaper", Application.ExecutablePath);
                        setAutoRunToolStripMenuItem.Checked = true;
                    }
                }
                else
                {
                    setAutoRunToolStripMenuItem.Checked = true;
                }
            }

            string url = ReadJsonFile()?.Url ?? "https://jhsec.xiaoyuan151.top";
            chromiumWebBrowser1.Load(url);
        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {
            toolStripTextBox1.Text = "http://";
            toolStripTextBox1.Select(toolStripTextBox1.Text.Length, 0);
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!suppressTextChangedEvent && !toolStripTextBox1.Text.StartsWith("http"))
            {
                toolStripTextBox1.Text = "http://";
                toolStripTextBox1.Select(toolStripTextBox1.Text.Length, 0);
            }
        }

        private void contextMenuStrip1_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            HandleUrlSetting();
        }

        private void setWallpaperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HandleUrlSetting();
        }

        private void refreshWallpaperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chromiumWebBrowser1.Refresh();
        }

        private void setAutoRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (!setAutoRunToolStripMenuItem.Checked)
                {
                    regKey.SetValue("WebWallpaper", Application.ExecutablePath);
                }
                else
                {
                    regKey.DeleteValue("WebWallpaper", false);
                }
                setAutoRunToolStripMenuItem.Checked = !setAutoRunToolStripMenuItem.Checked;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void HandleUrlSetting()
        {
            if (Uri.TryCreate(toolStripTextBox1.Text, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                && toolStripTextBox1.Text != "Type Url Here")
            {
                var url = toolStripTextBox1.Text;
                const string outputPath = @"WebWallpaper.json";
                try
                {
                    var jsonOutput = JsonConvert.SerializeObject(new WebWallpaper { Url = url }, Formatting.Indented);
                    File.WriteAllText(outputPath, jsonOutput);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error writing JSON file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                MessageBox.Show($"Set Url To: {url}", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                chromiumWebBrowser1.Load(url);
            }
            suppressTextChangedEvent = true;
            toolStripTextBox1.Text = "Type Url Here";
            suppressTextChangedEvent = false;
        }

        private static WebWallpaper ReadJsonFile()
        {
            const string filePath = @"WebWallpaper.json";
            if (!File.Exists(filePath))
            {
                try
                {
                    var jsonOutput = JsonConvert.SerializeObject(new WebWallpaper { Url = "https://jhsec.xiaoyuan151.top" }, Formatting.Indented);
                    File.WriteAllText(filePath, jsonOutput);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error writing JSON file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            try
            {
                var jsonContent = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<WebWallpaper>(jsonContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading JSON file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }

    public class WebWallpaper
    {
        public string Url { get; set; }
    }

    public static class Win32Func
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string winName);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint timeout, ref IntPtr result);

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string winName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hwnd, IntPtr parentHwnd);
    }
}
