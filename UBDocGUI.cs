using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml.Linq;

internal static class UiTheme
{
    public static readonly Color AppBack = Color.FromArgb(8, 9, 10);
    public static readonly Color PanelBack = Color.FromArgb(15, 16, 17);
    public static readonly Color SurfaceBack = Color.FromArgb(25, 26, 27);
    public static readonly Color InputBack = Color.FromArgb(18, 19, 21);
    public static readonly Color DisabledBack = Color.FromArgb(24, 25, 27);
    public static readonly Color PrimaryText = Color.FromArgb(247, 248, 248);
    public static readonly Color SecondaryText = Color.FromArgb(208, 214, 224);
    public static readonly Color MutedText = Color.FromArgb(138, 143, 152);
    public static readonly Color DisabledText = Color.FromArgb(98, 102, 109);
    public static readonly Color Accent = Color.FromArgb(94, 106, 210);
    public static readonly Color AccentHover = Color.FromArgb(113, 112, 255);
    public static readonly Color AccentPressed = Color.FromArgb(79, 88, 184);
    public static readonly Color BorderSubtle = Color.FromArgb(13, 255, 255, 255);
    public static readonly Color BorderStandard = Color.FromArgb(20, 255, 255, 255);
    public static readonly Color GhostFill = Color.FromArgb(10, 255, 255, 255);
    public static readonly Color GhostHover = Color.FromArgb(18, 255, 255, 255);
    public static readonly Color GhostPressed = Color.FromArgb(25, 255, 255, 255);

    private static readonly string UiFontName = ResolveFontName(new string[] { "Inter", "Segoe UI Variable Display", "Segoe UI" });
    private static readonly string MonoFontName = ResolveFontName(new string[] { "Berkeley Mono", "Cascadia Mono", "Consolas" });

    public static Font Font(float size, FontStyle style)
    {
        return new Font(UiFontName, size, style, GraphicsUnit.Point);
    }

    public static Font MonoFont(float size, FontStyle style)
    {
        return new Font(MonoFontName, size, style, GraphicsUnit.Point);
    }

    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 1)
        {
            path.AddRectangle(bounds);
            return path;
        }

        Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static string ResolveFontName(string[] names)
    {
        try
        {
            using (InstalledFontCollection fonts = new InstalledFontCollection())
            {
                foreach (string name in names)
                {
                    foreach (FontFamily family in fonts.Families)
                    {
                        if (string.Equals(family.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return family.Name;
                        }
                    }
                }
            }
        }
        catch
        {
        }
        return names[names.Length - 1];
    }
}

internal sealed class LinearSurfacePanel : Panel
{
    private Color fillColor = UiTheme.PanelBack;
    private Color borderColor = UiTheme.BorderStandard;
    private int radius = 8;

    public LinearSurfacePanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = fillColor;
    }

    public Color FillColor
    {
        get { return fillColor; }
        set
        {
            fillColor = value;
            BackColor = value;
            Invalidate();
        }
    }

    public Color BorderColor
    {
        get { return borderColor; }
        set
        {
            borderColor = value;
            Invalidate();
        }
    }

    public int Radius
    {
        get { return radius; }
        set
        {
            radius = Math.Max(0, value);
            Invalidate();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent == null ? UiTheme.AppBack : Parent.BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (GraphicsPath path = UiTheme.CreateRoundedRectangle(rect, radius))
        using (SolidBrush brush = new SolidBrush(fillColor))
        using (Pen pen = new Pen(borderColor))
        {
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }
    }
}

internal sealed class LinearButton : Button
{
    private bool mouseOver;
    private bool mouseDown;

    public Color FillColor = UiTheme.GhostFill;
    public Color HoverColor = UiTheme.GhostHover;
    public Color PressedColor = UiTheme.GhostPressed;
    public Color BorderColor = UiTheme.BorderStandard;
    public Color TextColor = UiTheme.SecondaryText;
    public int Radius = 6;

    public LinearButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        BackColor = UiTheme.PanelBack;
        ForeColor = TextColor;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        mouseOver = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        mouseOver = false;
        mouseDown = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        mouseDown = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        mouseDown = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent == null ? UiTheme.AppBack : Parent.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Color fill = Enabled ? (mouseDown ? PressedColor : (mouseOver ? HoverColor : FillColor)) : UiTheme.DisabledBack;
        Color border = Enabled ? BorderColor : UiTheme.BorderSubtle;
        Color text = Enabled ? TextColor : UiTheme.DisabledText;
        Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

        using (GraphicsPath path = UiTheme.CreateRoundedRectangle(rect, Radius))
        using (SolidBrush brush = new SolidBrush(fill))
        using (Pen pen = new Pen(border))
        {
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class LinearCheckBox : CheckBox
{
    private bool mouseOver;

    public LinearCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AutoSize = false;
        Font = UiTheme.Font(9F, FontStyle.Regular);
        ForeColor = UiTheme.SecondaryText;
        BackColor = UiTheme.PanelBack;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        mouseOver = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        mouseOver = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        Invalidate();
        base.OnCheckedChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent == null ? UiTheme.AppBack : Parent.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int boxSize = 18;
        Rectangle boxRect = new Rectangle(0, (Height - boxSize) / 2, boxSize, boxSize);
        Color fill = Checked ? UiTheme.Accent : (mouseOver ? UiTheme.SurfaceBack : UiTheme.InputBack);
        Color border = Checked ? UiTheme.AccentHover : UiTheme.BorderStandard;
        Color text = Enabled ? UiTheme.SecondaryText : UiTheme.DisabledText;

        if (!Enabled)
        {
            fill = UiTheme.DisabledBack;
            border = UiTheme.BorderSubtle;
        }

        using (GraphicsPath path = UiTheme.CreateRoundedRectangle(boxRect, 4))
        using (SolidBrush brush = new SolidBrush(fill))
        using (Pen pen = new Pen(border))
        {
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }

        if (Checked)
        {
            using (Pen checkPen = new Pen(UiTheme.PrimaryText, 2F))
            {
                checkPen.StartCap = LineCap.Round;
                checkPen.EndCap = LineCap.Round;
                e.Graphics.DrawLines(checkPen, new Point[] {
                    new Point(5, boxRect.Top + 9),
                    new Point(8, boxRect.Top + 12),
                    new Point(13, boxRect.Top + 6)
                });
            }
        }

        Rectangle textRect = new Rectangle(28, 0, Width - 28, Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textRect,
            text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class NaturalLogTextBox : TextBox
{
    public event MouseEventHandler WheelRequested;

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (WheelRequested != null)
        {
            WheelRequested(this, e);
            return;
        }

        base.OnMouseWheel(e);
    }
}

internal sealed class LinearScrollBar : Control
{
    private int maximum;
    private int largeChange = 1;
    private int value;
    private bool mouseOver;
    private bool dragging;
    private int dragStartY;
    private int dragStartValue;

    public event EventHandler ValueChanged;

    public LinearScrollBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = false;
        Cursor = Cursors.Hand;
        BackColor = UiTheme.InputBack;
    }

    public int Maximum
    {
        get { return maximum; }
    }

    public int Value
    {
        get { return value; }
        set { SetValue(value, true); }
    }

    public void SetScrollValues(int newMaximum, int newLargeChange, int newValue)
    {
        maximum = Math.Max(0, newMaximum);
        largeChange = Math.Max(1, newLargeChange);
        Visible = maximum > 0;
        SetValue(newValue, false);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent == null ? UiTheme.InputBack : Parent.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle track = GetTrackRectangle();
        using (GraphicsPath trackPath = UiTheme.CreateRoundedRectangle(track, 4))
        using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
        {
            e.Graphics.FillPath(trackBrush, trackPath);
        }

        if (maximum <= 0)
        {
            return;
        }

        Color thumbColor = dragging
            ? UiTheme.AccentHover
            : (mouseOver ? Color.FromArgb(150, UiTheme.SecondaryText) : Color.FromArgb(92, UiTheme.SecondaryText));

        using (GraphicsPath thumbPath = UiTheme.CreateRoundedRectangle(GetThumbRectangle(), 4))
        using (SolidBrush thumbBrush = new SolidBrush(thumbColor))
        {
            e.Graphics.FillPath(thumbBrush, thumbPath);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        mouseOver = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (!dragging)
        {
            mouseOver = false;
            Invalidate();
        }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && maximum > 0)
        {
            Focus();
            Rectangle thumb = GetThumbRectangle();
            if (thumb.Contains(e.Location))
            {
                dragging = true;
                dragStartY = e.Y;
                dragStartValue = value;
                Capture = true;
            }
            else
            {
                Value = value + (e.Y < thumb.Top ? -largeChange : largeChange);
            }
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (dragging && maximum > 0)
        {
            Rectangle track = GetTrackRectangle();
            Rectangle thumb = GetThumbRectangle();
            int travel = Math.Max(1, track.Height - thumb.Height);
            int delta = (int)Math.Round((e.Y - dragStartY) * (maximum / (double)travel));
            Value = dragStartValue + delta;
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (dragging)
        {
            dragging = false;
            Capture = false;
            mouseOver = ClientRectangle.Contains(PointToClient(Cursor.Position));
            Invalidate();
        }

        base.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        int lines = SystemInformation.MouseWheelScrollLines;
        if (lines <= 0 || lines > 20)
        {
            lines = 3;
        }

        int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;
        if (notches == 0)
        {
            notches = e.Delta > 0 ? 1 : -1;
        }

        Value = value - notches * lines;
        base.OnMouseWheel(e);
    }

    protected override void OnResize(EventArgs e)
    {
        Invalidate();
        base.OnResize(e);
    }

    private void SetValue(int newValue, bool raiseChanged)
    {
        int clamped = Math.Max(0, Math.Min(maximum, newValue));
        if (clamped == value)
        {
            return;
        }

        value = clamped;
        Invalidate();
        if (raiseChanged && ValueChanged != null)
        {
            ValueChanged(this, EventArgs.Empty);
        }
    }

    private Rectangle GetTrackRectangle()
    {
        int width = Math.Min(8, Math.Max(4, Width));
        int x = (Width - width) / 2;
        return new Rectangle(x, 2, width, Math.Max(1, Height - 4));
    }

    private Rectangle GetThumbRectangle()
    {
        Rectangle track = GetTrackRectangle();
        if (maximum <= 0)
        {
            return track;
        }

        int total = maximum + largeChange;
        int thumbHeight = total <= 0
            ? track.Height
            : (int)Math.Round(track.Height * (largeChange / (double)total));
        thumbHeight = Math.Max(24, Math.Min(track.Height, thumbHeight));

        int travel = Math.Max(0, track.Height - thumbHeight);
        int thumbTop = track.Top;
        if (maximum > 0 && travel > 0)
        {
            thumbTop += (int)Math.Round(travel * (value / (double)maximum));
        }

        return new Rectangle(track.Left, thumbTop, track.Width, thumbHeight);
    }
}

internal sealed class UBDocGuiForm : Form
{
    private const int EM_LINESCROLL = 0x00B6;
    private const int EM_GETLINECOUNT = 0x00BA;
    private const int EM_GETFIRSTVISIBLELINE = 0x00CE;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private readonly string appDir;
    private readonly string defaultOutput;
    private readonly string logDir;
    private readonly string lastRunLog;
    private readonly string guiErrorLog;
    private readonly string settingsPath;

    private TextBox urlBox;
    private TextBox outputBox;
    private NaturalLogTextBox logBox;
    private LinearScrollBar logScrollBar;
    private Button browseButton;
    private Button runButton;
    private CheckBox keepImagesBox;
    private LinearSurfacePanel urlInputPanel;
    private LinearSurfacePanel outputInputPanel;
    private bool updatingLogScrollBar;

    private readonly StringBuilder stdoutBuffer = new StringBuilder();
    private readonly StringBuilder stderrBuffer = new StringBuilder();
    private readonly object bufferLock = new object();

    public UBDocGuiForm()
    {
        appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        defaultOutput = Path.Combine(appDir, "PDF");
        logDir = Path.Combine(appDir, "Logs");
        lastRunLog = Path.Combine(logDir, "last_run.log");
        guiErrorLog = Path.Combine(logDir, "gui_error.log");
        settingsPath = Path.Combine(appDir, "settings.ini");

        ServicePointManager.SecurityProtocol =
            (SecurityProtocolType)768 | (SecurityProtocolType)3072 | SecurityProtocolType.Tls;

        InitializeComponent();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyNativeTitleBarTheme();
    }

    private void InitializeComponent()
    {
        Text = "TLS PDF Downloader";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = true;
        MaximizeBox = true;
        ControlBox = true;
        ClientSize = new Size(780, 560);
        MinimumSize = new Size(720, 520);
        BackColor = UiTheme.AppBack;
        ForeColor = UiTheme.PrimaryText;
        Font = UiTheme.Font(9F, FontStyle.Regular);
        DoubleBuffered = true;

        Icon loadedIcon = LoadApplicationIcon();
        if (loadedIcon != null)
        {
            Icon = loadedIcon;
        }

        LinearSurfacePanel toolPanel = new LinearSurfacePanel();
        toolPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        toolPanel.Location = new Point(24, 22);
        toolPanel.Size = new Size(ClientSize.Width - 48, 228);
        toolPanel.FillColor = UiTheme.PanelBack;
        toolPanel.BorderColor = UiTheme.BorderStandard;
        toolPanel.Radius = 12;

        Label panelTitle = CreateLabel("문서 변환", 24, 20, 220, 24, UiTheme.Font(10F, FontStyle.Regular), UiTheme.PrimaryText);
        Label urlLabel = CreateLabel("ubdoc URL", 24, 56, 160, 22, UiTheme.Font(8.5F, FontStyle.Regular), UiTheme.SecondaryText);

        urlInputPanel = CreateInputPanel(24, 80, toolPanel.Width - 48, 38);
        urlInputPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        urlBox = CreateInputBox("");
        urlBox.Location = new Point(13, 10);
        urlBox.Size = new Size(urlInputPanel.Width - 26, 18);
        urlBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        urlBox.Text = "";
        urlInputPanel.Controls.Add(urlBox);

        Label outputLabel = CreateLabel("PDF 저장 폴더", 24, 130, 180, 22, UiTheme.Font(8.5F, FontStyle.Regular), UiTheme.SecondaryText);

        outputInputPanel = CreateInputPanel(24, 154, toolPanel.Width - 292, 38);
        outputInputPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        outputBox = CreateInputBox(LoadLastOutputDir());
        outputBox.Location = new Point(13, 10);
        outputBox.Size = new Size(outputInputPanel.Width - 26, 18);
        outputBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        outputBox.Text = LoadLastOutputDir();
        outputInputPanel.Controls.Add(outputBox);

        browseButton = new LinearButton();
        browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseButton.Location = new Point(toolPanel.Width - 262, 154);
        browseButton.Size = new Size(96, 38);
        browseButton.Text = "찾기";
        browseButton.Font = UiTheme.Font(9F, FontStyle.Regular);
        browseButton.Click += BrowseButton_Click;

        keepImagesBox = new LinearCheckBox();
        keepImagesBox.Location = new Point(24, 196);
        keepImagesBox.Size = new Size(240, 24);
        keepImagesBox.Text = "PNG 이미지 남기기";
        keepImagesBox.Checked = false;

        runButton = new LinearButton();
        runButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        runButton.Location = new Point(toolPanel.Width - 154, 154);
        runButton.Size = new Size(130, 38);
        runButton.Text = "PDF 저장";
        runButton.Font = UiTheme.Font(9F, FontStyle.Regular);
        ((LinearButton)runButton).FillColor = UiTheme.Accent;
        ((LinearButton)runButton).HoverColor = UiTheme.AccentHover;
        ((LinearButton)runButton).PressedColor = UiTheme.AccentPressed;
        ((LinearButton)runButton).BorderColor = UiTheme.Accent;
        ((LinearButton)runButton).TextColor = UiTheme.PrimaryText;
        runButton.Click += RunButton_Click;

        toolPanel.Controls.Add(panelTitle);
        toolPanel.Controls.Add(urlLabel);
        toolPanel.Controls.Add(urlInputPanel);
        toolPanel.Controls.Add(outputLabel);
        toolPanel.Controls.Add(outputInputPanel);
        toolPanel.Controls.Add(browseButton);
        toolPanel.Controls.Add(keepImagesBox);
        toolPanel.Controls.Add(runButton);

        LinearSurfacePanel logPanel = new LinearSurfacePanel();
        logPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logPanel.Location = new Point(24, 266);
        logPanel.Size = new Size(ClientSize.Width - 48, ClientSize.Height - 290);
        logPanel.FillColor = UiTheme.PanelBack;
        logPanel.BorderColor = UiTheme.BorderStandard;
        logPanel.Radius = 12;

        Label logTitle = CreateLabel("진행 로그", 24, 18, 200, 24, UiTheme.Font(10F, FontStyle.Regular), UiTheme.PrimaryText);

        LinearSurfacePanel logInputPanel = CreateInputPanel(24, 50, logPanel.Width - 48, logPanel.Height - 74);
        logInputPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        logBox = CreateLogBox();
        logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logBox.Location = new Point(13, 12);
        logBox.Size = new Size(logInputPanel.Width - 44, logInputPanel.Height - 24);
        logBox.WheelRequested += LogBox_WheelRequested;
        logBox.TextChanged += LogBox_ViewportChanged;
        logBox.Resize += LogBox_ViewportChanged;
        logBox.HandleCreated += LogBox_ViewportChanged;
        logBox.KeyUp += LogBox_KeyUp;
        logBox.MouseUp += LogBox_MouseUp;
        logInputPanel.Controls.Add(logBox);

        logScrollBar = new LinearScrollBar();
        logScrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        logScrollBar.Location = new Point(logInputPanel.Width - 22, 12);
        logScrollBar.Size = new Size(10, logInputPanel.Height - 24);
        logScrollBar.Visible = false;
        logScrollBar.ValueChanged += LogScrollBar_ValueChanged;
        logInputPanel.Controls.Add(logScrollBar);
        UpdateLogScrollBar();

        logPanel.Controls.Add(logTitle);
        logPanel.Controls.Add(logInputPanel);

        Controls.Add(toolPanel);
        Controls.Add(logPanel);
    }

    private static Label CreateLabel(string text, int x, int y, int width, int height, Font font, Color color)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = new Point(x, y);
        label.Size = new Size(width, height);
        label.Font = font;
        label.ForeColor = color;
        label.BackColor = Color.Transparent;
        label.AutoEllipsis = true;
        label.UseMnemonic = false;
        return label;
    }

    private static LinearSurfacePanel CreateInputPanel(int x, int y, int width, int height)
    {
        LinearSurfacePanel panel = new LinearSurfacePanel();
        panel.Location = new Point(x, y);
        panel.Size = new Size(width, height);
        panel.FillColor = UiTheme.InputBack;
        panel.BorderColor = UiTheme.BorderStandard;
        panel.Radius = 6;
        return panel;
    }

    private static TextBox CreateInputBox(string text)
    {
        TextBox box = new TextBox();
        box.BorderStyle = BorderStyle.None;
        box.BackColor = UiTheme.InputBack;
        box.ForeColor = UiTheme.SecondaryText;
        box.Font = UiTheme.Font(9F, FontStyle.Regular);
        box.Text = text;
        return box;
    }

    private static NaturalLogTextBox CreateLogBox()
    {
        NaturalLogTextBox box = new NaturalLogTextBox();
        box.BorderStyle = BorderStyle.None;
        box.BackColor = UiTheme.InputBack;
        box.ForeColor = UiTheme.SecondaryText;
        box.Font = UiTheme.MonoFont(8.5F, FontStyle.Regular);
        box.Multiline = true;
        box.ReadOnly = true;
        box.ScrollBars = ScrollBars.None;
        box.ShortcutsEnabled = true;
        return box;
    }

    private Icon LoadApplicationIcon()
    {
        string iconPath = Path.Combine(appDir, "Assets", "TlsPdfDownloader.ico");
        try
        {
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
        }

        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private void ApplyNativeTitleBarTheme()
    {
        if (!IsHandleCreated)
        {
            return;
        }

        int enabled = 1;
        TrySetDwmAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE, enabled);
        TrySetDwmAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, enabled);

        TrySetDwmAttribute(DWMWA_CAPTION_COLOR, ColorTranslator.ToWin32(UiTheme.AppBack));
        TrySetDwmAttribute(DWMWA_TEXT_COLOR, ColorTranslator.ToWin32(UiTheme.PrimaryText));
        TrySetDwmAttribute(DWMWA_BORDER_COLOR, ColorTranslator.ToWin32(UiTheme.AppBack));
    }

    private void TrySetDwmAttribute(int attribute, int value)
    {
        try
        {
            DwmSetWindowAttribute(Handle, attribute, ref value, sizeof(int));
        }
        catch
        {
        }
    }

    private void LogBox_WheelRequested(object sender, MouseEventArgs e)
    {
        int lines = SystemInformation.MouseWheelScrollLines;
        if (lines <= 0 || lines > 20)
        {
            lines = 3;
        }

        int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;
        if (notches == 0)
        {
            notches = e.Delta > 0 ? 1 : -1;
        }

        ScrollLogToLine(GetFirstVisibleLogLine() - notches * lines);
    }

    private void LogScrollBar_ValueChanged(object sender, EventArgs e)
    {
        if (updatingLogScrollBar)
        {
            return;
        }

        ScrollLogToLine(logScrollBar.Value);
    }

    private void LogBox_ViewportChanged(object sender, EventArgs e)
    {
        QueueLogScrollBarUpdate();
    }

    private void LogBox_KeyUp(object sender, KeyEventArgs e)
    {
        QueueLogScrollBarUpdate();
    }

    private void LogBox_MouseUp(object sender, MouseEventArgs e)
    {
        QueueLogScrollBarUpdate();
    }

    private void QueueLogScrollBarUpdate()
    {
        if (IsDisposed)
        {
            return;
        }

        if (!IsHandleCreated)
        {
            UpdateLogScrollBar();
            return;
        }

        try
        {
            BeginInvoke(new MethodInvoker(UpdateLogScrollBar));
        }
        catch
        {
            UpdateLogScrollBar();
        }
    }

    private void ScrollLogToLine(int targetLine)
    {
        if (logBox == null || logBox.IsDisposed || !logBox.IsHandleCreated)
        {
            return;
        }

        int maximum = Math.Max(0, GetLogLineCount() - GetVisibleLogLineCount());
        targetLine = Math.Max(0, Math.Min(maximum, targetLine));

        int currentLine = GetFirstVisibleLogLine();
        int delta = targetLine - currentLine;
        if (delta != 0)
        {
            SendMessage(logBox.Handle, EM_LINESCROLL, IntPtr.Zero, new IntPtr(delta));
        }

        UpdateLogScrollBar();
    }

    private void UpdateLogScrollBar()
    {
        if (logBox == null || logScrollBar == null || logBox.IsDisposed || !logBox.IsHandleCreated)
        {
            return;
        }

        int visibleLines = GetVisibleLogLineCount();
        int maximum = Math.Max(0, GetLogLineCount() - visibleLines);
        int value = Math.Max(0, Math.Min(maximum, GetFirstVisibleLogLine()));

        updatingLogScrollBar = true;
        try
        {
            logScrollBar.SetScrollValues(maximum, visibleLines, value);
        }
        finally
        {
            updatingLogScrollBar = false;
        }
    }

    private int GetLogLineCount()
    {
        if (logBox == null || logBox.IsDisposed || !logBox.IsHandleCreated)
        {
            return 1;
        }

        return Math.Max(1, SendMessage(logBox.Handle, EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero).ToInt32());
    }

    private int GetFirstVisibleLogLine()
    {
        if (logBox == null || logBox.IsDisposed || !logBox.IsHandleCreated)
        {
            return 0;
        }

        return Math.Max(0, SendMessage(logBox.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32());
    }

    private int GetVisibleLogLineCount()
    {
        if (logBox == null)
        {
            return 1;
        }

        int lineHeight = Math.Max(1, (int)Math.Ceiling(logBox.Font.GetHeight()));
        return Math.Max(1, logBox.ClientSize.Height / lineHeight);
    }

    private void BrowseButton_Click(object sender, EventArgs e)
    {
        string selectedPath = null;
        string initialPath = Directory.Exists(outputBox.Text) ? outputBox.Text : defaultOutput;

        try
        {
            selectedPath = ModernFolderPicker.Show(this.Handle, "PDF 저장 폴더 선택", initialPath);
        }
        catch
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "PDF 저장 폴더 선택";
                dialog.SelectedPath = initialPath;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    selectedPath = dialog.SelectedPath;
                }
            }
        }

        if (!string.IsNullOrEmpty(selectedPath))
        {
            outputBox.Text = selectedPath;
            SaveLastOutputDir(selectedPath);
        }
    }

    private void RunButton_Click(object sender, EventArgs e)
    {
        string url = urlBox.Text.Trim();
        string outputDir = outputBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show(this, "ubdoc URL을 입력하세요.", "확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            MessageBox.Show(this, "PDF 저장 폴더를 입력하세요.", "확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        SaveLastOutputDir(outputDir);

        stdoutBuffer.Length = 0;
        stderrBuffer.Length = 0;
        logBox.Clear();
        UpdateLogScrollBar();
        AppendLog("변환 중입니다. 완료될 때까지 기다리세요.");
        SetInputsEnabled(false);

        RunConfig config = new RunConfig();
        config.ViewerUrl = url;
        config.OutputDir = outputDir;
        config.KeepImages = keepImagesBox.Checked;

        BackgroundWorker worker = new BackgroundWorker();
        worker.WorkerReportsProgress = true;
        worker.DoWork += Worker_DoWork;
        worker.ProgressChanged += Worker_ProgressChanged;
        worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        worker.RunWorkerAsync(config);
    }

    private void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        BackgroundWorker worker = (BackgroundWorker)sender;
        RunConfig config = (RunConfig)e.Argument;
        e.Result = ConvertDocument(config, worker);
    }

    private ConversionResult ConvertDocument(RunConfig config, BackgroundWorker worker)
    {
        Dictionary<string, object> state = CheckState(config.ViewerUrl);
        string originalName = GetString(state, "file_realname");
        if (string.IsNullOrEmpty(originalName))
        {
            originalName = GetString(state, "file_name");
        }
        if (string.IsNullOrEmpty(originalName))
        {
            originalName = "ubdoc.pdf";
        }

        Directory.CreateDirectory(config.OutputDir);
        string outputPdf = AvailableOutputPath(Path.Combine(config.OutputDir, SafeFileName(originalName, "ubdoc.pdf")));
        if (!outputPdf.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            outputPdf = outputPdf + ".pdf";
        }

        string imageDir = Path.Combine(appDir, Path.GetFileNameWithoutExtension(outputPdf) + "_images");
        PageSet pageSet = LoadPages(state);
        Report(worker, "found " + pageSet.Pages.Count.ToString(CultureInfo.InvariantCulture) + " pages");

        List<string> images = DownloadImages(pageSet, imageDir, worker);
        CreatePdfFromImages(images, outputPdf, worker);
        Report(worker, "saved PDF: " + Path.GetFullPath(outputPdf));

        if (!config.KeepImages)
        {
            bool removed = CleanupImages(images, imageDir);
            if (removed)
            {
                Report(worker, "deleted images and folder: " + Path.GetFullPath(imageDir));
            }
            else
            {
                Report(worker, "deleted images; folder kept because it is not empty: " + Path.GetFullPath(imageDir));
            }
        }

        ConversionResult result = new ConversionResult();
        result.OutputPdf = outputPdf;
        return result;
    }

    private Dictionary<string, object> CheckState(string viewerUrl)
    {
        Uri uri = new Uri(viewerUrl);
        Dictionary<string, string> query = ParseQuery(uri.Query);
        string postData =
            "job=checkState" +
            "&id=" + Escape(query.ContainsKey("id") ? query["id"] : "") +
            "&tp=" + Escape(query.ContainsKey("tp") ? query["tp"] : "") +
            "&pg=" + Escape(query.ContainsKey("pg") ? query["pg"] : "") +
            "&item=" + Escape(query.ContainsKey("item") ? query["item"] : "") +
            "&fid=" + Escape(query.ContainsKey("fid") ? query["fid"] : "");

        Uri workerUri = new Uri(uri, "/local/ubdoc/worker.php");
        string json = Encoding.UTF8.GetString(RequestBytes(workerUri.ToString(), "POST", postData, viewerUrl));
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        Dictionary<string, object> result = serializer.DeserializeObject(json) as Dictionary<string, object>;
        if (result == null)
        {
            throw new InvalidOperationException("worker.php 응답을 해석할 수 없습니다.");
        }

        string stateCode = GetString(result, "state_code");
        if (stateCode != "100")
        {
            throw new InvalidOperationException("문서가 아직 준비되지 않았습니다: " + json);
        }
        return result;
    }

    private PageSet LoadPages(Dictionary<string, object> state)
    {
        string fileId = GetString(state, "file_id");
        string organization = GetString(state, "organization_code");
        string owner = GetString(state, "owner_id");
        if (string.IsNullOrEmpty(owner))
        {
            owner = GetString(state, "fileuser");
        }

        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(owner))
        {
            throw new InvalidOperationException("문서 이미지 경로 정보를 찾을 수 없습니다.");
        }

        string baseUrl = "https://doc.coursemos.co.kr/" + organization + "/" + owner + "/" + fileId + "/";
        string xmlUrl = baseUrl + fileId + ".xml";
        string xmlText = Encoding.UTF8.GetString(RequestBytes(xmlUrl, "GET", null, null));
        XDocument document = XDocument.Parse(xmlText);

        List<PageInfo> pages = new List<PageInfo>();
        foreach (XElement page in document.Descendants("pdf"))
        {
            string path = ElementText(page, "path_html");
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            PageInfo info = new PageInfo();
            info.Id = ParseInt(ElementText(page, "id"), pages.Count + 1);
            info.PathHtml = path;
            info.Width = ParseInt(ElementText(page, "w"), 0);
            info.Height = ParseInt(ElementText(page, "h"), 0);
            pages.Add(info);
        }

        if (pages.Count == 0)
        {
            throw new InvalidOperationException("XML 메타데이터에서 페이지 이미지를 찾을 수 없습니다.");
        }

        PageSet pageSet = new PageSet();
        pageSet.BaseUrl = baseUrl;
        pageSet.Pages = pages;
        return pageSet;
    }

    private List<string> DownloadImages(PageSet pageSet, string imageDir, BackgroundWorker worker)
    {
        Directory.CreateDirectory(imageDir);
        List<string> imagePaths = new List<string>();
        int digits = pageSet.Pages.Count.ToString(CultureInfo.InvariantCulture).Length;

        for (int i = 0; i < pageSet.Pages.Count; i++)
        {
            PageInfo page = pageSet.Pages[i];
            Uri imageUri = new Uri(new Uri(pageSet.BaseUrl), page.PathHtml);
            string fileName = (i + 1).ToString(new string('0', digits), CultureInfo.InvariantCulture) + ".png";
            string outputPath = Path.Combine(imageDir, fileName);

            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                byte[] imageBytes = RequestBytes(imageUri.ToString(), "GET", null, pageSet.BaseUrl);
                File.WriteAllBytes(outputPath, imageBytes);
            }

            imagePaths.Add(outputPath);
            Report(worker, "downloaded " + (i + 1).ToString(CultureInfo.InvariantCulture) + "/" +
                pageSet.Pages.Count.ToString(CultureInfo.InvariantCulture) + ": " + fileName);
        }

        return imagePaths;
    }

    private void CreatePdfFromImages(List<string> imagePaths, string outputPdf, BackgroundWorker worker)
    {
        List<PdfImage> pdfImages = new List<PdfImage>();
        try
        {
            for (int i = 0; i < imagePaths.Count; i++)
            {
                Report(worker, "building PDF page " + (i + 1).ToString(CultureInfo.InvariantCulture) + "/" +
                    imagePaths.Count.ToString(CultureInfo.InvariantCulture));
                pdfImages.Add(LoadPdfImage(imagePaths[i]));
            }

            SimplePdfWriter.Write(outputPdf, pdfImages);
        }
        finally
        {
            foreach (PdfImage image in pdfImages)
            {
                image.Dispose();
            }
        }
    }

    private PdfImage LoadPdfImage(string path)
    {
        using (Image original = Image.FromFile(path))
        {
            using (Bitmap bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(original, 0, 0, original.Width, original.Height);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    ImageCodecInfo codec = GetJpegCodec();
                    EncoderParameters parameters = new EncoderParameters(1);
                    parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                    bitmap.Save(stream, codec, parameters);

                    PdfImage image = new PdfImage();
                    image.Width = original.Width;
                    image.Height = original.Height;
                    image.JpegBytes = stream.ToArray();
                    return image;
                }
            }
        }
    }

    private static ImageCodecInfo GetJpegCodec()
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == ImageFormat.Jpeg.Guid)
            {
                return codec;
            }
        }
        throw new InvalidOperationException("JPEG encoder를 찾을 수 없습니다.");
    }

    private bool CleanupImages(List<string> imagePaths, string imageDir)
    {
        foreach (string imagePath in imagePaths)
        {
            try
            {
                File.Delete(imagePath);
            }
            catch
            {
            }
        }

        try
        {
            Directory.Delete(imageDir, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] RequestBytes(string url, string method, string body, string referer)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = method;
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";
        request.Timeout = 30000;
        request.ReadWriteTimeout = 30000;
        if (!string.IsNullOrEmpty(referer))
        {
            request.Referer = referer;
        }

        if (method == "POST")
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body ?? "");
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bodyBytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }
        }

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (MemoryStream memory = new MemoryStream())
        {
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (query.StartsWith("?"))
        {
            query = query.Substring(1);
        }

        string[] parts = query.Split('&');
        foreach (string part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }

            string[] pair = part.Split(new char[] { '=' }, 2);
            string key = Uri.UnescapeDataString(pair[0].Replace("+", " "));
            string value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace("+", " ")) : "";
            result[key] = value;
        }
        return result;
    }

    private static string Escape(string value)
    {
        return Uri.EscapeDataString(value ?? "");
    }

    private static string GetString(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key) || data[key] == null)
        {
            return "";
        }
        return Convert.ToString(data[key], CultureInfo.InvariantCulture);
    }

    private static string ElementText(XElement parent, string name)
    {
        XElement element = parent.Element(name);
        return element == null ? "" : element.Value;
    }

    private static int ParseInt(string text, int fallback)
    {
        int value;
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
    }

    private static string SafeFileName(string name, string fallback)
    {
        string cleaned = Regex.Replace(name ?? "", "[<>:\"/\\\\|?*\\x00-\\x1f]", "_").Trim().TrimEnd('.');
        return string.IsNullOrEmpty(cleaned) ? fallback : cleaned;
    }

    private static string AvailableOutputPath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        string directory = Path.GetDirectoryName(path);
        string stem = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        for (int i = 1; ; i++)
        {
            string candidate = Path.Combine(directory, stem + "_" + i.ToString(CultureInfo.InvariantCulture) + extension);
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        AppendLog((string)e.UserState);
    }

    private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        SetInputsEnabled(true);

        string stdout;
        string stderr;
        lock (bufferLock)
        {
            stdout = stdoutBuffer.ToString();
            stderr = stderrBuffer.ToString();
        }
        WriteTextLog(lastRunLog, stdout);
        WriteTextLog(guiErrorLog, stderr);

        if (e.Error != null)
        {
            AppendLog(e.Error.ToString());
            WriteTextLog(guiErrorLog, e.Error.ToString());
            MessageBox.Show(this, e.Error.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SaveLastOutputDir(outputBox.Text.Trim());
        AppendLog("Done.");
        MessageBox.Show(this, "PDF 저장이 완료되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Report(BackgroundWorker worker, string message)
    {
        lock (bufferLock)
        {
            stdoutBuffer.AppendLine(message);
        }
        if (worker != null)
        {
            worker.ReportProgress(0, message);
        }
    }

    private void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
        logBox.AppendText(message + Environment.NewLine);
        logBox.SelectionStart = logBox.TextLength;
        logBox.ScrollToCaret();
        ScrollLogToLine(GetLogLineCount());
    }

    private void SetInputsEnabled(bool enabled)
    {
        runButton.Enabled = enabled;
        browseButton.Enabled = enabled;
        urlBox.ReadOnly = !enabled;
        outputBox.ReadOnly = !enabled;
        urlBox.BackColor = enabled ? UiTheme.InputBack : UiTheme.DisabledBack;
        outputBox.BackColor = enabled ? UiTheme.InputBack : UiTheme.DisabledBack;
        if (urlInputPanel != null)
        {
            urlInputPanel.FillColor = enabled ? UiTheme.InputBack : UiTheme.DisabledBack;
        }
        if (outputInputPanel != null)
        {
            outputInputPanel.FillColor = enabled ? UiTheme.InputBack : UiTheme.DisabledBack;
        }
        keepImagesBox.Enabled = enabled;
    }

    private static void WriteTextLog(string path, string text)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, text ?? "", new UTF8Encoding(true));
    }

    private string LoadLastOutputDir()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                string value = File.ReadAllText(settingsPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }
        catch
        {
        }
        return defaultOutput;
    }

    private void SaveLastOutputDir(string outputDir)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                File.WriteAllText(settingsPath, outputDir.Trim(), new UTF8Encoding(true));
            }
        }
        catch
        {
        }
    }

    private sealed class RunConfig
    {
        public string ViewerUrl;
        public string OutputDir;
        public bool KeepImages;
    }

    public sealed class ConversionResult
    {
        public string OutputPdf;
    }

    private sealed class PageSet
    {
        public string BaseUrl;
        public List<PageInfo> Pages;
    }

    private sealed class PageInfo
    {
        public int Id;
        public string PathHtml;
        public int Width;
        public int Height;
    }

    private sealed class PdfImage : IDisposable
    {
        public int Width;
        public int Height;
        public byte[] JpegBytes;

        public void Dispose()
        {
            JpegBytes = null;
        }
    }

    private static class SimplePdfWriter
    {
        public static void Write(string outputPath, List<PdfImage> images)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                int objectCount = 2 + images.Count * 3;
                long[] offsets = new long[objectCount + 1];
                WriteAscii(stream, "%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n");

                offsets[1] = stream.Position;
                WriteObject(stream, 1, "<< /Type /Catalog /Pages 2 0 R >>");

                StringBuilder kids = new StringBuilder();
                for (int i = 0; i < images.Count; i++)
                {
                    int pageObj = 3 + i * 3;
                    kids.Append(pageObj.ToString(CultureInfo.InvariantCulture)).Append(" 0 R ");
                }

                offsets[2] = stream.Position;
                WriteObject(stream, 2, "<< /Type /Pages /Kids [" + kids.ToString() + "] /Count " +
                    images.Count.ToString(CultureInfo.InvariantCulture) + " >>");

                for (int i = 0; i < images.Count; i++)
                {
                    PdfImage image = images[i];
                    int pageObj = 3 + i * 3;
                    int imageObj = pageObj + 1;
                    int contentObj = pageObj + 2;
                    string imageName = "Im" + (i + 1).ToString(CultureInfo.InvariantCulture);

                    offsets[pageObj] = stream.Position;
                    string page = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " +
                        image.Width.ToString(CultureInfo.InvariantCulture) + " " +
                        image.Height.ToString(CultureInfo.InvariantCulture) +
                        "] /Resources << /XObject << /" + imageName + " " +
                        imageObj.ToString(CultureInfo.InvariantCulture) +
                        " 0 R >> >> /Contents " + contentObj.ToString(CultureInfo.InvariantCulture) + " 0 R >>";
                    WriteObject(stream, pageObj, page);

                    offsets[imageObj] = stream.Position;
                    WriteAscii(stream, imageObj.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    WriteAscii(stream, "<< /Type /XObject /Subtype /Image /Width " +
                        image.Width.ToString(CultureInfo.InvariantCulture) + " /Height " +
                        image.Height.ToString(CultureInfo.InvariantCulture) +
                        " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " +
                        image.JpegBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
                    stream.Write(image.JpegBytes, 0, image.JpegBytes.Length);
                    WriteAscii(stream, "\nendstream\nendobj\n");

                    string content = "q\n" +
                        image.Width.ToString(CultureInfo.InvariantCulture) + " 0 0 " +
                        image.Height.ToString(CultureInfo.InvariantCulture) + " 0 0 cm\n/" +
                        imageName + " Do\nQ\n";
                    byte[] contentBytes = Encoding.ASCII.GetBytes(content);

                    offsets[contentObj] = stream.Position;
                    WriteAscii(stream, contentObj.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    WriteAscii(stream, "<< /Length " + contentBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
                    stream.Write(contentBytes, 0, contentBytes.Length);
                    WriteAscii(stream, "endstream\nendobj\n");
                }

                long xref = stream.Position;
                WriteAscii(stream, "xref\n0 " + (objectCount + 1).ToString(CultureInfo.InvariantCulture) + "\n");
                WriteAscii(stream, "0000000000 65535 f \n");
                for (int i = 1; i <= objectCount; i++)
                {
                    WriteAscii(stream, offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
                }
                WriteAscii(stream, "trailer\n<< /Size " + (objectCount + 1).ToString(CultureInfo.InvariantCulture) +
                    " /Root 1 0 R >>\nstartxref\n" + xref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
            }
        }

        private static void WriteObject(FileStream stream, int number, string body)
        {
            WriteAscii(stream, number.ToString(CultureInfo.InvariantCulture) + " 0 obj\n" + body + "\nendobj\n");
        }

        private static void WriteAscii(FileStream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    private static class ModernFolderPicker
    {
        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint FOS_PATHMUSTEXIST = 0x00000800;
        private const uint FOS_NOCHANGEDIR = 0x00000008;
        private const int ERROR_CANCELLED = unchecked((int)0x800704C7);

        public static string Show(IntPtr owner, string title, string initialPath)
        {
            IFileOpenDialog dialog = (IFileOpenDialog)new FileOpenDialogRCW();
            try
            {
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_NOCHANGEDIR);
                dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    IShellItem folder;
                    Guid shellItemGuid = typeof(IShellItem).GUID;
                    SHCreateItemFromParsingName(initialPath, IntPtr.Zero, ref shellItemGuid, out folder);
                    dialog.SetFolder(folder);
                }

                int result = dialog.Show(owner);
                if (result == ERROR_CANCELLED)
                {
                    return null;
                }
                if (result != 0)
                {
                    Marshal.ThrowExceptionForHR(result);
                }

                IShellItem item;
                dialog.GetResult(out item);
                IntPtr pathPtr;
                item.GetDisplayName(SIGDN_FILESYSPATH, out pathPtr);
                try
                {
                    return Marshal.PtrToStringUni(pathPtr);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pathPtr);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(dialog);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        private const uint SIGDN_FILESYSPATH = 0x80058000;

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW
        {
        }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);
            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(uint fos);
            void GetOptions(out uint pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid(ref Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
        }

        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog : IFileDialog
        {
            void GetResults(IntPtr ppenum);
            void GetSelectedItems(IntPtr ppsai);
        }

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }
    }
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "--self-test")
            {
                using (UBDocGuiForm form = new UBDocGuiForm())
                {
                }
                return 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UBDocGuiForm());
            return 0;
        }
        catch (Exception ex)
        {
            WriteStartupError(ex);
            try
            {
                MessageBox.Show("프로그램을 시작할 수 없습니다.\n\n" + ex.Message, "TLS PDF Downloader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
            return 1;
        }
    }

    private static void WriteStartupError(Exception ex)
    {
        try
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string logDir = Path.Combine(appDir, "Logs");
            Directory.CreateDirectory(logDir);
            File.WriteAllText(Path.Combine(logDir, "startup_error.log"), ex.ToString(), new UTF8Encoding(true));
        }
        catch
        {
        }
    }
}
