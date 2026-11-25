using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ESurfingTray
{
    // ---------- 扁平配色 ----------
    internal static class C
    {
        public static readonly Color Bg        = Color.FromArgb(255, 255, 255);
        public static readonly Color Hover     = Color.FromArgb(240, 243, 247);
        public static readonly Color Separator = Color.FromArgb(238, 240, 243);
        public static readonly Color Text      = Color.FromArgb(31, 41, 55);
        public static readonly Color TextSel   = Color.FromArgb(17, 24, 39);
    }

    // ---------- 扁平渲染器：紧凑、圆角、无边框 ----------
    internal sealed class TrayRenderer : ToolStripProfessionalRenderer
    {
        private const int MenuRadius = 8;
        private const int ItemRadius = 6;

        public TrayRenderer() : base(new MyColorTable()) { RoundedEdges = false; }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0 || r.Width <= d || r.Height <= d)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(C.Bg))
                e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item as ToolStripMenuItem;
            if (item == null) return;

            if (item.Enabled && item.Selected)
            {
                var rc = new Rectangle(3, 1, e.Item.Width - 6, e.Item.Height - 2);
                using (var path = RoundedRect(rc, ItemRadius))
                using (var b = new SolidBrush(C.Hover))
                    e.Graphics.FillPath(b, path);
            }
            else
            {
                using (var b = new SolidBrush(C.Bg))
                    e.Graphics.FillRectangle(b, e.Item.ContentRectangle);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var p = new Pen(C.Separator))
                e.Graphics.DrawLine(p, 12, e.Item.Height / 2, e.Item.Width - 12, e.Item.Height / 2);
        }

        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Image == null) return;
            int x = 10;
            int y = (e.Item.Height - e.Image.Height) / 2;
            e.Graphics.DrawImage(e.Image, new Rectangle(x, y, e.Image.Width, e.Image.Height));
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var mi = e.Item as ToolStripMenuItem;
            bool isAutoStart = mi != null && mi.CheckOnClick;

            var rc = new Rectangle(34, 0, e.Item.Width - 34 - 10, e.Item.Height);
            Color color = (e.Item.Selected && e.Item.Enabled) ? C.TextSel : C.Text;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, rc, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (!isAutoStart) return;
            int size = 16;
            int x = 10;
            int y = (e.Item.Height - size) / 2;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (mi.Checked)
            {
                using (var pen = new Pen(Color.FromArgb(34, 197, 94), 2.2f)
                {
                    StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
                })
                    e.Graphics.DrawLines(pen, new Point[]
                    {
                        new Point(x + 2, y + 9),
                        new Point(x + 6, y + 13),
                        new Point(x + size - 2, y + 3)
                    });
            }
            else
            {
                using (var pen = new Pen(Color.FromArgb(156, 163, 175), 2.2f)
                {
                    StartCap = LineCap.Round, EndCap = LineCap.Round
                })
                {
                    e.Graphics.DrawLine(pen, x + 4, y + 4, x + size - 4, y + size - 4);
                    e.Graphics.DrawLine(pen, x + size - 4, y + 4, x + 4, y + size - 4);
                }
            }
        }
    }

    internal sealed class MyColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return C.Bg; } }
        public override Color ImageMarginGradientBegin { get { return C.Bg; } }
        public override Color ImageMarginGradientMiddle { get { return C.Bg; } }
        public override Color ImageMarginGradientEnd { get { return C.Bg; } }
        public override Color MenuItemSelected { get { return C.Hover; } }
        public override Color MenuItemBorder { get { return C.Hover; } }
    }

    // ---------- Win32 控制台信号（优雅退出） ----------
    internal static class Win32
    {
        public const uint CTRL_C_EVENT = 0;
        public const uint CTRL_BREAK_EVENT = 1;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    }

    // ---------- HTTP 小工具 ----------
    internal static class Api
    {
        public static string Get(string url)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 3000;
                req.ReadWriteTimeout = 3000;
                req.UserAgent = "ESurfingTray";
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream()))
                    return sr.ReadToEnd();
            }
            catch { return null; }
        }

        public static int StatusCode(string url)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 3000;
                req.AllowAutoRedirect = false;
                using (var resp = (HttpWebResponse)req.GetResponse())
                    return (int)resp.StatusCode;
            }
            catch (WebException we)
            {
                if (we.Response != null) return (int)((HttpWebResponse)we.Response).StatusCode;
                return -1;
            }
            catch { return -1; }
        }

        public static int Post(string url)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.Timeout = 3000;
                req.ReadWriteTimeout = 3000;
                req.UserAgent = "ESurfingTray";
                req.ContentLength = 0;
                using (var resp = (HttpWebResponse)req.GetResponse())
                    return (int)resp.StatusCode;
            }
            catch (WebException we)
            {
                if (we.Response != null) return (int)((HttpWebResponse)we.Response).StatusCode;
                return -1;
            }
            catch { return -1; }
        }

        public static bool JsonBool(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)");
            return m.Success && m.Groups[1].Value == "true";
        }

        public static string JsonStr(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : "";
        }
    }

    // ---------- 状态窗口 ----------
    internal sealed class StatusForm : Form
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly string _manageUrl;
        private readonly string _fallbackAccount;
        private readonly Action _openManage;
        private readonly Label _authVal;
        private readonly Label _netVal;
        private readonly Label _acctVal;
        private bool _closing;

        private static readonly Color Green  = Color.FromArgb(34, 197, 94);
        private static readonly Color Yellow = Color.FromArgb(245, 158, 11);
        private static readonly Color Red    = Color.FromArgb(239, 68, 68);
        private static readonly Color Gray   = Color.FromArgb(156, 163, 175);
        private static readonly Color TextC  = Color.FromArgb(31, 41, 55);
        private static readonly Color LabelC = Color.FromArgb(107, 114, 128);

        public StatusForm(string manageUrl, string fallbackAccount, Action openManage)
        {
            _manageUrl = manageUrl;
            _fallbackAccount = fallbackAccount;
            _openManage = openManage;

            Text = "天翼校园客户端 · 状态";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(384, 340);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);

            Controls.Add(new Label
            {
                Text = "天翼校园客户端",
                AutoSize = true,
                Location = new Point(24, 20),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextC
            });
            Controls.Add(new Label
            {
                Text = "状态监控 · " + manageUrl,
                AutoSize = true,
                Location = new Point(26, 50),
                ForeColor = LabelC
            });

            Controls.Add(MakeName("认证状态", 90));
            _authVal = MakeValue(112); Controls.Add(_authVal);

            Controls.Add(MakeName("联网状态", 152));
            _netVal = MakeValue(174); Controls.Add(_netVal);

            Controls.Add(MakeName("账号", 214));
            _acctVal = MakeValue(236); Controls.Add(_acctVal);

            var openBtn = new Button
            {
                Text = "打开管理页",
                AutoSize = true,
                Location = new Point(24, 276),
                FlatStyle = FlatStyle.Flat,
                ForeColor = TextC
            };
            openBtn.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            openBtn.Click += (s, e) => _openManage();
            Controls.Add(openBtn);

            var reauthBtn = new Button
            {
                Text = "强制重认证",
                AutoSize = true,
                Location = new Point(140, 276),
                FlatStyle = FlatStyle.Flat,
                ForeColor = TextC
            };
            reauthBtn.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            reauthBtn.Click += (s, e) => ReAuthenticate();
            Controls.Add(reauthBtn);

            Controls.Add(new Label
            {
                Text = "关闭此窗口不影响后台运行 · 右键托盘退出",
                AutoSize = true,
                Location = new Point(24, 312),
                ForeColor = LabelC,
                Font = new Font("Segoe UI", 8f)
            });

            FormClosing += (s, e) => { if (!_closing) { e.Cancel = true; Hide(); } };

            _timer = new System.Windows.Forms.Timer { Interval = 3000 };
            _timer.Tick += (s, e) => RefreshStatus();
            Shown += (s, e) => { RefreshStatus(); _timer.Start(); };
        }

        public void ForceClose()
        {
            _closing = true;
            _timer.Stop();
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _timer.Stop(); _timer.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

        private static Label MakeName(string text, int y)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(24, y),
                ForeColor = LabelC
            };
        }

        private static Label MakeValue(int y)
        {
            return new Label
            {
                Text = "●  加载中…",
                AutoSize = true,
                Location = new Point(24, y),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Gray
            };
        }

        private void RefreshStatus()
        {
            // 联网状态（先取，供认证文案联动判断）
            int code = Api.StatusCode(_manageUrl + "/api/status/online");
            string netText; Color netColor;
            switch (code)
            {
                case 204: netText = "已连接互联网"; netColor = Green; break;
                case 302: netText = "需要认证"; netColor = Yellow; break;
                case 503: netText = "未连接互联网"; netColor = Red; break;
                case -1: netText = "无法访问"; netColor = Red; break;
                default: netText = "状态码 " + code; netColor = Gray; break;
            }
            SetVal(_netVal, netText, netColor);

            // 认证状态
            string authJson = Api.Get(_manageUrl + "/api/status/auth");
            string authText; Color authColor;
            if (string.IsNullOrEmpty(authJson))
            {
                authText = "客户端未运行"; authColor = Red;
            }
            else
            {
                bool reachable = Api.JsonBool(authJson, "reachable");
                bool authed = Api.JsonBool(authJson, "status");
                string reason = Api.JsonStr(authJson, "reason");
                string err = Api.JsonStr(authJson, "errorMessage");

                if (reason == "config") { authText = "等待配置"; authColor = Gray; }
                else if (reason == "disabled") { authText = "已停用"; authColor = Gray; }
                else if (!reachable) { authText = "认证进程未运行"; authColor = Red; }
                else if (authed) { authText = "已认证"; authColor = Green; }
                else if (code == 204) { authText = "已联网 · 无需认证"; authColor = Green; }
                else if (code == 302) { authText = "待认证"; authColor = Yellow; }
                else { authText = "未认证"; authColor = Yellow; }

                if (!string.IsNullOrEmpty(err)) authText += " · " + err;
            }
            SetVal(_authVal, authText, authColor);

            // 账号
            string cfg = Api.Get(_manageUrl + "/api/getConfigs");
            string acct = string.IsNullOrEmpty(cfg) ? null : Api.JsonStr(cfg, "username");
            if (string.IsNullOrEmpty(acct)) acct = _fallbackAccount;
            if (string.IsNullOrEmpty(acct)) acct = "—";
            SetVal(_acctVal, acct, TextC);
        }

        private void ReAuthenticate()
        {
            int code = Api.Post(_manageUrl + "/api/restartAuth");
            string msg;
            if (code == 204) msg = "认证进程已重启，正在重新检测网络";
            else if (code == -1) msg = "无法连接管理页，客户端可能未运行";
            else if (code == 503) msg = "认证进程不可达，请求被拒绝";
            else msg = "请求失败，状态码 " + code;

            MessageBox.Show(msg, "强制重认证", MessageBoxButtons.OK,
                code == 204 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private static void SetVal(Label lbl, string text, Color color)
        {
            lbl.Text = "●  " + text;
            lbl.ForeColor = color;
        }
    }

    // ---------- 托盘应用 ----------
    public sealed class TrayApp : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ToolStripMenuItem _autoStartItem;
        private StatusForm _statusForm;

        private readonly Bitmap _openIcon;
        private readonly Bitmap _exitIcon;

        private const string RunKeyPath   = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "ESurfingTray";
        private const string ClientExePattern = "ESurfingClient-*.exe";
        private const string ManageUrl = "http://127.0.0.1:8888";

        public TrayApp()
        {
            _openIcon = Res("ESurfingTray.open.png");
            _exitIcon = Res("ESurfingTray.exit.png");

            Icon icon;
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ESurfingTray.icon.ico"))
                icon = (s == null) ? SystemIcons.Application : new Icon(s);

            var menu = BuildMenu();

            _trayIcon = new NotifyIcon()
            {
                Icon = icon,
                Text = "天翼校园客户端",
                Visible = true,
                ContextMenuStrip = menu
            };
            _trayIcon.DoubleClick += (s, e) => ShowStatusWindow();

            EnsureClientRunning();
            ShowStatusWindow();
        }

        private static Bitmap Res(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                return s == null ? null : new Bitmap(s);
        }

        private ContextMenuStrip BuildMenu()
        {
            const int W = 136, ItemH = 32;
            const int Radius = 8;

            var menu = new ContextMenuStrip()
            {
                Renderer = new TrayRenderer(),
                AutoSize = true,
                Padding = new Padding(0),
                ShowImageMargin = false,
                ShowCheckMargin = false,
                ShowItemToolTips = false,
                DropShadowEnabled = true,
                GripMargin = new Padding(0),
                Font = new Font("Segoe UI", 9f)
            };

            menu.Opening += (s, e) =>
            {
                menu.Region = RoundedRegion(new Rectangle(Point.Empty, menu.ClientSize), Radius);
            };

            var open = MakeItem("打开管理页面", _openIcon, ItemH, W);
            open.Click += (s, e) => OpenManagePage();
            menu.Items.Add(open);

            var reauth = MakeItem("强制重认证", null, ItemH, W);
            reauth.Click += (s, e) => ReAuthenticateTray();
            menu.Items.Add(reauth);

            _autoStartItem = MakeItem("开机自启", null, ItemH, W);
            _autoStartItem.CheckOnClick = true;
            _autoStartItem.Checked = IsAutoStartEnabled();
            _autoStartItem.CheckedChanged += (s, e) =>
            {
                SetAutoStart(_autoStartItem.Checked);
                _autoStartItem.Invalidate();
            };
            menu.Items.Add(_autoStartItem);

            var exit = MakeItem("退出", _exitIcon, ItemH, W);
            exit.Click += (s, e) => ExitApp();
            menu.Items.Add(exit);

            return menu;
        }

        private static Region RoundedRegion(Rectangle r, int radius)
        {
            using (var path = new GraphicsPath())
            {
                int d = radius * 2;
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return new Region(path);
            }
        }

        private static ToolStripMenuItem MakeItem(string text, Image img, int h, int w)
        {
            return new ToolStripMenuItem(text, img)
            {
                AutoSize = false,
                Height = h,
                Width = w,
                Margin = new Padding(0),
                Padding = new Padding(0),
                ImageScaling = ToolStripItemImageScaling.None
            };
        }

        private string SelfPath
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    var v = key != null ? key.GetValue(RunValueName) as string : null;
                    return !string.IsNullOrEmpty(v);
                }
            }
            catch { return false; }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                    ?? Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (enable)
                        key.SetValue(RunValueName, "\"" + SelfPath + "\"");
                    else
                        key.DeleteValue(RunValueName, false);
                }
                _trayIcon.ShowBalloonTip(2000, "开机自启", enable ? "已开启" : "已关闭", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置开机自启失败：" + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenManagePage()
        {
            try
            {
                Process.Start(new ProcessStartInfo(ManageUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开管理页面失败：" + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReAuthenticateTray()
        {
            int code = Api.Post(ManageUrl + "/api/restartAuth");
            string msg;
            if (code == 204) msg = "认证进程已重启，正在重新检测网络";
            else if (code == -1) msg = "无法连接管理页，客户端可能未运行";
            else if (code == 503) msg = "认证进程不可达，请求被拒绝";
            else msg = "请求失败，状态码 " + code;

            _trayIcon.ShowBalloonTip(2500, "强制重认证", msg,
                code == 204 ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        private void ShowStatusWindow()
        {
            if (_statusForm == null || _statusForm.IsDisposed)
                _statusForm = new StatusForm(ManageUrl, ReadAccountFromConfig(), OpenManagePage);
            if (!_statusForm.Visible) _statusForm.Show();
            _statusForm.Activate();
        }

        private string ReadAccountFromConfig()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ESurfingClient.json");
                return Api.JsonStr(File.ReadAllText(path), "username");
            }
            catch { return ""; }
        }

        // 静默启动客户端：无控制台窗口
        private void EnsureClientRunning()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            string[] files;
            try { files = Directory.GetFiles(dir, ClientExePattern); }
            catch { files = new string[0]; }

            if (files.Length == 0)
            {
                _trayIcon.ShowBalloonTip(3000, "提示",
                    "未找到客户端程序，请将托盘与本程序放在同一目录。", ToolTipIcon.Warning);
                return;
            }

            var baseName = Path.GetFileNameWithoutExtension(files[0]);
            if (Process.GetProcessesByName(baseName).Length > 0) return;

            try
            {
                Process.Start(new ProcessStartInfo(files[0])
                {
                    WorkingDirectory = dir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip(3000, "提示",
                    "启动客户端失败：" + ex.Message, ToolTipIcon.Error);
            }
        }

        private List<Process> GetClientProcesses()
        {
            var result = new List<Process>();
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            string[] files;
            try { files = Directory.GetFiles(dir, ClientExePattern); }
            catch { files = new string[0]; }

            foreach (var f in files)
            {
                var baseName = Path.GetFileNameWithoutExtension(f);
                try { result.AddRange(Process.GetProcessesByName(baseName)); }
                catch { }
            }
            return result;
        }

        // 优雅退出：向客户端 web/auth 进程组发 Ctrl+Break，触发登出（term）并优雅关闭。
        // 实测 CTRL_C 对隐藏 console 无效，必须用 CTRL_BREAK；supervisor 不是组 leader（发信号失败），由 KillClients 兜底强杀。
        private void GracefulStopClients()
        {
            foreach (var p in GetClientProcesses())
            {
                try
                {
                    if (Win32.AttachConsole((uint)p.Id))
                    {
                        Win32.SetConsoleCtrlHandler(IntPtr.Zero, true);
                        Win32.GenerateConsoleCtrlEvent(Win32.CTRL_BREAK_EVENT, (uint)p.Id);
                        Win32.FreeConsole();
                    }
                }
                catch { }
            }
        }

        // 强杀所有相关进程（兜底）
        private void KillClients()
        {
            foreach (var p in GetClientProcesses())
                try { p.Kill(); } catch { }
        }

        private void ExitApp()
        {
            _trayIcon.Visible = false;
            if (_statusForm != null)
            {
                try { _statusForm.ForceClose(); } catch { }
            }

            // 后台线程：CTRL_BREAK 优雅关闭（auth 登出 term + web 退出）→ 等待登出完成 → 强杀 supervisor 与残留 → 退出
            var t = new Thread(() =>
            {
                GracefulStopClients();
                Thread.Sleep(5000);   // 留足 auth 完成 term 登出（正常 1~2s，网络慢时更长）
                KillClients();        // 杀 supervisor（阻止重新拉起）及任何残留
                Environment.Exit(0);
            });
            t.IsBackground = true;
            t.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trayIcon != null) { _trayIcon.Dispose(); _trayIcon = null; }
            }
            base.Dispose(disposing);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, "ESurfingTray_SingleInstance", out createdNew))
            {
                if (!createdNew) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp());
            }
        }
    }
}
