using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowViewer
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int SW_RESTORE = 9;
        private const int SW_MINIMIZE = 6;
        private const int WM_CLOSE = 0x0010;
        private const int WM_COMMAND = 0x0111;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        private const int SW_SHOW = 5;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GCL_HICON = -14;
        private const int GCL_HICONSM = -34;
        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_TOPMOST = 0x00000008;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private ContextMenuStrip contextMenu;
        private dynamic selectedItem;
        public Form1()
        {
            InitializeComponent();
            UpdateList();
            timer1.Start();
        }
        private void обновитьСписокToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateList();
        }
        private void UpdateList()
        {
            windowList.Items.Clear();
            EnumWindows(new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);
        }
        private bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
        {
            if (IsWindowVisible(hWnd))
            {
                StringBuilder windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                Process process = Process.GetProcessById((int)processId);
                string processName = process.ProcessName;

                IntPtr hIcon = GetClassLongPtr(hWnd, GCL_HICON);
                if (hIcon == IntPtr.Zero)
                {
                    hIcon = GetClassLongPtr(hWnd, GCL_HICONSM);
                }

                Bitmap iconBitmap = null;
                if (hIcon != IntPtr.Zero)
                {
                    try
                    {
                        Icon icon = Icon.FromHandle(hIcon);
                        iconBitmap = new Bitmap(icon.ToBitmap(), new Size(16, 16));
                    }
                    catch
                    {
                    }
                }

                if (iconBitmap == null)
                {
                    iconBitmap = new Bitmap(SystemIcons.Application.ToBitmap(), new Size(16, 16));
                }

                uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                bool isTopMost = (exStyle & WS_EX_TOPMOST) == WS_EX_TOPMOST;

                var listItem = new WindowItem
                {
                    Icon = iconBitmap,
                    WindowTitle = windowText.ToString(),
                    ProcessName = processName,
                    IsTopMost = isTopMost,
                    hWnd = hWnd,
                    Process = process
                };
                windowList.Items.Add(listItem);
            }
            return true;
        }
        private void windowList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= windowList.Items.Count)
            {
                return;
            }
            e.DrawBackground();
            var item = (dynamic)windowList.Items[e.Index];
            string windowTitle = item.WindowTitle;
            string processName = item.ProcessName;
            Bitmap icon = item.Icon;
            bool isTopMost = item.IsTopMost;

            e.Graphics.DrawImage(icon, e.Bounds.Left, e.Bounds.Top);
            e.Graphics.DrawString($"{windowTitle} {processName} TopMost: {isTopMost}", e.Font, Brushes.Black, e.Bounds.Left + icon.Width + 5, e.Bounds.Top);

            e.DrawFocusRectangle();
        }
        private void windowList_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = windowList.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    windowList.SelectedIndex = index;
                    selectedItem = (WindowItem)windowList.Items[index];
                    if (selectedItem == null)
                    {
                        contextMenu.Enabled = false;
                    }
                    else
                    {
                        contextMenu.Enabled = true;

                        bool isTopMost = selectedItem.IsTopMost;
                        if (contextMenu.Items.Count > 7)
                        {
                            contextMenu.Items[6].Visible = isTopMost;
                            contextMenu.Items[7].Visible = !isTopMost;
                        }

                        bool isMinimized = IsIconic(selectedItem.hWnd);
                        bool isMaximized = IsZoomed(selectedItem.hWnd);

                        if (contextMenu.Items.Count > 2)
                        {
                            contextMenu.Items[0].Visible = isMinimized;
                            contextMenu.Items[1].Visible = !isMinimized;
                        }
                    }
                }
            }
        }
        private void OpenProcessFile_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", $"/select,\"{selectedItem.Process.MainModule.FileName}\"");
        }

        private void DisableTopMost_Click(object sender, EventArgs e)
        {
            SetWindowPos(selectedItem.hWnd, new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0004);
        }

        private void EnableTopMost_Click(object sender, EventArgs e)
        {
            SetWindowPos(selectedItem.hWnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0004);
        }
        private void Expand_Click(object sender, EventArgs e)
        {
            ShowWindow(selectedItem.hWnd, SW_RESTORE);
            SetForegroundWindow(selectedItem.hWnd);
        }
        private void Minimize_Click(object sender, EventArgs e)
        {
            ShowWindow(selectedItem.hWnd, SW_MINIMIZE);
        }
        private void Close_Click(object sender, EventArgs e)
        {
            Process.GetProcessById((int)selectedItem.ProcessId).Kill();
        }
        private void SendDialogResult(DialogResult result)
        {
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите элемент из списка.");
                return;
            }

            IntPtr resultValue;
            switch (result)
            {
                case DialogResult.OK:
                    resultValue = (IntPtr)1;
                    break;
                case DialogResult.Cancel:
                    resultValue = (IntPtr)2;
                    break;
                case DialogResult.Abort:
                    resultValue = (IntPtr)3;
                    break;
                case DialogResult.Retry:
                    resultValue = (IntPtr)4;
                    break;
                case DialogResult.Ignore:
                    resultValue = (IntPtr)5;
                    break;
                case DialogResult.Yes:
                    resultValue = (IntPtr)6;
                    break;
                case DialogResult.No:
                    resultValue = (IntPtr)7;
                    break;
                default:
                    resultValue = IntPtr.Zero;
                    break;
            }

            if (resultValue != IntPtr.Zero)
            {
                SendMessage(selectedItem.hWnd, WM_COMMAND, resultValue, IntPtr.Zero);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Развернуть", null, Expand_Click);
            contextMenu.Items.Add("Свернуть", null, Minimize_Click);
            contextMenu.Items.Add("Закрыть", null, Close_Click);

            var dialogMenu = new ToolStripMenuItem("Отправить результат диалога");
            dialogMenu.DropDownItems.Add("OK", null, (s, ev) => SendDialogResult(DialogResult.OK));
            dialogMenu.DropDownItems.Add("Cancel", null, (s, ev) => SendDialogResult(DialogResult.Cancel));
            dialogMenu.DropDownItems.Add("No", null, (s, ev) => SendDialogResult(DialogResult.No));
            dialogMenu.DropDownItems.Add("Yes", null, (s, ev) => SendDialogResult(DialogResult.Yes));
            dialogMenu.DropDownItems.Add("Retry", null, (s, ev) => SendDialogResult(DialogResult.Retry));
            dialogMenu.DropDownItems.Add("Abort", null, (s, ev) => SendDialogResult(DialogResult.Abort));
            dialogMenu.DropDownItems.Add("Ignore", null, (s, ev) => SendDialogResult(DialogResult.Ignore));
            contextMenu.Items.Add(dialogMenu);

            contextMenu.Items.Add("Отключить TopMost", null, DisableTopMost_Click);
            contextMenu.Items.Add("Включить TopMost", null, EnableTopMost_Click);
            contextMenu.Items.Add("Открыть файл процесса", null, OpenProcessFile_Click);

            windowList.ContextMenuStrip = contextMenu;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateList();
        }
    }
    public class WindowItem
    {
        public Bitmap Icon { get; set; }
        public string WindowTitle { get; set; }
        public string ProcessName { get; set; }
        public bool IsTopMost { get; set; }
        public IntPtr hWnd { get; set; }
        public Process Process { get; set; }
    }
}