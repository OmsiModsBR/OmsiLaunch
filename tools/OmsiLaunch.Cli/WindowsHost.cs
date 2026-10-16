using System.Runtime.InteropServices;
using System.Windows.Forms;
using OmsiLaunch.Api;

internal static class WindowsHost
{
    public static bool IsActive => string.Equals(Environment.GetEnvironmentVariable("OMSILAUNCH_WINDOWS_HOST"), "1", StringComparison.Ordinal);
    public static bool SuppressConsole => IsActive;

    // Test seam: when set, receives the dialog instead of a real message box.
    internal static Action<string, string>? FailureObserver { get; set; }

    public static void ShowFailure(string code, string message)
    {
        if (!IsActive) return;
        if (FailureObserver is { } observer) { observer(code, message); return; }
        MessageBoxW(IntPtr.Zero, message + "\r\n\r\nCode: " + code + "\r\n\r\nSee .omsilaunch\\diagnostics for details.", "OmsiLaunch", 0x10);
    }

    public static void ShowFailure(IReadOnlyList<LaunchDiagnostic> diagnostics, string fallback)
    {
        var diagnostic = diagnostics.LastOrDefault(x => x.Code.StartsWith("OL_E_", StringComparison.Ordinal));
        ShowFailure(diagnostic?.Code ?? "OL_E_SESSION_START_FAILED", diagnostic?.Message ?? fallback);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr owner, string text, string caption, uint type);
}

// This Windows presentation adapter owns neither OMSI nor recovery. Its stop
// action only signals the canonical session-owner path in Program.cs.
internal sealed class SessionTrayIndicator : IDisposable
{
    private readonly string installationRoot;
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private readonly object gate = new();
    private readonly SessionPlan plan;
    private readonly Func<SessionStatus> status;
    private readonly Action requestCanonicalStop;
    private readonly bool readyInTime;
    private Exception? startupError;
    private NotifyIcon? icon;
    private ContextMenuStrip? menu;
    private ApplicationContext? context;
    private Control? marshal;
    private TrayWindow? window;
    private StatusWindow? statusWindow;
    private StopConfirmationWindow? confirmation;
    private WindowsUiStrings? ui;
    private System.Windows.Forms.Timer? notices;
    private long lastNotice;
    private volatile bool disposed;

    private SessionTrayIndicator(string installationRoot, SessionPlan plan, Func<SessionStatus> status, Action requestCanonicalStop)
    {
        this.installationRoot = installationRoot;
        this.plan = plan;
        this.status = status;
        this.requestCanonicalStop = requestCanonicalStop;
        thread = new Thread(Run) { IsBackground = true, Name = "OmsiLaunch tray" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        readyInTime = ready.Wait(TimeSpan.FromSeconds(2));
        if (!readyInTime) WriteState("startup-timeout");
    }

    public static SessionTrayIndicator? CreateIfAvailable(string installationRoot, SessionPlan plan, Func<SessionStatus> status, Action requestCanonicalStop)
    {
        try
        {
            var indicator = new SessionTrayIndicator(installationRoot, plan, status, requestCanonicalStop);
            if (indicator.readyInTime && indicator.startupError is null && indicator.icon is not null) return indicator;
            // A slow or failed start must never orphan a visible icon. Dispose
            // tells the UI thread to skip (or leave) its loop and remove the icon.
            indicator.Dispose();
            return null;
        }
        catch { return null; }
    }

    private void Run()
    {
        try
        {
            Application.EnableVisualStyles();
            ui = WindowsUiStrings.Resolve();
            context = new ApplicationContext();
            // Hidden control that marshals shutdown requests from foreign
            // threads onto this message loop. Its handle must exist up front.
            marshal = new Control();
            _ = marshal.Handle;
            window = new TrayWindow();
            menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem(ui["Tray.Status"], null, (_, _) => ShowStatus()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(ui["Tray.EndSession"], null, (_, _) => ConfirmAndRequestStop()) { AccessibleDescription = ui["Tray.EndSessionDescription"] });
            icon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? System.Drawing.SystemIcons.Application,
                Text = ui["Tray.Running"],
                Visible = true
            };
            icon.DoubleClick += (_, _) => ShowStatus();
            // NotifyIcon can report (0,0) for shell-hosted mouse events. Read
            // the actual cursor position instead of trusting event coordinates.
            icon.MouseUp += (_, eventArgs) => { if (eventArgs.Button == MouseButtons.Right) ShowMenu(Cursor.Position); };
            window.RestoreRequested += (_, _) => RefreshIcon();
            notices = new System.Windows.Forms.Timer { Interval = 1000 };
            notices.Tick += (_, _) => ShowNewNotices();
            notices.Start();
            WriteState("created");
            // Decide about the loop under the same lock Dispose uses: a Dispose
            // that races startup either posts its shutdown to the loop or, if
            // it wins, makes this thread skip the loop and clean up right away.
            bool runLoop;
            lock (gate) { runLoop = !disposed; }
            SignalReady();
            if (runLoop) Application.Run(context);
            else WriteState("startup-cancelled");
        }
        catch (Exception error)
        {
            startupError = error;
            WriteFailure(error);
            SignalReady();
        }
        finally
        {
            try { statusWindow?.Close(); } catch (Exception error) { WriteFailure(error); }
            notices?.Dispose();
            if (icon is not null) { icon.Visible = false; icon.Dispose(); }
            menu?.Dispose();
            marshal?.Dispose();
            WriteState("removed");
            window?.Dispose();
        }
    }

    private void SignalReady()
    {
        try { ready.Set(); } catch (ObjectDisposedException) { }
    }

    // Runs on the UI thread only (posted by Dispose). Closes whatever the tray
    // has open, then ends the message loop so Run() reaches its cleanup.
    private void ShutdownOnUiThread()
    {
        try
        {
            menu?.Close();
            confirmation?.Close();
            statusWindow?.Close();
        }
        catch (Exception error) { WriteFailure(error); }
        try { context?.ExitThread(); }
        catch (Exception error) { WriteFailure(error); }
    }

    private void RefreshIcon() { if (icon is not null) { icon.Visible = false; icon.Visible = true; } }

    // A runtime module's notice (event module.<id>.notice) appears as a balloon
    // naming the module. The text is the module's data, bounded and without
    // control characters (the bridge sanitizes it); it is never opened or run.
    private void ShowNewNotices()
    {
        if (disposed || icon is null) return;
        try
        {
            var events = status().RuntimeEvents ?? Array.Empty<RuntimeEvent>();
            foreach (var item in events.Where(x => x.Sequence > lastNotice).OrderBy(x => x.Sequence))
            {
                lastNotice = item.Sequence;
                if (!item.Type.StartsWith("module.", StringComparison.Ordinal) || !item.Type.EndsWith(".notice", StringComparison.Ordinal) || item.Type.Length <= "module..notice".Length) continue;
                var module = item.Type["module.".Length..^".notice".Length];
                if (!item.Data.TryGetValue("text", out var text) || text.Length == 0) continue;
                var title = item.Data.TryGetValue("title", out var given) && given.Length != 0 ? given + " (" + module + ")" : module;
                icon.ShowBalloonTip(8000, title.Length <= 63 ? title : title[..63], text.Length <= 255 ? text : text[..255], ToolTipIcon.Info);
            }
        }
        catch (Exception error) { WriteFailure(error); }
    }

    private void ShowMenu(System.Drawing.Point requestedLocation)
    {
        if (menu is null) return;
        var workArea = Screen.FromPoint(requestedLocation).WorkingArea;
        var size = menu.GetPreferredSize(System.Drawing.Size.Empty);
        var x = Math.Clamp(requestedLocation.X, workArea.Left, Math.Max(workArea.Left, workArea.Right - size.Width));
        var y = requestedLocation.Y + size.Height > workArea.Bottom
            ? Math.Max(workArea.Top, workArea.Bottom - size.Height)
            : Math.Max(workArea.Top, requestedLocation.Y);
        // A notification-icon menu requires the owner to be the foreground
        // window first (as NotifyIcon.ContextMenuStrip does); otherwise it may
        // ignore clicks and never close while another app (OMSI) is in front
        // (runtime closure BUG-04).
        window?.BringToForeground();
        menu.Show(new System.Drawing.Point(x, y));
    }

    private void ShowStatus()
    {
        if (disposed) return;
        try
        {
            if (statusWindow is { IsDisposed: false }) { statusWindow.Activate(); return; }
            statusWindow = new StatusWindow(SessionStatusPresenter.Create(plan, status(), ui!), ui!);
            statusWindow.FormClosed += (_, _) => statusWindow = null;
            statusWindow.Show();
        }
        catch (Exception error) { WriteFailure(error); }
    }

    private void ConfirmAndRequestStop()
    {
        if (disposed) return;
        // Tray messages still arrive while the dialog is modal; do not stack a
        // second confirmation on top of the open one.
        if (confirmation is { IsDisposed: false }) { confirmation.Activate(); return; }
        try
        {
            using var dialog = new StopConfirmationWindow(ui!);
            confirmation = dialog;
            DialogResult result;
            try { result = dialog.ShowDialog(); }
            finally { confirmation = null; }
            // WindowsUiStrings has no "stopping" tray text yet, so the icon text
            // stays as is until the canonical owner removes the icon.
            if (result == DialogResult.OK && !disposed) requestCanonicalStop();
        }
        catch (Exception error)
        {
            WriteFailure(error);
            if (!disposed) MessageBox.Show(ui!["Stop.Failed"], ui["Stop.Title"], MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void Dispose()
    {
        Control? target;
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            target = marshal;
        }
        // Closing forms and ExitThread belong to the UI thread; only post to it.
        // If the UI thread has not published its marshaller yet, Run() observes
        // the disposed flag under the lock and skips the loop by itself.
        if (target is not null)
        {
            try { target.BeginInvoke(new Action(ShutdownOnUiThread)); }
            catch (Exception error) { WriteFailure(error); }
        }
        if (Thread.CurrentThread == thread) return;
        if (thread.IsAlive && !thread.Join(TimeSpan.FromSeconds(2))) { WriteState("dispose-timeout"); return; }
        ready.Dispose();
    }

    private void WriteFailure(Exception error) => WriteLog(error.ToString());
    private void WriteState(string state) => WriteLog(state);

    // Tray diagnostics live beside the session host's own logs under the
    // installation root. Logging is best effort and never throws.
    private void WriteLog(string entry)
    {
        try { var directory = Path.Combine(installationRoot, ".omsilaunch", "diagnostics"); Directory.CreateDirectory(directory); File.AppendAllText(Path.Combine(directory, "tray-host.log"), DateTimeOffset.UtcNow.ToString("O") + "\t" + entry + Environment.NewLine); } catch { }
    }

    private sealed class TrayWindow : NativeWindow, IDisposable
    {
        private static readonly int TaskbarCreated = RegisterWindowMessage("TaskbarCreated");
        public event EventHandler? RestoreRequested;
        public TrayWindow() => CreateHandle(new CreateParams());
        protected override void WndProc(ref Message message) { if (message.Msg == TaskbarCreated) RestoreRequested?.Invoke(this, EventArgs.Empty); base.WndProc(ref message); }
        public void Dispose() => DestroyHandle();
        public bool BringToForeground() => Handle != IntPtr.Zero && SetForegroundWindow(Handle);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int RegisterWindowMessage(string value);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    }
}

internal sealed class StatusWindow : Form
{
    public StatusWindow(SessionStatusView view, WindowsUiStrings ui)
    {
        Text = view.Heading; Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? System.Drawing.SystemIcons.Application; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; StartPosition = FormStartPosition.CenterScreen; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; Padding = new Padding(14);
        var root = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Dock = DockStyle.Fill };
        root.Controls.Add(new Label { Text = view.Heading, AutoSize = true, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold), Margin = new Padding(0, 0, 0, 10) });
        foreach (var section in view.Sections)
        {
            var group = new GroupBox { Text = section.Title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 8) };
            var fields = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Padding = new Padding(8) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            foreach (var field in section.Fields) { fields.Controls.Add(new Label { Text = field.Label + ":", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 2, 14, 2) }); fields.Controls.Add(new Label { Text = field.Value, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 2, 0, 2) }); }
            group.Controls.Add(fields); root.Controls.Add(group);
        }
        var close = new Button { Text = ui["Status.Close"], AutoSize = true, Anchor = AnchorStyles.Right };
        close.Click += (_, _) => Close();
        root.Controls.Add(close); CancelButton = close; Controls.Add(root);
    }
}

internal sealed class StopConfirmationWindow : Form
{
    public StopConfirmationWindow(WindowsUiStrings ui)
    {
        Text = ui["Stop.Title"]; Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? System.Drawing.SystemIcons.Application; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; StartPosition = FormStartPosition.CenterScreen; ClientSize = new System.Drawing.Size(420, 142); Padding = new Padding(14);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label { Text = ui["Stop.Message"], AutoSize = false, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.TopLeft, Margin = new Padding(0, 0, 0, 14) }, 0, 0);
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Anchor = AnchorStyles.Right, Margin = Padding.Empty };
        var confirm = new Button { Text = ui["Stop.Confirm"], AutoSize = true, DialogResult = DialogResult.OK }; var cancel = new Button { Text = ui["Stop.Cancel"], AutoSize = true, DialogResult = DialogResult.Cancel };
        actions.Controls.Add(confirm); actions.Controls.Add(cancel); root.Controls.Add(actions, 0, 1); AcceptButton = confirm; CancelButton = cancel; Controls.Add(root);
    }
}
