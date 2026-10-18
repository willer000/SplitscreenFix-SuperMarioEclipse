using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

class Pad {
    public static bool English;
    public string Id, Name;
    public IntPtr Handle;
    public bool Available;
    public int SdlIndex = -1;
    public override string ToString() { return Name + "  [" + Id + "]" + (Available ? "" : (English ? "  · no input" : "  · sem entrada")); }
}

static class Theme {
    public static readonly Color Background = Color.FromArgb(11, 15, 21);
    public static readonly Color Card = Color.FromArgb(22, 29, 39);
    public static readonly Color Border = Color.FromArgb(42, 53, 67);
    public static readonly Color Text = Color.FromArgb(238, 246, 247);
    public static readonly Color Muted = Color.FromArgb(151, 166, 181);
    public static readonly Color Green = Color.FromArgb(91, 219, 153);
    public static readonly Color Red = Color.FromArgb(239, 109, 105);
    public const int BackdropOpacity = 226;
    public const int CardOpacity = 231;
    public static Image Banner;
    public static void PaintBackdrop(Graphics g, Rectangle bounds, Point offset, Size total, Color ink, int opacity) {
        if(Banner == null || total.Width < 1 || total.Height < 1) { g.Clear(ink); return; }
        var saved = g.Save();
        g.SetClip(bounds);
        double scale = Math.Max((double)total.Width / Banner.Width, (double)total.Height / Banner.Height);
        int width = (int)Math.Ceiling(Banner.Width * scale), height = (int)Math.Ceiling(Banner.Height * scale);
        int x = (total.Width - width) / 2 - offset.X, y = (total.Height - height) / 2 - offset.Y;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(Banner, new Rectangle(x, y, width, height));
        using(var shade = new SolidBrush(Color.FromArgb(opacity, ink))) g.FillRectangle(shade, bounds);
        g.Restore(saved);
    }
}

class BackdropPanel : Panel {
    public BackdropPanel() { DoubleBuffered = true; }
    protected override void OnPaintBackground(PaintEventArgs e) {
        Theme.PaintBackdrop(e.Graphics, ClientRectangle, Point.Empty, ClientSize, Theme.Background, Theme.BackdropOpacity);
    }
}

class RoundedPanel : Panel {
    public Color Accent = Color.Transparent;
    public RoundedPanel() {
        DoubleBuffered = true;
        BackColor = Theme.Card;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }
    protected override void OnPaintBackground(PaintEventArgs e) {
        if(Parent is BackdropPanel)
            Theme.PaintBackdrop(e.Graphics, ClientRectangle, Location, Parent.ClientSize, Theme.Card, Theme.CardOpacity);
        else e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);
    }
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.SmoothingMode = SmoothingMode.None;
        using (var path = RoundRect(new Rectangle(1, 1, Width - 3, Height - 3), 0)) {
            if(Theme.Banner == null) using (var fill = new SolidBrush(BackColor)) e.Graphics.FillPath(fill, path);
            using (var edge = new Pen(Theme.Border)) e.Graphics.DrawPath(edge, path);
        }
        if (Accent != Color.Transparent) {
            using (var brush = new SolidBrush(Accent))
                e.Graphics.FillRectangle(brush, 1, 2, 2, Math.Max(0, Height - 4));
        }
    }
    public static GraphicsPath RoundRect(Rectangle r, int radius) {
        var p = new GraphicsPath();
        if(radius <= 0) { p.AddRectangle(r); return p; }
        int d = radius * 2;
        p.AddArc(r.Left, r.Top, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

class StatusPanel : RoundedPanel {
    public StatusPanel() { SetStyle(ControlStyles.Selectable, true); TabStop = true; Cursor = Cursors.Hand; }
    protected override void OnKeyDown(KeyEventArgs e) {
        if(e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; }
        base.OnKeyDown(e);
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        if(Focused) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(3, 3, Width-7, Height-7));
    }
}

class HeroPanel : Panel {
    public HeroPanel() { DoubleBuffered = true; }
    protected override void OnPaintBackground(PaintEventArgs e) {
        using (var fill = new SolidBrush(Theme.Card)) e.Graphics.FillRectangle(fill, ClientRectangle);
        using (var line = new Pen(Theme.Border)) e.Graphics.DrawLine(line, 0, Height - 1, Width, Height - 1);
    }
}

class ModernButton : Button {
    public bool Primary;
    public bool GreenOutline;
    bool hover, down;
    public ModernButton() {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e) {
        if(Parent is BackdropPanel) Theme.PaintBackdrop(e.Graphics,ClientRectangle,Location,Parent.ClientSize,Theme.Background,Theme.BackdropOpacity);
        else e.Graphics.Clear(Parent == null ? Theme.Background : Parent.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        Color bg = Primary ? (down ? Color.FromArgb(55, 174, 118) : hover ? Color.FromArgb(122, 235, 174) : Theme.Green)
                           : hover ? Color.FromArgb(43, 55, 67) : Color.FromArgb(31, 40, 52);
        Color edge = GreenOutline ? Theme.Green : Primary ? bg : Theme.Border;
        using (var path = RoundedPanel.RoundRect(new Rectangle(1, 1, Width - 3, Height - 3), 0)) {
            using (var brush = new SolidBrush(bg)) e.Graphics.FillPath(brush, path);
            using (var pen = new Pen(edge)) e.Graphics.DrawPath(pen, path);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(8, 1, Width - 16, Height - 2),
            Primary ? Color.FromArgb(9, 28, 22) : Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(5, 5, Width - 10, Height - 10), Theme.Text, bg);
    }
}

class DarkComboBox : ComboBox {
    public DarkComboBox() {
        DropDownStyle = ComboBoxStyle.DropDownList;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 23;
        FlatStyle = FlatStyle.Flat;
        BackColor = Color.FromArgb(12, 19, 28);
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9f);
    }
    protected override void OnDrawItem(DrawItemEventArgs e) {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using (var brush = new SolidBrush(selected ? Color.FromArgb(35, 82, 67) : BackColor))
            e.Graphics.FillRectangle(brush, e.Bounds);
        TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font,
            new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height), Theme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}

class SwitchCheckBox : CheckBox {
    public SwitchCheckBox() {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Font = new Font("Segoe UI", 9f);
        ForeColor = Theme.Text;
        Cursor = Cursors.Hand;
    }
    protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) {
        if(Parent is RoundedPanel && Parent.Parent is BackdropPanel)
            Theme.PaintBackdrop(e.Graphics,ClientRectangle,new Point(Parent.Left+Left,Parent.Top+Top),Parent.Parent.ClientSize,Theme.Card,Theme.CardOpacity);
        else e.Graphics.Clear(Parent == null ? Theme.Card : Parent.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        int x = Width - 19, y = (Height - 16) / 2;
        using (var fill = new SolidBrush(Checked ? Theme.Green : Color.FromArgb(12, 19, 28)))
            e.Graphics.FillRectangle(fill, x, y, 16, 16);
        using (var edge = new Pen(Checked ? Theme.Green : Theme.Muted))
            e.Graphics.DrawRectangle(edge, x, y, 15, 15);
        if(Checked) using(var check = new Pen(Color.FromArgb(9, 28, 22), 2f)) {
            e.Graphics.DrawLine(check, x+3, y+8, x+6, y+11);
            e.Graphics.DrawLine(check, x+6, y+11, x+13, y+4);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(0, 0, Math.Max(0, x - 5), Height),
            Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

class Avatar : Control {
    public string Letter;
    public Color Accent;
    public Avatar(string letter, Color color) { Letter = letter; Accent = color; DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e) {
        if(Parent is RoundedPanel && Parent.Parent is BackdropPanel)
            Theme.PaintBackdrop(e.Graphics,ClientRectangle,new Point(Parent.Left+Left,Parent.Top+Top),Parent.Parent.ClientSize,Theme.Card,Theme.CardOpacity);
        else e.Graphics.Clear(Parent == null ? Theme.Card : Parent.BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        using (var brush = new SolidBrush(Color.FromArgb(35, Accent))) e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
        using (var pen = new Pen(Accent)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        using (var font = new Font("Consolas", 13f, FontStyle.Bold))
            TextRenderer.DrawText(e.Graphics, Letter, font, ClientRectangle, Accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

class LanguageButton : Control {
    public bool Brazil, Selected;
    public LanguageButton(bool brazil) { Brazil=brazil; DoubleBuffered=true; Cursor=Cursors.Hand; TabStop=true; AccessibleName=brazil?"Português do Brasil":"English"; }
    protected override void OnKeyDown(KeyEventArgs e) { if(e.KeyCode==Keys.Enter || e.KeyCode==Keys.Space) { OnClick(EventArgs.Empty); e.Handled=true; } base.OnKeyDown(e); }
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.Clear(Parent==null?Theme.Background:Parent.BackColor);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        var r=new Rectangle(1,1,Width-3,Height-3);
        using(var p=RoundedPanel.RoundRect(r,0)) {
            using(var b=new SolidBrush(Selected?Color.FromArgb(35,66,54):Color.FromArgb(23,30,39))) e.Graphics.FillPath(b,p);
            using(var pen=new Pen(Selected?Theme.Green:Theme.Border)) e.Graphics.DrawPath(pen,p);
        }
        var f=new Rectangle(9,9,20,14);
        if(Brazil) {
            using(var b=new SolidBrush(Color.FromArgb(28,150,78))) e.Graphics.FillRectangle(b,f);
            using(var b=new SolidBrush(Color.FromArgb(247,207,55))) e.Graphics.FillPolygon(b,new[]{new Point(19,10),new Point(28,16),new Point(19,22),new Point(10,16)});
            using(var b=new SolidBrush(Color.FromArgb(37,70,153))) e.Graphics.FillEllipse(b,15,12,8,8);
        } else {
            using(var b=new SolidBrush(Color.White)) e.Graphics.FillRectangle(b,f);
            using(var b=new SolidBrush(Color.FromArgb(190,54,62))) for(int y=9;y<23;y+=4) e.Graphics.FillRectangle(b,9,y,20,2);
            using(var b=new SolidBrush(Color.FromArgb(48,66,128))) e.Graphics.FillRectangle(b,9,9,9,8);
        }
        TextRenderer.DrawText(e.Graphics,Brazil?"PT-BR":"EN",new Font("Segoe UI",8.5f,FontStyle.Bold),new Rectangle(33,1,Width-36,Height-2),Selected?Theme.Text:Theme.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
    }
}

class Launcher : Form {
    const int GridGutter = 8;
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int command);
    [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern bool SetWindowText(IntPtr hWnd, string title);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongW")] static extern int SetWindowLong(IntPtr hWnd, int index, int style);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("SDL2.dll")] static extern int SDL_Init(uint flags);
    [DllImport("SDL2.dll")] static extern void SDL_Quit();
    [DllImport("SDL2.dll")] static extern int SDL_NumJoysticks();
    [DllImport("SDL2.dll")] static extern IntPtr SDL_JoystickNameForIndex(int index);
    [DllImport("SDL2.dll")] static extern ushort SDL_JoystickGetDeviceVendor(int index);
    [DllImport("SDL2.dll")] static extern ushort SDL_JoystickGetDeviceProduct(int index);
    [DllImport("SDL2.dll")] static extern IntPtr SDL_GameControllerOpen(int index);
    [DllImport("SDL2.dll")] static extern void SDL_GameControllerClose(IntPtr pad);
    [DllImport("SDL2.dll")] static extern byte SDL_GameControllerGetButton(IntPtr pad, int button);
    [DllImport("SDL2.dll")] static extern void SDL_GameControllerUpdate();
    readonly ComboBox p1 = new DarkComboBox(), p2 = new DarkComboBox(), layout = new DarkComboBox();
    readonly CheckBox fill = new SwitchCheckBox(), borderlessStart = new SwitchCheckBox();
    readonly Label active = new Label(), summary = new Label();
    readonly Label marioRole = new Label(), luigiRole = new Label();
    readonly Label marioState = new Label(), luigiState = new Label();
    readonly Label marioPick = new Label(), luigiPick = new Label();
    readonly Label splitTitle = new Label(), splitHint = new Label(), imageTitle = new Label();
    readonly Label controlsHeading = new Label(), setupHeading = new Label(), actionsHeading = new Label(), footnote = new Label();
    readonly Label heroTitle = new Label(), heroSubtitle = new Label();
    readonly HeroPanel hero = new HeroPanel();
    readonly PictureBox sunshineMark = new PictureBox();
    readonly Panel scroll = new Panel();
    readonly BackdropPanel canvas = new BackdropPanel();
    readonly VScrollBar vertical = new VScrollBar();
    readonly StatusPanel statusCard = new StatusPanel();
    readonly RoundedPanel marioCard = new RoundedPanel(), luigiCard = new RoundedPanel();
    public Control SoloSaveCard { get { return marioCard; } }
    readonly RoundedPanel splitCard = new RoundedPanel(), imageCard = new RoundedPanel();
    readonly Avatar marioAvatar = new Avatar("M", Theme.Red), luigiAvatar = new Avatar("L", Theme.Green);
    readonly ModernButton refreshButton = new ModernButton(), launchButton = new ModernButton(), applyButton = new ModernButton();
    readonly ModernButton arrangeButton = new ModernButton(), frameButton = new ModernButton(), helpButton = new ModernButton(), folderButton = new ModernButton();
    readonly LanguageButton ptButton = new LanguageButton(true), enButton = new LanguageButton(false);
    readonly List<Pad> pads = new List<Pad>();
    readonly Timer timer = new Timer();
    readonly ToolTip diagnosticsTip = new ToolTip();
    bool sdlReady;
    bool layingOut;
    bool english;
    string languagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SplitscreenFix","ui-language.txt");
    string T(string pt,string en) { return english?en:pt; }

    public Launcher() {
        string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets");
        try { using(var source=Image.FromFile(Path.Combine(assets,"EclipseBanner.jpg"))) Theme.Banner=new Bitmap(source); } catch { Theme.Banner=null; }
        try { using(var source=Image.FromFile(Path.Combine(assets,"SunshineWordmark.png"))) sunshineMark.Image=new Bitmap(source); } catch { }
        try { Icon=new Icon(Path.Combine(assets,"SunshineLogo.ico")); } catch { }
        Text = "SplitscreenFix  |  Super Mario Eclipse";
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(410, 385);
        StartPosition = FormStartPosition.CenterScreen;
        try { english=File.Exists(languagePath) && File.ReadAllText(languagePath).Trim()=="en"; } catch { }

        hero.Dock = DockStyle.Top;
        hero.Height = 40;
        StyleLabel(heroTitle, "Super Mario Eclipse", 11.5f, FontStyle.Bold, Theme.Text);
        StyleLabel(heroSubtitle, "SPLITSCREEN FIX", 7.5f, FontStyle.Bold, Theme.Green);
        StyleLabel(summary, "", 8.5f, FontStyle.Regular, Theme.Green);
        summary.TextAlign = ContentAlignment.MiddleRight;
        sunshineMark.SizeMode=PictureBoxSizeMode.Zoom;
        sunshineMark.BackColor=Theme.Card;
        hero.Controls.Add(sunshineMark);
        hero.Controls.Add(heroTitle); hero.Controls.Add(heroSubtitle); hero.Controls.Add(summary);
        hero.Controls.Add(ptButton); hero.Controls.Add(enButton);
        ptButton.Click += (s,e) => SetLanguage(false);
        enButton.Click += (s,e) => SetLanguage(true);
        hero.Resize += (s,e) => LayoutHero();

        scroll.Dock = DockStyle.Fill;
        scroll.BackColor = Theme.Background;
        scroll.AutoScroll = false;
        canvas.BackColor = Theme.Background;
        vertical.Dock = DockStyle.Right;
        vertical.Visible = false;
        vertical.Scroll += (s,e) => { canvas.Top = -vertical.Value; };
        scroll.MouseWheel += ScrollWheel;
        canvas.MouseWheel += ScrollWheel;
        scroll.Controls.Add(canvas); scroll.Controls.Add(vertical);
        Controls.Add(scroll); Controls.Add(hero);
        scroll.Resize += (s,e) => LayoutDashboard();

        StyleLabel(controlsHeading, "", 8f, FontStyle.Bold, Theme.Muted);
        StyleLabel(setupHeading, "", 9f, FontStyle.Bold, Theme.Muted);
        StyleLabel(actionsHeading, "", 9f, FontStyle.Bold, Theme.Muted);
        foreach (var heading in new[] { controlsHeading, setupHeading, actionsHeading }) canvas.Controls.Add(heading);

        active.Text = "Ligue os controles e aperte um botão para identificá-los.";
        StyleLabel(active, active.Text, 8.5f, FontStyle.Regular, Theme.Text);
        active.AutoEllipsis = true;
        statusCard.Controls.Add(active); canvas.Controls.Add(statusCard);
        statusCard.AccessibleName = "Controller diagnostics";
        statusCard.Click += (s,e) => ShowDiagnostics();
        active.Cursor = Cursors.Hand;
        active.Click += (s,e) => ShowDiagnostics();
        diagnosticsTip.SetToolTip(statusCard,T("Clique para ver o diagnóstico completo.","Click for full diagnostics."));
        diagnosticsTip.SetToolTip(active,T("Clique para ver o diagnóstico completo.","Click for full diagnostics."));

        marioCard.Accent = Theme.Red;
        luigiCard.Accent = Theme.Green;
        SetupPlayerCard(marioCard, marioAvatar, marioRole, marioState, marioPick, p1,
                        "Mario", "P1 · HOST", Theme.Red);
        SetupPlayerCard(luigiCard, luigiAvatar, luigiRole, luigiState, luigiPick, p2,
                        "Luigi", "P2 · CLIENT", Theme.Green);
        canvas.Controls.Add(marioCard); canvas.Controls.Add(luigiCard);
        p1.SelectedIndexChanged += (s,e) => UpdateControllerCards();
        p2.SelectedIndexChanged += (s,e) => UpdateControllerCards();

        StyleLabel(splitTitle, "Divisão da tela", 9f, FontStyle.Bold, Theme.Text);
        StyleLabel(splitHint, "Preenche melhor a tela.", 7.5f, FontStyle.Regular, Theme.Muted);
        splitCard.Controls.Add(splitTitle); splitCard.Controls.Add(layout); splitCard.Controls.Add(splitHint);
        canvas.Controls.Add(splitCard);
        layout.Items.AddRange(new object[] { "Em cima / embaixo", "Lado a lado" }); layout.SelectedIndex = 0;

        StyleLabel(imageTitle, "Imagem e moldura", 9f, FontStyle.Bold, Theme.Text);
        imageCard.Controls.Add(imageTitle);
        fill.Text = "Esticar imagem"; fill.Checked = false; fill.BackColor = Theme.Card;
        borderlessStart.Text = "Iniciar sem moldura"; borderlessStart.Checked = true; borderlessStart.BackColor = Theme.Card;
        imageCard.Controls.Add(fill); imageCard.Controls.Add(borderlessStart);
        canvas.Controls.Add(imageCard);

        SetupButton(refreshButton, "", false, false, (s,e) => RefreshPads());
        SetupButton(launchButton, "", true, false, Launch);
        SetupButton(applyButton, "", false, true, (s,e) => ApplyLiveBindings());
        SetupButton(arrangeButton, "Organizar janelas", false, false, (s,e) => ArrangeExisting());
        SetupButton(frameButton, "Alternar borda", false, false, (s,e) => ToggleBorderless());
        SetupButton(helpButton, "Tutorial", false, false, (s,e) => ShowHelp());
        SetupButton(folderButton, "Abrir pasta", false, false,
                    (s,e) => Process.Start(AppDomain.CurrentDomain.BaseDirectory));
        foreach (var button in new[] { refreshButton, launchButton, applyButton, arrangeButton,
                                      frameButton, helpButton, folderButton }) canvas.Controls.Add(button);
        StyleLabel(footnote, "", 8.5f, FontStyle.Regular, Theme.Muted);
        footnote.AutoEllipsis = true; canvas.Controls.Add(footnote);

        FormClosed += (s,e) => { ClosePads(); diagnosticsTip.Dispose(); if(sunshineMark.Image!=null) sunshineMark.Image.Dispose(); if(Theme.Banner!=null) { Theme.Banner.Dispose(); Theme.Banner=null; } };
        ApplyLanguage();
        RefreshPads();
        timer.Interval = 100; timer.Tick += (s,e) => PollPads(); timer.Start();
        Shown += (s,e) => { EnableDarkTitleBar(); LayoutHero(); LayoutDashboard(); };
    }
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    void EnableDarkTitleBar() {
        try { int enabled = 1; DwmSetWindowAttribute(Handle, 20, ref enabled, 4); } catch { }
    }
    Label NewLabel(string text, float size, FontStyle style, Color color) {
        var l = new Label(); StyleLabel(l, text, size, style, color); return l;
    }
    void StyleLabel(Label l, string text, float size, FontStyle style, Color color) {
        l.Text = text; l.Font = new Font("Segoe UI", size, style); l.ForeColor = color;
        l.BackColor = Color.Transparent; l.TextAlign = ContentAlignment.MiddleLeft;
    }
    void SetupButton(ModernButton b, string text, bool primary, bool outline, EventHandler handler) {
        b.Text = text; b.Primary = primary; b.GreenOutline = outline; b.Click += handler;
    }
    void SetupPlayerCard(RoundedPanel card, Avatar avatar, Label role, Label state, Label pick,
                         ComboBox combo, string name, string subtitle, Color color) {
        card.Controls.Add(avatar);
        StyleLabel(role, name + "   /   " + subtitle, 9f, FontStyle.Bold, Theme.Text);
        StyleLabel(state, "", 7.5f, FontStyle.Regular, Theme.Muted);
        StyleLabel(pick, "", 7.5f, FontStyle.Regular, Theme.Muted);
        card.Controls.Add(role); card.Controls.Add(state); card.Controls.Add(pick); card.Controls.Add(combo);
    }
    void SetLanguage(bool en) {
        english=en;
        try { Directory.CreateDirectory(Path.GetDirectoryName(languagePath)); File.WriteAllText(languagePath,en?"en":"pt-BR"); } catch { }
        ApplyLanguage();
    }
    void ApplyLanguage() {
        Pad.English=english;
        foreach(var pad in pads) if(!pad.Available && pad.Name.StartsWith("Bluetooth")) pad.Name="Bluetooth";
        p1.Invalidate(); p2.Invalidate();
        ptButton.Selected=!english; enButton.Selected=english; ptButton.Invalidate(); enButton.Invalidate();
        controlsHeading.Text=T("CONTROLES","CONTROLLERS");
        setupHeading.Text=T("TELA E JANELAS","SCREEN / WINDOWS");
        actionsHeading.Text=T("AÇÕES","ACTIONS");
        marioRole.Text=T("Mario  ·  JOGADOR 1 / HOST","Mario  ·  PLAYER 1 / HOST");
        luigiRole.Text=T("Luigi  ·  JOGADOR 2 / CLIENT","Luigi  ·  PLAYER 2 / CLIENT");
        marioPick.Text=luigiPick.Text=T("Dispositivo","Device");
        splitTitle.Text=T("Divisão","Split layout");
        splitHint.Text=T("Preenche melhor a tela.","Better screen fit.");
        imageTitle.Text=T("Imagem e moldura","Image / frame");
        fill.Text=T("Esticar imagem","Stretch image");
        borderlessStart.Text=T("Abrir sem borda","Start borderless");
        string selected=layout.SelectedIndex==1?"side":"top";
        layout.Items.Clear(); layout.Items.AddRange(english?new object[]{"Top / bottom","Side by side"}:new object[]{"Em cima / embaixo","Lado a lado"});
        layout.SelectedIndex=selected=="side"?1:0;
        refreshButton.Text=T("Atualizar","Refresh");
        launchButton.Text=T("Iniciar Mario + Luigi","Start Mario + Luigi");
        applyButton.Text=T("Aplicar controles","Apply controllers");
        arrangeButton.Text=T("Atualizar layout","Update Layout");
        frameButton.Text=T("Alternar borda","Toggle Border");
        helpButton.Text=T("Ajuda","Help");
        folderButton.Text=T("Abrir pasta","Open folder");
        footnote.Text=T("Troca durante a partida: atualize a lista, escolha os controles e aplique.","During play: refresh, select controllers, then apply.");
        active.Text=ControllerSummary();
        diagnosticsTip.SetToolTip(statusCard,T("Clique para ver o diagnóstico completo.","Click for full diagnostics."));
        diagnosticsTip.SetToolTip(active,T("Clique para ver o diagnóstico completo.","Click for full diagnostics."));
        UpdateControllerCards();
    }
    string ControllerSummary() {
        int ready=pads.FindAll(x=>x.Available).Count;
        int missing=pads.Count-ready;
        return ready+T(ready==1?" pronto":" prontos",ready==1?" ready":" ready")+"  ·  "+missing+T(" sem entrada"," unavailable")+"  ›";
    }
    string DiagnosticReport() {
        var lines=new StringBuilder();
        lines.AppendLine("SplitscreenFix  |  "+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        lines.AppendLine("Windows: "+Environment.OSVersion+"  |  "+(Environment.Is64BitOperatingSystem?"64-bit":"32-bit"));
        lines.AppendLine("Launcher: "+Application.ExecutablePath);
        lines.AppendLine("SDL: "+(sdlReady?T("iniciado","initialized"):T("não iniciado","not initialized")));
        lines.AppendLine("SDL2.dll: "+(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SDL2.dll"))?T("encontrado","found"):T("ausente","missing")));
        lines.AppendLine("Config: "+(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SplitscreenFix.config.json"))?T("encontrada","found"):T("ausente","missing")));
        lines.AppendLine();
        lines.AppendLine(english?"CONTROLLERS":"CONTROLES");
        lines.AppendLine("Total: "+pads.Count+"  |  "+pads.FindAll(x=>x.Available).Count+T(" prontos"," ready")+"  |  "+pads.FindAll(x=>!x.Available).Count+T(" sem entrada"," unavailable"));
        if(pads.Count==0) lines.AppendLine(english?"No controller detected.":"Nenhum controle detectado.");
        foreach(var pad in pads) {
            lines.AppendLine("- "+pad.Name+"  ["+pad.Id+"]");
            lines.AppendLine("  "+(pad.SdlIndex>=0?"SDL index: "+pad.SdlIndex:T("Somente Bluetooth/WMI","Bluetooth/WMI only"))+"  |  "+(pad.Available?T("entrada pronta","game input ready"):T("SEM ENTRADA DE JOGO","NO GAME INPUT"))+"  |  handle: "+(pad.Handle!=IntPtr.Zero?T("aberto","open"):T("nenhum","none")));
        }
        lines.AppendLine();
        lines.AppendLine(english?"WINDOW ASSIGNMENTS":"ATRIBUIÇÕES DAS JANELAS");
        foreach(var role in new[]{"host","client"}) {
            var selected=(role=="host"?p1:p2).SelectedItem as Pad;
            lines.AppendLine((role=="host"?"Mario / host: ":"Luigi / client: ")+(selected==null?T("nenhum","none"):selected.Name+" ["+selected.Id+"] "+(selected.Available?T("pronto","ready"):T("SEM ENTRADA DE JOGO","NO GAME INPUT"))));
            string binding=BindingPath(role), ack=binding+".ack";
            lines.AppendLine("  binding: "+ReadDiagnosticFile(binding));
            lines.AppendLine("  ack: "+ReadDiagnosticFile(ack));
        }
        lines.AppendLine();
        lines.AppendLine(english?"GAME PROCESSES":"PROCESSOS DO JOGO");
        var games=GameProcesses();
        if(games.Count==0) lines.AppendLine(english?"None running.":"Nenhum em execução.");
        foreach(var process in games) {
            try { process.Refresh(); lines.AppendLine("- PID "+process.Id+"  "+process.ProcessName+"  |  window: "+(process.MainWindowHandle!=IntPtr.Zero?process.MainWindowTitle:"none")); }
            catch(Exception ex) { lines.AppendLine("- PID "+process.Id+"  |  "+ex.Message); }
            finally { process.Dispose(); }
        }
        return lines.ToString();
    }
    string ReadDiagnosticFile(string path) {
        try { return File.Exists(path)?File.ReadAllText(path).Trim():"missing"; }
        catch(Exception ex) { return "unreadable ("+ex.Message+")"; }
    }
    void ShowDiagnostics() {
        using(var window=new Form()) {
            window.Text=T("Diagnóstico dos controles","Controller diagnostics");
            window.StartPosition=FormStartPosition.CenterParent;
            window.Size=new Size(650,460);
            window.MinimumSize=new Size(480,330);
            window.BackColor=Theme.Background;
            window.ForeColor=Theme.Text;
            window.Icon=Icon;
            var actions=new Panel { Dock=DockStyle.Bottom, Height=44, BackColor=Theme.Card };
            var report=new TextBox {
                Dock=DockStyle.Fill, Multiline=true, ReadOnly=true, ScrollBars=ScrollBars.Both,
                WordWrap=false, BackColor=Theme.Background, ForeColor=Theme.Text,
                BorderStyle=BorderStyle.None, Font=new Font("Consolas",9f), TabStop=true
            };
            var refresh=new Button { Text=T("Atualizar","Refresh"), Width=96, Height=27, Left=8, Top=8, FlatStyle=FlatStyle.Flat, BackColor=Theme.Card, ForeColor=Theme.Text };
            var copy=new Button { Text=T("Copiar relatório","Copy report"), Width=130, Height=27, Left=112, Top=8, FlatStyle=FlatStyle.Flat, BackColor=Theme.Card, ForeColor=Theme.Text };
            var close=new Button { Text=T("Fechar","Close"), Width=86, Height=27, Top=8, FlatStyle=FlatStyle.Flat, BackColor=Theme.Card, ForeColor=Theme.Text, Anchor=AnchorStyles.Top|AnchorStyles.Right };
            close.Left=window.ClientSize.Width-close.Width-8;
            actions.Resize += (s,e) => close.Left=actions.ClientSize.Width-close.Width-8;
            refresh.Click += (s,e) => report.Text=DiagnosticReport();
            copy.Click += (s,e) => { if(report.Text.Length>0) Clipboard.SetText(report.Text); };
            close.Click += (s,e) => window.Close();
            actions.Controls.Add(refresh); actions.Controls.Add(copy); actions.Controls.Add(close);
            window.Controls.Add(report); window.Controls.Add(actions);
            window.AcceptButton=refresh; window.CancelButton=close;
            report.Text=DiagnosticReport();
            window.Shown += (s,e) => { report.SelectionStart=0; report.SelectionLength=0; try { int enabled=1; DwmSetWindowAttribute(window.Handle,20,ref enabled,4); } catch { } };
            window.ShowDialog(this);
        }
    }
    void LayoutHero() {
        int w = hero.ClientSize.Width;
        int gridWidth = w - (vertical.Visible ? vertical.Width : 0);
        sunshineMark.SetBounds(GridGutter,4,48,31);
        heroTitle.SetBounds(GridGutter+55, 7, Math.Max(145,gridWidth-GridGutter-55-154), 25);
        heroSubtitle.Visible=false;
        ptButton.SetBounds(gridWidth-GridGutter-136,5,76,29);
        enButton.SetBounds(gridWidth-GridGutter-54,5,54,29);
        summary.Visible=false;
    }
    void LayoutPlayerCard(RoundedPanel card, Avatar avatar, Label role, Label state,
                          Label pick, ComboBox combo, int x, int y, int width) {
        card.SetBounds(x, y, width, 65);
        avatar.SetBounds(8, 7, 23, 23);
        role.SetBounds(38, 4, width - 48, 18);
        state.SetBounds(38, 21, width - 48, 15);
        pick.Visible=false;
        combo.SetBounds(8, 38, width - 16, 24);
    }
    void LayoutDashboard() {
        if (layingOut || scroll.ClientSize.Width < 200) return;
        layingOut = true; canvas.SuspendLayout();
        try {
            const int canvasHeight = 345;
            bool needsScroll = canvasHeight > scroll.ClientSize.Height;
            int w = scroll.ClientSize.Width - (needsScroll ? vertical.Width : 0);
            int pad = GridGutter, gap = 6, content = w - pad * 2;
            int col = (content - gap) / 2;
            canvas.Width = w;
            controlsHeading.SetBounds(pad, 1, 120, 22);
            refreshButton.SetBounds(w-pad-70, 1, 70, 22);
            applyButton.SetBounds(w-pad-70-5-132, 1, 132, 22);
            LayoutPlayerCard(marioCard, marioAvatar, marioRole, marioState, marioPick, p1, pad, 25, content);
            LayoutPlayerCard(luigiCard, luigiAvatar, luigiRole, luigiState, luigiPick, p2, pad, 94, content);
            imageCard.SetBounds(pad, 163, col, 66);
            splitCard.SetBounds(pad+col+gap, 163, content-col-gap, 66);
            imageTitle.SetBounds(8, 2, col-16, 17);
            fill.SetBounds(8, 17, col-16, 21);
            borderlessStart.SetBounds(8, 39, col-16, 21);
            splitTitle.SetBounds(8, 2, col-16, 17);
            layout.SetBounds(8, 21, col-16, 23);
            splitHint.SetBounds(8, 45, col-16, 16);
            launchButton.SetBounds(pad, 233, content, 24);
            frameButton.SetBounds(pad, 261, col, 23);
            arrangeButton.SetBounds(pad+col+gap, 261, content-col-gap, 23);
            helpButton.SetBounds(pad, 288, col, 23);
            folderButton.SetBounds(pad+col+gap, 288, content-col-gap, 23);
            statusCard.SetBounds(pad, 315, content, 22);
            active.SetBounds(8, 1, content-16, 18);
            setupHeading.Visible=false;
            actionsHeading.Visible=false;
            footnote.Visible=false;
            canvas.Height = canvasHeight;
            vertical.Visible = needsScroll;
            LayoutHero();
            vertical.Maximum = Math.Max(0, canvas.Height - 1);
            vertical.LargeChange = Math.Max(1, scroll.ClientSize.Height);
            vertical.SmallChange = 42;
            vertical.Value = Math.Min(vertical.Value, Math.Max(0, canvas.Height - scroll.ClientSize.Height));
            canvas.Top = -vertical.Value;
        } finally { canvas.ResumeLayout(); layingOut = false; }
    }
    void ScrollWheel(object sender, MouseEventArgs e) {
        if(!vertical.Visible) return;
        int limit=Math.Max(0,canvas.Height-scroll.ClientSize.Height);
        vertical.Value=Math.Max(0,Math.Min(limit,vertical.Value-Math.Sign(e.Delta)*60));
        canvas.Top=-vertical.Value;
    }
    void UpdateControllerCards() {
        SetPadStatus(marioState, p1.SelectedItem as Pad);
        SetPadStatus(luigiState, p2.SelectedItem as Pad);
        int ready = pads.FindAll(x => x.Available).Count;
        summary.Text = "●  " + ready + T(ready==1?" controle pronto":" controles prontos",ready==1?" controller ready":" controllers ready");
        summary.ForeColor = ready >= 2 ? Theme.Green : Theme.Muted;
    }
    void SetPadStatus(Label label, Pad pad) {
        if (pad == null) { label.Text = "●  "+T("Nenhum selecionado","None selected"); label.ForeColor = Theme.Muted; }
        else if (pad.Available) { label.Text = "●  "+T("Pronto","Ready")+"  ·  " + pad.Id; label.ForeColor = Theme.Green; }
        else { label.Text = "●  "+T("Sem entrada de jogo","No game input")+"  ·  " + pad.Id; label.ForeColor = Theme.Red; }
    }
    void ClosePads() {
        timer.Stop();
        foreach (var pad in pads) if (pad.Handle != IntPtr.Zero) SDL_GameControllerClose(pad.Handle);
        pads.Clear(); if (sdlReady) { SDL_Quit(); sdlReady=false; }
    }
    void RefreshPads() {
        string oldP1=(p1.SelectedItem as Pad)==null?null:((Pad)p1.SelectedItem).Id;
        string oldP2=(p2.SelectedItem as Pad)==null?null:((Pad)p2.SelectedItem).Id;
        ClosePads(); p1.Items.Clear(); p2.Items.Clear();
        try {
            var dll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SDL2.dll");
            if (!File.Exists(dll)) throw new FileNotFoundException(T("SDL2.dll não encontrado ao lado do executável.","SDL2.dll was not found next to the launcher."));
            if (SDL_Init(0x00002000u) != 0) throw new Exception(T("SDL não iniciou.","SDL failed to start."));
            sdlReady=true;
            for(int i=0;i<SDL_NumJoysticks();i++) {
                var pad=new Pad();
                pad.Id=SDL_JoystickGetDeviceVendor(i).ToString("x4")+":"+SDL_JoystickGetDeviceProduct(i).ToString("x4");
                pad.Name=Marshal.PtrToStringAnsi(SDL_JoystickNameForIndex(i)) ?? "Controle";
                pad.SdlIndex=i;
                pad.Handle=SDL_GameControllerOpen(i); pad.Available=pad.Handle!=IntPtr.Zero;
                pads.Add(pad); p1.Items.Add(pad); p2.Items.Add(pad);
            }
            // Windows may show a Bluetooth device as connected while its HID gamepad
            // interface has not appeared. Show it as unavailable, not as a usable pad.
            try {
                using(var search=new ManagementObjectSearcher("SELECT Name,PNPDeviceID FROM Win32_PnPEntity"))
                foreach(ManagementObject device in search.Get()) {
                    string instance=(device["PNPDeviceID"] as string) ?? "";
                    if(!instance.Contains("00001200")) continue;
                    var match=Regex.Match(instance,@"VID&0002([0-9A-Fa-f]{4})_PID&([0-9A-Fa-f]{4})");
                    if(!match.Success) continue;
                    string id=match.Groups[1].Value.ToLowerInvariant()+":"+match.Groups[2].Value.ToLowerInvariant();
                    if(pads.Exists(x=>x.Id==id)) continue;
                    var pad=new Pad { Id=id, Name="Bluetooth", Available=false };
                    pads.Add(pad); p1.Items.Add(pad); p2.Items.Add(pad);
                }
            } catch { /* SDL remains authoritative when WMI is unavailable. */ }
            if(pads.Count>0) p1.SelectedIndex=Math.Max(0,pads.FindIndex(x=>x.Id==oldP1));
            if(pads.Count>1) p2.SelectedIndex=oldP2==null?1:Math.Max(0,pads.FindIndex(x=>x.Id==oldP2));
            active.Text=ControllerSummary();
            UpdateControllerCards();
        } catch(Exception ex) { MessageBox.Show(ex.Message,"Controles",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        timer.Start();
    }
    void PollPads() {
        if(!sdlReady) return;
        SDL_GameControllerUpdate();
        foreach(var pad in pads) for(int b=0;b<21;b++)
            if(pad.Handle!=IntPtr.Zero && SDL_GameControllerGetButton(pad.Handle,b)!=0) {
                active.Text=T("Botão pressionado: ","Button pressed: ")+pad.ToString(); return;
            }
    }
    void Launch(object sender, EventArgs e) {
        if(!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SplitscreenFix.config.json"))) {
            MessageBox.Show(T("Execute Instalar.cmd nesta pasta antes de iniciar o jogo. O instalador localiza a ISO e cria os atalhos.","Run Instalar.cmd in this folder before starting the game. The installer locates the ISO and creates shortcuts."),T("Instalação necessária","Installation required"));
            return;
        }
        if(GameProcesses().Count>0) {
            MessageBox.Show(T("Já há uma instância do jogo aberta. Feche as janelas atuais após salvar e então inicie a nova sessão pelo lançador.","A game instance is already open. Save, close the current windows, then start a new session from the launcher."),T("Jogo em execução","Game running"));
            return;
        }
        var a=p1.SelectedItem as Pad; var b=p2.SelectedItem as Pad;
        if(a!=null&&!a.Available || b!=null&&!b.Available) {
            MessageBox.Show(T("Um controle aparece no Bluetooth, mas ainda não oferece entrada HID/SDL ao jogo. Desligue e reconecte esse controle dentro da partida; depois clique em Atualizar controles. O filtro por ID não corrige a ausência da interface de entrada.","A controller appears in Bluetooth but provides no HID/SDL game input. Reconnect it while in-game, then click Refresh. An ID filter cannot repair a missing input interface.")); return;
        }
        if(a==null||b==null||a.Id==b.Id) {
            MessageBox.Show(T("Selecione dois controles com IDs diferentes. Se ambos tiverem o mesmo ID, este filtro SDL não consegue separá-los.","Select two controllers with different IDs. This SDL filter cannot separate controllers that share the same ID.")); return;
        }
        var script=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Iniciar_Eclipse_Multiplayer_Controles.ps1");
        if(!File.Exists(script)) { MessageBox.Show(T("Script de lançamento não encontrado: ","Launch script not found: ")+script); return; }
        var args="-NoProfile -ExecutionPolicy Bypass -File \""+script+"\" -Mode Both -HostController "+a.Id+
            " -ClientController "+b.Id+" -Aspect "+(fill.Checked?"Stretch":"Keep")+
            " -Layout "+(layout.SelectedIndex==0?"TopBottom":"SideBySide")+
            (borderlessStart.Checked?" -Borderless":"");
        try { Process.Start(new ProcessStartInfo("powershell.exe",args) { UseShellExecute=true, WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory }); }
        catch(Exception ex) { MessageBox.Show(ex.Message,T("Inicialização","Startup"),MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }
    List<Process> GameProcesses() {
        var result=new List<Process>();
        result.AddRange(Process.GetProcessesByName("sms"));
        result.AddRange(Process.GetProcessesByName("sms-rebind"));
        return result;
    }
    string BindingPath(string role) {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "SuperMarioEclipse", "pad-bindings", role+".txt");
    }
    void WriteBinding(string role,string id) {
        string path=BindingPath(role);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        File.WriteAllText(temporary,id.ToLowerInvariant()+"\n",System.Text.Encoding.ASCII);
        try {
            if(File.Exists(path)) File.Replace(temporary,path,null);
            else File.Move(temporary,path);
        } finally { if(File.Exists(temporary)) File.Delete(temporary); }
    }
    void ApplyLiveBindings() {
        try {
            var a=p1.SelectedItem as Pad; var b=p2.SelectedItem as Pad;
            var windows=new List<Process>();
            foreach(var process in GameProcesses()) if(process.MainWindowHandle!=IntPtr.Zero) windows.Add(process);
            if(windows.Count==0) throw new Exception("Nenhuma janela do jogo está aberta.");
            if(windows.Exists(x=>x.ProcessName.Equals("sms",StringComparison.OrdinalIgnoreCase)))
                throw new Exception("As janelas abertas usam a versão anterior do jogo. Salve a partida e inicie uma vez pelo launcher atualizado; depois as trocas funcionarão sem fechar o jogo.");
            Process host=null,client=null;
            foreach(var process in windows) {
                string title=process.MainWindowTitle.ToLowerInvariant();
                if(title.Contains("jogador 1") || title.Contains("mario (host)")) {
                    if(host!=null) throw new Exception("Mais de uma janela foi identificada como Mario. Feche a sessão extra para evitar aplicar ao processo errado.");
                    host=process;
                }
                if(title.Contains("jogador 2") || title.Contains("luigi (client)")) {
                    if(client!=null) throw new Exception("Mais de uma janela foi identificada como Luigi. Feche a sessão extra para evitar aplicar ao processo errado.");
                    client=process;
                }
            }
            if(host==null && client==null) throw new Exception("Não consegui identificar Mario e Luigi pelos títulos das janelas. Inicie o jogo pelo launcher.");
            if((host!=null && (a==null || !a.Available)) ||
               (client!=null && (b==null || !b.Available)) ||
               (host!=null && client!=null && a.Id==b.Id))
                throw new Exception("Escolha um controle disponível para cada janela aberta, com IDs diferentes. Clique em Atualizar controles se acabou de conectar um novo.");
            if(host!=null) WriteBinding("host",a.Id);
            if(client!=null) WriteBinding("client",b.Id);
            var waiting=new List<string>();
            if(host!=null) waiting.Add("host:"+a.Id.ToLowerInvariant());
            if(client!=null) waiting.Add("client:"+b.Id.ToLowerInvariant());
            for(int i=0;i<20 && waiting.Count>0;i++) {
                Application.DoEvents(); System.Threading.Thread.Sleep(100);
                waiting.RemoveAll(item=> {
                    var parts=item.Split(new[]{':'},2);
                    string ack=BindingPath(parts[0])+".ack";
                    return File.Exists(ack) && File.ReadAllText(ack).Trim().Equals(parts[1],StringComparison.OrdinalIgnoreCase);
                });
            }
            if(waiting.Count>0) throw new Exception("O jogo não confirmou o novo controle em: "+String.Join(", ",waiting.ToArray())+". Confira se a janela ainda está aberta e consulte o log do jogo.");
            MessageBox.Show(T("Vínculos atualizados. ","Bindings updated. ")+(host!=null?"Mario: "+a.Id+". ":"")+
                            (client!=null?"Luigi: "+b.Id+". ":"")+T("Teste um botão em cada janela.","Test a button in each window."),T("Controles aplicados","Controllers applied"));
        } catch(Exception ex) { MessageBox.Show(ex.Message,T("Não foi possível atualizar","Could not update"),MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }
    Process[] FindGamePair() {
        var windows=new List<Process>();
        foreach(var process in GameProcesses())
            if(process.MainWindowHandle!=IntPtr.Zero) windows.Add(process);
        if(windows.Count<2) throw new Exception("Abra as duas janelas de Mario e Luigi primeiro.");
        windows.Sort((a,b) => a.StartTime.CompareTo(b.StartTime));
        Process host=null, client=null;
        foreach(var process in windows) {
            var title=process.MainWindowTitle.ToLowerInvariant();
            if(title.Contains("jogador 1") || title.Contains("mario (host)")) host=process;
            if(title.Contains("jogador 2") || title.Contains("luigi (client)")) client=process;
        }
        if(host==null || client==null || host.Id==client.Id) {
            if(windows.Count!=2) throw new Exception("Há mais de duas janelas e não consegui distinguir Mario de Luigi pelos títulos.");
            host=windows[0]; client=windows[1];
        }
        return new[] { host, client };
    }
    void ArrangePair(Process host, Process client) {
        var area=Screen.PrimaryScreen.WorkingArea;
        int topHeight=area.Height/2;
        PutWindow(host,"Jogador 1 - Mario (Host)",area.X,area.Y,area.Width,topHeight);
        PutWindow(client,"Jogador 2 - Luigi (Client)",area.X,area.Y+topHeight,area.Width,area.Height-topHeight);
    }
    void ArrangeExisting() {
        try {
            var pair=FindGamePair();
            ArrangePair(pair[0],pair[1]);
            MessageBox.Show(T("Pronto: Mario em cima e Luigi embaixo. A conexão e os controles continuam como estavam.","Done: Mario on top and Luigi below. Connection and controllers are unchanged."),T("Janelas organizadas","Windows arranged"));
        } catch(Exception ex) { MessageBox.Show(ex.Message,T("Não foi possível organizar","Could not arrange windows"),MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }
    void ToggleBorderless() {
        try {
            var pair=FindGamePair();
            const int frameMask=0x00C40000; // WS_CAPTION | WS_THICKFRAME
            bool bothBorderless=true;
            foreach(var process in pair) if((GetWindowLong(process.MainWindowHandle,-16)&frameMask)!=0) bothBorderless=false;
            bool turnOn=!bothBorderless;
            foreach(var process in pair) {
                IntPtr handle=process.MainWindowHandle;
                ShowWindow(handle,9);
                int style=GetWindowLong(handle,-16);
                int changed=turnOn ? style & ~frameMask : style | frameMask;
                SetWindowLong(handle,-16,changed);
                if(!SetWindowPos(handle,IntPtr.Zero,0,0,0,0,0x27))
                    throw new Exception("Não consegui atualizar a moldura da janela "+process.Id+".");
            }
            ArrangePair(pair[0],pair[1]);
            MessageBox.Show(turnOn?
                T("Molduras removidas. Para preencher cada metade sem barras ou distorção, inicie a próxima partida com 'Abrir sem borda' marcado.","Frames removed. For a full view without bars or distortion, start the next session with 'Start borderless' checked."):
                T("Molduras restauradas. As janelas foram reorganizadas.","Frames restored. Windows have been arranged."),T("Moldura das janelas","Window frames"));
        } catch(Exception ex) { MessageBox.Show(ex.Message,T("Não foi possível alterar a moldura","Could not change frames"),MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }
    void PutWindow(Process process,string title,int x,int y,int width,int height) {
        process.Refresh();
        if(process.HasExited || process.MainWindowHandle==IntPtr.Zero) throw new Exception("Uma janela do jogo fechou durante a organização.");
        ShowWindow(process.MainWindowHandle,9); // restore a maximized window
        if(!MoveWindow(process.MainWindowHandle,x,y,width,height,true)) throw new Exception("Falha ao mover a janela "+process.Id+".");
        SetWindowText(process.MainWindowHandle,title);
    }
    void ShowHelp() {
        if(english) { MessageBox.Show("1. Connect both controllers and click Refresh. Press a button on each to identify its ID. A controller without game input must be reconnected while in-game.\n\n"+
          "2. Assign one controller to Mario (host) and another to Luigi (client). Click Start Mario + Luigi. Enter a level with Mario; Luigi connects afterward.\n\n"+
          "3. Top / bottom fills the screen better without stretching characters. Leave Stretch image off. Start borderless removes window frames at launch.\n\n"+
          "4. During play, connect a replacement controller, click Refresh, assign it to the correct player, then click Apply controllers. The game confirms each new binding.\n\n"+
          "5. Arrange restores the two windows' positions. Toggle frame changes their current borders; field of view is recalculated on the next launch.\n\n"+
          "6. Controller button mapping comes from the PC port. This launcher selects which controller belongs to each game process.","Help",MessageBoxButtons.OK,MessageBoxIcon.Information); return; }
        MessageBox.Show("1. Ligue os dois controles e clique em Atualizar controles.\n\n"+
          "2. Aperte um botão em cada controle. O ID que aparece identifica qual item escolher para Mario e Luigi. Um iPega pode aparecer como PS4 ou Xbox conforme o modo. Se disser SEM ENTRADA DE JOGO, reconecte o controle; o Windows o listou mas o jogo não recebe seus botões.\n\n"+
          "3. Escolha os controles. A janela Host é Jogador 1 (Mario); a Client é Jogador 2 (Luigi).\n\n"+
          "4. Inicie. Entre em uma fase com Mario; o lançador espera e só então conecta Luigi. Não feche a janela de comandos enquanto estiver iniciando.\n\n"+
          "5. Em cima / embaixo amplia a câmera até a proporção de cada faixa e preenche a tela sem deformar os personagens. Deixe 'Esticar a imagem' desmarcado. Lado a lado não permite esse encaixe sem barras.\n\n"+
          "6. Marque Abrir sem borda antes de abrir o jogo para preencher as duas faixas sem distorcer. Durante a partida, Alternar moldura remove ou restaura as bordas sem reiniciar, mas o campo de visão só é recalculado na próxima abertura.\n\n"+
          "7. Para trocar controles durante a partida, conecte-os, clique em Atualizar controles, escolha Mario e Luigi e clique em Aplicar controles às janelas abertas. O jogo confirma cada vínculo. Isso também funciona quando um controle descarrega e outro é conectado. As janelas precisam ter sido iniciadas pelo launcher atualizado uma vez.\n\n"+
          "8. Se as janelas saírem do lugar, clique em Organizar janelas agora. Os botões do jogo seguem os bindings do port; aqui você escolhe qual controle pertence a cada processo.",
          "Tutorial",MessageBoxButtons.OK,MessageBoxIcon.Information);
    }
    [STAThread] static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(SplitFixSoloSaveV1.Wrap(SplitFixSoloCoopLog.Wrap(new Launcher()))); }
}


// _SPLITFIX_SOLO_COOP_LOG_V1_
// Complemento ao launcher original. Nao altera o jogo nem os saves.
public static class SplitFixSoloCoopLog
{
    private static readonly System.Collections.Generic.List<System.Diagnostics.Process> Children =
        new System.Collections.Generic.List<System.Diagnostics.Process>();
    private static System.Windows.Forms.Form MainForm;
    private static System.Windows.Forms.Control LegacyStart;
    private static System.Windows.Forms.Button SoloButton;
    private static System.Windows.Forms.Button CoopButton;
    private static System.Windows.Forms.Control StatusControl;
    private static System.Windows.Forms.Timer RefreshTimer;
    private static string InstallDir;
    private const string IdPattern = @"(?i)\b[0-9a-f]{4}:[0-9a-f]{4}\b";

    public static System.Windows.Forms.Form Wrap(System.Windows.Forms.Form form)
    {
        MainForm = form;
        InstallDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        form.Shown += delegate(object sender, System.EventArgs e) { Install(); };
        return form;
    }

    private static bool IsEnglish()
    {
        try {
            string file = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                @"SplitscreenFix\ui-language.txt");
            return System.IO.File.Exists(file) && System.IO.File.ReadAllText(file).Trim().ToLowerInvariant() == "en";
        } catch { return false; }
    }

    private static System.Collections.Generic.List<System.Windows.Forms.Control> Descendants(System.Windows.Forms.Control root)
    {
        System.Collections.Generic.List<System.Windows.Forms.Control> result = new System.Collections.Generic.List<System.Windows.Forms.Control>();
        Walk(root, result);
        return result;
    }

    private static void Walk(System.Windows.Forms.Control parent,
        System.Collections.Generic.List<System.Windows.Forms.Control> result)
    {
        foreach (System.Windows.Forms.Control child in parent.Controls) {
            result.Add(child);
            Walk(child, result);
        }
    }

    private static int Y(System.Windows.Forms.Control c)
    {
        try { return c.PointToScreen(System.Drawing.Point.Empty).Y; }
        catch { return c.Top; }
    }

    private static bool IsStartText(string text)
    {
        if (text == null) return false;
        string t = text.ToLowerInvariant();
        return (t.Contains("mario") && t.Contains("luigi") &&
            (t.Contains("iniciar") || t.Contains("start") || t.Contains("jogar")));
    }

    private static bool IsMissingText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string t = text.ToLowerInvariant();
        return t.Contains("sem entrada") || t.Contains("no game input") ||
            t.Contains("no input") || t.Contains("unavailable") || t.Contains("disconnected") ||
            t.Contains("nao conectado") || t.Contains("nÃ£o conectado");
    }

    private static void Install()
    {
        try {
            System.Collections.Generic.List<System.Windows.Forms.Control> all = Descendants(MainForm);
            foreach (System.Windows.Forms.Control c in all) {
                if (IsStartText(c.Text) && c.Height >= 18 && c.Width >= 110) {
                    LegacyStart = c;
                    break;
                }
            }
            if (LegacyStart == null || LegacyStart.Parent == null) {
                System.Windows.Forms.MessageBox.Show(MainForm,
                    "O botao original de iniciar nao foi encontrado. O launcher foi mantido sem alteracao visual.\nConsulte o LOG ou envie o EclipseLauncher.cs.",
                    "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            System.Windows.Forms.Control parent = LegacyStart.Parent;
            System.Drawing.Rectangle b = LegacyStart.Bounds;
            int gap = 5;
            int leftWidth = (b.Width - gap) / 2;
            int rightWidth = b.Width - leftWidth - gap;
            SoloButton = NewButton(IsEnglish() ? "Start 1P - SOLO" : "Iniciar 1P - SOLO",
                System.Drawing.Color.FromArgb(35, 137, 198));
            CoopButton = NewButton(IsEnglish() ? "Start 1P + 2P - COOP" : "Iniciar 1P + 2P - COOP",
                System.Drawing.Color.FromArgb(50, 180, 115));
            SoloButton.SetBounds(b.X, b.Y, leftWidth, b.Height);
            CoopButton.SetBounds(b.X + leftWidth + gap, b.Y, rightWidth, b.Height);
            SoloButton.Anchor = LegacyStart.Anchor;
            CoopButton.Anchor = LegacyStart.Anchor;
            LegacyStart.Visible = false;
            parent.Controls.Add(SoloButton);
            parent.Controls.Add(CoopButton);
            SoloButton.BringToFront();
            CoopButton.BringToFront();
            SoloButton.Click += delegate(object s, System.EventArgs e) { Launch(false); };
            CoopButton.Click += delegate(object s, System.EventArgs e) { Launch(true); };
            MainForm.Activated += delegate(object s, System.EventArgs e) { RefreshLanguageAndStatus(); };
            RefreshTimer = new System.Windows.Forms.Timer();
            RefreshTimer.Interval = 800;
            RefreshTimer.Tick += delegate(object s, System.EventArgs e) { RefreshLanguageAndStatus(); };
            RefreshTimer.Start();
            RefreshLanguageAndStatus();
            SplitFixNoGamepadV2.Install(MainForm);
        } catch (System.Exception ex) {
            System.Windows.Forms.MessageBox.Show(MainForm, ex.ToString(), "SplitscreenFix - erro ao preparar interface",
                System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    private static System.Windows.Forms.Button NewButton(string text, System.Drawing.Color color)
    {
        System.Windows.Forms.Button b = new System.Windows.Forms.Button();
        b.Text = text;
        b.UseVisualStyleBackColor = false;
        b.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = color;
        b.ForeColor = System.Drawing.Color.White;
        b.Font = new System.Drawing.Font("Segoe UI", 8.0f, System.Drawing.FontStyle.Bold);
        b.TabStop = true;
        return b;
    }

    private static void RefreshLanguageAndStatus()
    {
        if (SoloButton != null) SoloButton.Text = IsEnglish() ? "Start 1P - SOLO" : "Iniciar 1P - SOLO";
        if (CoopButton != null) CoopButton.Text = IsEnglish() ? "Start 1P + 2P - COOP" : "Iniciar 1P + 2P - COOP";
        if (StatusControl == null) {
            foreach (System.Windows.Forms.Control c in Descendants(MainForm)) {
                string t = c.Text == null ? "" : c.Text.ToLowerInvariant();
                if ((t.Contains("pronto") || t.Contains("ready")) &&
                    (t.Contains("entrada") || t.Contains("input")) && c.Width >= 150) {
                    StatusControl = c;
                    c.Cursor = System.Windows.Forms.Cursors.Hand;
                    c.TextChanged += delegate(object sender, System.EventArgs e) { SetLogText(); };
                    break;
                }
            }
        }
        SetLogText();
    }

    private static void SetLogText()
    {
        if (StatusControl == null) return;
        string t = StatusControl.Text;
        if (t == null || t.ToUpperInvariant().Contains("LOG")) return;
        t = System.Text.RegularExpressions.Regex.Replace(t, @"\s*[>â€º]\s*$", "");
        StatusControl.Text = t.TrimEnd() + " Â· LOG â€º";
    }

    private static System.Collections.Generic.List<System.Windows.Forms.ComboBox> Pads()
    {
        System.Collections.Generic.List<System.Windows.Forms.ComboBox> result =
            new System.Collections.Generic.List<System.Windows.Forms.ComboBox>();
        foreach (System.Windows.Forms.Control c in Descendants(MainForm)) {
            System.Windows.Forms.ComboBox b = c as System.Windows.Forms.ComboBox;
            if (b != null && System.Text.RegularExpressions.Regex.IsMatch(b.Text, IdPattern)) result.Add(b);
        }
        result.Sort(delegate(System.Windows.Forms.ComboBox a, System.Windows.Forms.ComboBox b) { return Y(a).CompareTo(Y(b)); });
        return result;
    }

    private static string PadId(System.Windows.Forms.ComboBox cb)
    {
        if (cb == null) return "";
        System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(cb.Text, IdPattern);
        return m.Success ? m.Value.ToLowerInvariant() : "";
    }

    private static bool Missing(System.Windows.Forms.ComboBox cb)
    {
        if (cb == null || PadId(cb).Length == 0 || IsMissingText(cb.Text)) return true;
        int y = Y(cb);
        foreach (System.Windows.Forms.Control c in Descendants(MainForm)) {
            if (c == cb || c == StatusControl) continue;
            // O status individual fica imediatamente acima do seletor de cada jogador.
            if (System.Math.Abs(Y(c) - y) <= 38 && IsMissingText(c.Text)) return true;
        }
        return false;
    }

    private static bool ConfirmMissing(bool solo, bool missing1, bool missing2)
    {
        if (!missing1 && (solo || !missing2)) return true;
        bool english = IsEnglish();
        string who = missing1 && (!missing2 || solo) ?
            (english ? "Player 1 (Mario)" : "Jogador 1 (Mario)") :
            (english ? "Player 2 (Luigi)" : "Jogador 2 (Luigi)");
        if (missing1 && missing2 && !solo) who = english ? "Players 1 and 2" : "Jogadores 1 e 2";
        string message = english ?
            "You are starting without a working controller.\n" + who +
            " is missing a controller.\n\nPlay with controller + keyboard/mouse?" :
            "Voce esta iniciando o jogo sem um controle.\n" + who +
            " esta sem controle.\n\nPretende jogar com controle + teclado/mouse?";
        using (System.Windows.Forms.Form dlg = new System.Windows.Forms.Form()) {
            dlg.Text = english ? "Controller warning" : "Aviso de controle";
            dlg.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;
            dlg.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            dlg.ClientSize = new System.Drawing.Size(410, 184);
            dlg.BackColor = System.Drawing.Color.FromArgb(24, 29, 43);
            dlg.ForeColor = System.Drawing.Color.White;
            System.Windows.Forms.Label description = new System.Windows.Forms.Label();
            description.Text = message;
            description.Font = new System.Drawing.Font("Segoe UI", 9.3f);
            description.ForeColor = System.Drawing.Color.White;
            description.SetBounds(18, 18, 380, 113);
            dlg.Controls.Add(description);
            System.Windows.Forms.Button cancel = NewButton(english ? "Cancel" : "Cancelar",
                System.Drawing.Color.FromArgb(85, 94, 110));
            cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancel.SetBounds(190, 140, 92, 29);
            System.Windows.Forms.Button confirm = NewButton(english ? "Continue" : "Continuar",
                System.Drawing.Color.FromArgb(41, 175, 113));
            confirm.DialogResult = System.Windows.Forms.DialogResult.OK;
            confirm.SetBounds(292, 140, 100, 29);
            dlg.Controls.Add(cancel);
            dlg.Controls.Add(confirm);
            dlg.CancelButton = cancel;
            dlg.AcceptButton = confirm;
            return dlg.ShowDialog(MainForm) == System.Windows.Forms.DialogResult.OK;
        }
    }

    private static void Launch(bool coop)
    {
        try {
            System.Collections.Generic.List<System.Windows.Forms.ComboBox> pads = SplitFixNoGamepadV2.GamepadCombos(MainForm);
            System.Windows.Forms.ComboBox cb1 = pads.Count > 0 ? pads[0] : null;
            System.Windows.Forms.ComboBox cb2 = pads.Count > 1 ? pads[1] : null;
            bool missing1 = Missing(cb1);
            bool missing2 = Missing(cb2);
            if (!ConfirmMissing(!coop, missing1 && !SplitFixNoGamepadV2.IsNoGamepad(cb1), missing2 && !SplitFixNoGamepadV2.IsNoGamepad(cb2))) return;

            string host = missing1 ? "0000:0000" : PadId(cb1);
            string client = missing2 ? "0000:0001" : PadId(cb2);
            if (host.Length == 0) host = "0000:0000";
            if (client.Length == 0) client = "0000:0001";
            if (host == client) {
                System.Windows.Forms.MessageBox.Show(MainForm,
                    IsEnglish() ? "Both players selected the same device. Choose separate controllers." :
                        "Os dois jogadores selecionaram o mesmo controle. Escolha dispositivos diferentes.",
                    "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            if (!coop) {
                string bindDir = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    @"SuperMarioEclipse\pad-bindings");
                System.IO.Directory.CreateDirectory(bindDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(bindDir, "host.txt"), host + "\n",
                    System.Text.Encoding.ASCII);
                StartScript("Iniciar_Eclipse_Solo.ps1", "-Jogador 1 -Janela -SoloSaveProfile " + SplitFixSoloSaveV1.CurrentProfile);
            } else {
                string layout = "SideBySide";
                string aspect = "Keep";
                bool borderless = false;
                foreach (System.Windows.Forms.Control c in Descendants(MainForm)) {
                    string text = (c.Text ?? "").ToLowerInvariant();
                    if (c is System.Windows.Forms.ComboBox) {
                        if (text.Contains("em cima") || text.Contains("embaixo") ||
                            text.Contains("top") && text.Contains("bottom")) layout = "TopBottom";
                    }
                    System.Windows.Forms.CheckBox check = c as System.Windows.Forms.CheckBox;
                    if (check != null) {
                        if (text.Contains("esticar") || text.Contains("stretch")) aspect = check.Checked ? "Stretch" : "Keep";
                        if (text.Contains("borda") || text.Contains("borderless")) borderless = check.Checked;
                    }
                }
                string args = "-Mode Both -HostController " + host + " -ClientController " + client +
                    " -Aspect " + aspect + " -Layout " + layout + (borderless ? " -Borderless" : "");
                StartScript("Iniciar_Eclipse_Multiplayer_Controles.ps1", args);
            }
        } catch (System.Exception ex) {
            System.Windows.Forms.MessageBox.Show(MainForm, ex.Message,
                IsEnglish() ? "Start error" : "Erro ao iniciar",
                System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    private static void StartScript(string file, string arguments)
    {
        string fullPath = System.IO.Path.Combine(InstallDir, file);
        if (!System.IO.File.Exists(fullPath)) throw new System.IO.FileNotFoundException("Script nao encontrado", fullPath);
        string debugPath = System.IO.Path.Combine(InstallDir, "Debug");
        System.IO.Directory.CreateDirectory(debugPath);
        string log = System.IO.Path.Combine(debugPath,
            "launcher-start-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".log");
        System.IO.File.WriteAllText(log, "SplitscreenFix launcher\r\nScript: " + fullPath + "\r\nArgs: " + arguments + "\r\n");
        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
        psi.FileName = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows),
            @"System32\WindowsPowerShell\v1.0\powershell.exe");
        psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + fullPath + "\" " + arguments;
        psi.WorkingDirectory = InstallDir;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        System.Diagnostics.Process p = new System.Diagnostics.Process();
        p.StartInfo = psi;
        p.EnableRaisingEvents = true;
        p.OutputDataReceived += delegate(object sender, System.Diagnostics.DataReceivedEventArgs e) {
            AppendLog(log, e.Data);
        };
        p.ErrorDataReceived += delegate(object sender, System.Diagnostics.DataReceivedEventArgs e) {
            AppendLog(log, e.Data);
        };
        p.Exited += delegate(object sender, System.EventArgs e) {
            try { AppendLog(log, "ExitCode=" + p.ExitCode); } catch { }
        };
        p.Start();
        Children.Add(p);
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
    }

    private static readonly object LogLock = new object();
    private static void AppendLog(string file, string line)
    {
        if (line == null) return;
        try {
            lock (LogLock) { System.IO.File.AppendAllText(file, line + System.Environment.NewLine); }
        } catch { }
    }
}

// _SPLITFIX_NO_GAMEPAD_V2_
// Rebinding independente para Mario e Luigi. O teclado/mouse segue focado na janela ativa.
public static class SplitFixNoGamepadV2
{
    private const string NoneId = "0000:0000";
    private static readonly System.Text.RegularExpressions.Regex Id =
        new System.Text.RegularExpressions.Regex(@"(?i)\b[0-9a-f]{4}:[0-9a-f]{4}\b");
    private static System.Windows.Forms.Form form;
    private static System.Windows.Forms.ComboBox mario;
    private static System.Windows.Forms.ComboBox luigi;
    private static System.Windows.Forms.Control legacyApply;
    private static System.Windows.Forms.Button apply;
    private static System.Windows.Forms.Timer timer;
    private static bool changing;
    private static bool marioNone;
    private static bool luigiNone;

    private static bool English()
    {
        try {
            string path = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                @"SplitscreenFix\ui-language.txt");
            return System.IO.File.Exists(path) &&
                System.IO.File.ReadAllText(path).Trim().Equals("en", System.StringComparison.OrdinalIgnoreCase);
        } catch { return false; }
    }

    private static System.Collections.Generic.List<System.Windows.Forms.Control> All(System.Windows.Forms.Control top)
    {
        System.Collections.Generic.List<System.Windows.Forms.Control> r =
            new System.Collections.Generic.List<System.Windows.Forms.Control>();
        Scan(top, r);
        return r;
    }
    private static void Scan(System.Windows.Forms.Control top,
        System.Collections.Generic.List<System.Windows.Forms.Control> result)
    {
        foreach (System.Windows.Forms.Control c in top.Controls) {
            result.Add(c);
            Scan(c, result);
        }
    }
    private static int Y(System.Windows.Forms.Control c)
    {
        try { return c.PointToScreen(System.Drawing.Point.Empty).Y; }
        catch { return c.Top; }
    }
    private static bool HasControllerId(System.Windows.Forms.ComboBox b)
    {
        if (Id.IsMatch(b.Text ?? "")) return true;
        foreach (object item in b.Items) {
            if (item != null && Id.IsMatch(item.ToString())) return true;
        }
        return false;
    }
    public static System.Collections.Generic.List<System.Windows.Forms.ComboBox> GamepadCombos(System.Windows.Forms.Form f)
    {
        System.Collections.Generic.List<System.Windows.Forms.ComboBox> all =
            new System.Collections.Generic.List<System.Windows.Forms.ComboBox>();
        foreach (System.Windows.Forms.Control c in All(f)) {
            System.Windows.Forms.ComboBox b = c as System.Windows.Forms.ComboBox;
            if (b != null && b.Visible) all.Add(b);
        }
        all.Sort(delegate(System.Windows.Forms.ComboBox a, System.Windows.Forms.ComboBox b) {
            return Y(a).CompareTo(Y(b));
        });
        // As listas Mario/Luigi vem antes da lista "Divisao" na interface original.
        // Se ambas foram identificadas, mantenha as referencias mesmo apos escolher "Sem gamepad".
        System.Collections.Generic.List<System.Windows.Forms.ComboBox> result =
            new System.Collections.Generic.List<System.Windows.Forms.ComboBox>();
        if (mario != null && !mario.IsDisposed && luigi != null && !luigi.IsDisposed) {
            result.Add(mario);
            result.Add(luigi);
            return result;
        }
        foreach (System.Windows.Forms.ComboBox b in all) {
            if (HasControllerId(b)) result.Add(b);
        }
        if (result.Count < 2 && all.Count >= 2) {
            result.Clear();
            result.Add(all[0]);
            result.Add(all[1]);
        }
        if (result.Count > 2) result.RemoveRange(2, result.Count - 2);
        return result;
    }

    private static bool NoneText(string value)
    {
        return value != null &&
            (value.Equals("(Sem gamepad)", System.StringComparison.OrdinalIgnoreCase) ||
             value.Equals("(No gamepad)", System.StringComparison.OrdinalIgnoreCase));
    }
    public static bool IsNoGamepad(System.Windows.Forms.ComboBox b)
    {
        if (b == null) return false;
        if (b == mario && marioNone) return true;
        if (b == luigi && luigiNone) return true;
        return NoneText(b.Text);
    }

    public static void Install(System.Windows.Forms.Form f)
    {
        form = f;
        System.Collections.Generic.List<System.Windows.Forms.ComboBox> cb = GamepadCombos(f);
        if (cb.Count != 2) {
            System.Windows.Forms.MessageBox.Show(f,
                "Nao consegui identificar os dois seletores de controles. Nenhum vinculo foi modificado.",
                "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
            return;
        }
        mario = cb[0];
        luigi = cb[1];
        mario.SelectedIndexChanged += delegate(object s, System.EventArgs e) { Track(mario, true); };
        luigi.SelectedIndexChanged += delegate(object s, System.EventArgs e) { Track(luigi, false); };
        mario.TextChanged += delegate(object s, System.EventArgs e) { Track(mario, true); };
        luigi.TextChanged += delegate(object s, System.EventArgs e) { Track(luigi, false); };
        AddChoice(mario, true);
        AddChoice(luigi, false);

        foreach (System.Windows.Forms.Control c in All(f)) {
            string name = (c.Text ?? "").Trim().ToLowerInvariant();
            if ((name.StartsWith("aplicar controles") || name.StartsWith("apply controllers") ||
                 name == "aplicar" || name == "apply") &&
                c.Parent != null && c.Visible && c.Width > 65) {
                legacyApply = c;
                break;
            }
        }
        if (legacyApply == null) {
            System.Windows.Forms.MessageBox.Show(f,
                "O botao Aplicar controles nao foi encontrado. Os seletores foram atualizados, mas a aplicacao independente nao foi ativada.",
                "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
            return;
        }
        // Substitui somente o botao Aplicar: seu handler antigo exigia dois IDs validos.
        System.Windows.Forms.Control p = legacyApply.Parent;
        System.Drawing.Rectangle r = legacyApply.Bounds;
        apply = new System.Windows.Forms.Button();
        apply.Text = English() ? "Apply controllers" : "Aplicar controles";
        apply.Font = legacyApply.Font;
        apply.ForeColor = legacyApply.ForeColor;
        apply.BackColor = legacyApply.BackColor;
        apply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        apply.FlatAppearance.BorderSize = 1;
        apply.Anchor = legacyApply.Anchor;
        apply.SetBounds(r.X, r.Y, r.Width, r.Height);
        p.Controls.Add(apply);
        legacyApply.Visible = false;
        apply.BringToFront();
        apply.Click += delegate(object s, System.EventArgs e) { Apply(); };
        timer = new System.Windows.Forms.Timer();
        timer.Interval = 700;
        timer.Tick += delegate(object s, System.EventArgs e) {
            if (form == null || form.IsDisposed) { timer.Stop(); return; }
            AddChoice(mario, true);
            AddChoice(luigi, false);
            if (apply != null && !apply.IsDisposed)
                apply.Text = English() ? "Apply controllers" : "Aplicar controles";
        };
        timer.Start();
    }

    private static void Track(System.Windows.Forms.ComboBox b, bool first)
    {
        if (changing) return;
        if (first) marioNone = NoneText(b.Text);
        else luigiNone = NoneText(b.Text);
    }
    private static void AddChoice(System.Windows.Forms.ComboBox b, bool first)
    {
        if (b == null || b.IsDisposed) return;
        string wanted = English() ? "(No gamepad)" : "(Sem gamepad)";
        bool keep = first ? marioNone : luigiNone;
        if (NoneText(b.Text)) keep = true;
        changing = true;
        try {
            for (int n = b.Items.Count - 1; n >= 0; n--) {
                object item = b.Items[n];
                if (item != null && NoneText(item.ToString()) && item.ToString() != wanted)
                    b.Items.RemoveAt(n);
            }
            if (!b.Items.Contains(wanted)) b.Items.Add(wanted);
            if (keep && !NoneText(b.Text)) b.SelectedItem = wanted;
            if (keep && NoneText(b.Text) && b.Text != wanted) b.SelectedItem = wanted;
        } finally { changing = false; }
        if (first) marioNone = keep;
        else luigiNone = keep;
    }

    private static string Choice(System.Windows.Forms.ComboBox b)
    {
        if (b == null || IsNoGamepad(b)) return NoneId;
        System.Text.RegularExpressions.Match found = Id.Match(b.Text ?? "");
        return found.Success ? found.Value.ToLowerInvariant() : NoneId;
    }
    private static void Apply()
    {
        try {
            string host = Choice(mario);
            string client = Choice(luigi);
            if (host != NoneId && client != NoneId &&
                host.Equals(client, System.StringComparison.OrdinalIgnoreCase)) {
                System.Windows.Forms.MessageBox.Show(form,
                    English() ? "This controller is already assigned to the other player." :
                        "Este controle ja esta atribuido ao outro jogador.",
                    "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }
            string folder = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                @"SuperMarioEclipse\pad-bindings");
            System.IO.Directory.CreateDirectory(folder);
            string hostPath = System.IO.Path.Combine(folder, "host.txt");
            string clientPath = System.IO.Path.Combine(folder, "client.txt");
            // O runtime sms-rebind.exe relÃª cada arquivo durante a partida.
            System.IO.File.WriteAllText(hostPath, host + "\n", System.Text.Encoding.ASCII);
            System.IO.File.WriteAllText(clientPath, client + "\n", System.Text.Encoding.ASCII);
            string debug = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Debug");
            System.IO.Directory.CreateDirectory(debug);
            string log = System.IO.Path.Combine(debug, "sem-gamepad-rebind.log");
            System.IO.File.AppendAllText(log, System.DateTime.Now.ToString("s") +
                "  Mario=" + host + "  Luigi=" + client + System.Environment.NewLine);
            System.Windows.Forms.MessageBox.Show(form,
                English() ? "Controller assignments updated. No gamepad means keyboard/mouse when that window has focus." :
                    "Controles atualizados. Sem gamepad permite teclado/mouse quando a janela estiver em foco.",
                "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        } catch (System.Exception ex) {
            System.Windows.Forms.MessageBox.Show(form, ex.Message,
                English() ? "Update failed" : "Falha ao atualizar",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }
}

// _SPLITFIX_SOLO_SAVE_PROFILE_V1_
// Right-click the 1P/Mario card to choose the SOLO save slot.
// This does not modify the selected gamepad or any saved data.
public static class SplitFixSoloSaveV1
{
    public static int CurrentProfile { get { return current; } }
    private static System.Windows.Forms.Form form;
    private static System.Windows.Forms.ContextMenuStrip menu;
    private static System.Windows.Forms.ToolStripMenuItem save1, save2;
    private static System.Windows.Forms.ToolTip tooltip;
    private static System.Windows.Forms.Control label;
    private static int current = 1;
    private static string FilePath {
        get {
            string dir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "SplitscreenFix");
            return System.IO.Path.Combine(dir, "solo-save-profile.txt");
        }
    }
    private static bool English()
    {
        try {
            string file = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                @"SplitscreenFix\ui-language.txt");
            return System.IO.File.Exists(file) &&
                System.IO.File.ReadAllText(file).Trim().Equals("en", System.StringComparison.OrdinalIgnoreCase);
        } catch { return false; }
    }
    public static System.Windows.Forms.Form Wrap(System.Windows.Forms.Form f)
    {
        form = f;
        try {
            if (System.IO.File.Exists(FilePath) && System.IO.File.ReadAllText(FilePath).Trim() == "2") current = 2;
        } catch { current = 1; }
        ApplyEnvironment();
        f.Shown += delegate(object sender, System.EventArgs args) { Install(); };
        return f;
    }
    private static void ApplyEnvironment()
    {
        System.Environment.SetEnvironmentVariable("SPLITFIX_SOLO_SAVE_PROFILE", current.ToString(),
            System.EnvironmentVariableTarget.Process);
    }
    private static void Visit(System.Windows.Forms.Control c,
        System.Collections.Generic.List<System.Windows.Forms.Control> dest)
    {
        foreach (System.Windows.Forms.Control child in c.Controls) {
            dest.Add(child);
            Visit(child, dest);
        }
    }
    private static bool MarioLabel(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        string t = s.ToLowerInvariant();
        return t.Contains("mario") && (t.Contains("1") || t.Contains("host"));
    }
    private static bool ContainsLuigi(System.Windows.Forms.Control c)
    {
        System.Collections.Generic.List<System.Windows.Forms.Control> all =
            new System.Collections.Generic.List<System.Windows.Forms.Control>();
        Visit(c, all);
        foreach (System.Windows.Forms.Control item in all) {
            if ((item.Text ?? "").ToLowerInvariant().Contains("luigi")) return true;
        }
        return false;
    }
    private static void Install()
    {
        Launcher launcher = form as Launcher;
        if (launcher != null) {
            label = launcher.SoloSaveCard;
        }
        if (label == null) {
        System.Collections.Generic.List<System.Windows.Forms.Control> all =
            new System.Collections.Generic.List<System.Windows.Forms.Control>();
        Visit(form, all);
        foreach (System.Windows.Forms.Control c in all) {
            if (MarioLabel(c.Text ?? "") && !(c is System.Windows.Forms.ComboBox)) {
                label = c;
                break;
            }
        }
        }
        if (label == null) {
            System.Windows.Forms.MessageBox.Show(form,
                "1P/Mario card not found. Save-selection shortcut was not installed.",
                "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
            return;
        }
        menu = new System.Windows.Forms.ContextMenuStrip();
        save1 = new System.Windows.Forms.ToolStripMenuItem();
        save2 = new System.Windows.Forms.ToolStripMenuItem();
        save1.Click += delegate(object sender, System.EventArgs args) { Select(1); };
        save2.Click += delegate(object sender, System.EventArgs args) { Select(2); };
        menu.Items.Add(save1);
        menu.Items.Add(save2);
        menu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs args) { UpdateLabels(); };
        tooltip = new System.Windows.Forms.ToolTip();
        label.ContextMenuStrip = menu;
        tooltip.SetToolTip(label, "Right-click: change SOLO save / Botao direito: trocar save SOLO");
        // Attach to the 1P card itself, but never to a common parent that also contains Luigi.
        System.Windows.Forms.Control card = launcher != null ? launcher.SoloSaveCard : label.Parent;
        if (card != null && card != form && !ContainsLuigi(card)) {
            AttachCard(card);
        }
        UpdateLabels();
    }
    private static void AttachCard(System.Windows.Forms.Control p)
    {
        p.ContextMenuStrip = menu;
        foreach (System.Windows.Forms.Control child in p.Controls) {
            // Do not replace gamepad selector context menus.
            if (child is System.Windows.Forms.ComboBox) continue;
            child.ContextMenuStrip = menu;
        }
    }
    private static void UpdateLabels()
    {
        if (save1 == null || save2 == null) return;
        save1.Text = English() ? "SOLO save 1P (Mario)" : "Save SOLO 1P (Mario)";
        save2.Text = English() ? "SOLO save 2P (Luigi)" : "Save SOLO 2P (Luigi)";
        save1.Checked = current == 1;
        save2.Checked = current == 2;
    }
    private static void Select(int value)
    {
        if (value != 1 && value != 2) return;
        current = value;
        try {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
            System.IO.File.WriteAllText(FilePath, current.ToString() + "\n",
                System.Text.Encoding.ASCII);
        } catch (System.Exception ex) {
            System.Windows.Forms.MessageBox.Show(form, ex.Message, "SplitscreenFix",
                System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            return;
        }
        ApplyEnvironment();
        UpdateLabels();
        System.Windows.Forms.MessageBox.Show(form,
            English() ? ("SOLO will use Player " + current + " save. Controller assignments stay unchanged.") :
                        ("SOLO vai usar o save do Jogador " + current + ". O controle 1P nao muda."),
            "SplitscreenFix", System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Information);
    }
}
