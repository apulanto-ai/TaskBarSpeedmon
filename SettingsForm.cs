using System;
using System.Drawing;
using System.Windows.Forms;

/// <summary>
/// Settings dialog. Every change is previewed live on the taskbar via
/// <see cref="Preview"/>; "Abbrechen" lets the caller restore the original.
/// </summary>
class SettingsForm : Form
{
    public event EventHandler Preview;

    public AppSettings Result { get; private set; }
    public bool AutostartEnabled
    {
        get { return _autostart.Checked; }
    }

    private readonly ComboBox      _layout;
    private readonly ComboBox      _unit;
    private readonly ComboBox      _textTheme;
    private readonly NumericUpDown _fontSize;
    private readonly CheckBox      _bold;
    private readonly CheckBox      _graph;
    private readonly Button        _downColor;
    private readonly Button        _upColor;
    private readonly NumericUpDown _offset;
    private readonly CheckBox      _trayIcon;
    private readonly CheckBox      _fullscreen;
    private readonly CheckBox      _autostart;

    private bool _loading;

    public SettingsForm(AppSettings settings, bool autostart, Icon icon)
    {
        Result = settings.Clone();

        Text            = "TaskBarSpeedmon – Einstellungen";
        Icon            = icon;
        Font            = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;
        AutoScaleMode   = AutoScaleMode.Font;
        AutoSize        = true;
        AutoSizeMode    = AutoSizeMode.GrowAndShrink;
        Padding         = new Padding(0, 0, 12, 12);
        ShowInTaskbar   = true;
        TopMost         = true;

        var root = new TableLayoutPanel
        {
            ColumnCount  = 1,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location     = new Point(12, 12),
        };
        Controls.Add(root);

        // ── Anzeige ──────────────────────────────────────────────────────────
        TableLayoutPanel display = AddGroup(root, "Anzeige");
        _layout = AddCombo(display, "Anordnung:", "Zweizeilig (untereinander)", "Einzeilig (nebeneinander)");
        _fontSize = AddNumeric(display, "Schriftgröße (pt):", 7, 16, 0.5m, 1);
        _bold = AddCheck(display, "Werte fett darstellen");
        _unit = AddCombo(display, "Einheit:", "Bytes (MB/s)", "Bits (Mbit/s)");
        _textTheme = AddCombo(display, "Textfarbe:", "Automatisch (wie Windows)", "Hell (für dunkle Taskleiste)", "Dunkel (für helle Taskleiste)");
        _graph = AddCheck(display, "Verlaufsgrafik im Hintergrund anzeigen");

        // ── Farben ───────────────────────────────────────────────────────────
        TableLayoutPanel colors = AddGroup(root, "Farben");
        _downColor = AddColorButton(colors, "Download (↓):");
        _upColor   = AddColorButton(colors, "Upload (↑):");

        // ── Position ─────────────────────────────────────────────────────────
        TableLayoutPanel position = AddGroup(root, "Position");
        _offset = AddNumeric(position, "Abstand zum Infobereich (px):", 0, 3000, 10, 0);
        var hint = new Label
        {
            Text        = "Tipp: Die Anzeige lässt sich auch direkt auf der Taskleiste mit der Maus verschieben.",
            AutoSize    = true,
            MaximumSize = new Size(330, 0),
            ForeColor   = SystemColors.GrayText,
            Margin      = new Padding(3, 6, 3, 3),
        };
        position.Controls.Add(hint);
        position.SetColumnSpan(hint, 2);

        // ── Verhalten ────────────────────────────────────────────────────────
        TableLayoutPanel behavior = AddGroup(root, "Verhalten", 1);
        _trayIcon   = AddCheck(behavior, "Zusätzliches Symbol im Infobereich anzeigen");
        _fullscreen = AddCheck(behavior, "Bei Vollbild-Anwendungen ausblenden");
        _autostart  = AddCheck(behavior, "Mit Windows starten");

        // ── Buttons ──────────────────────────────────────────────────────────
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize      = true,
            Anchor        = AnchorStyles.Right,
            Margin        = new Padding(0, 10, 0, 0),
        };
        var cancel   = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(90, 0) };
        var ok       = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(90, 0) };
        var defaults = new Button { Text = "Standard", AutoSize = true, MinimumSize = new Size(90, 0) };
        defaults.Click += delegate
        {
            var d = new AppSettings();
            LoadValues(d, _autostart.Checked);
            OnChanged(null, EventArgs.Empty);
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        buttons.Controls.Add(defaults);
        root.Controls.Add(buttons);

        AcceptButton = ok;
        CancelButton = cancel;

        LoadValues(settings, autostart);

        // Wire up live preview after the initial values are set.
        foreach (ComboBox c in new[] { _layout, _unit, _textTheme })
            c.SelectedIndexChanged += OnChanged;
        foreach (NumericUpDown n in new[] { _fontSize, _offset })
            n.ValueChanged += OnChanged;
        foreach (CheckBox c in new[] { _bold, _graph, _trayIcon, _fullscreen })
            c.CheckedChanged += OnChanged;
    }

    private void LoadValues(AppSettings s, bool autostart)
    {
        _loading = true;
        _layout.SelectedIndex    = s.Layout == WidgetLayout.TwoLines ? 0 : 1;
        _unit.SelectedIndex      = s.Unit == SpeedUnit.Bytes ? 0 : 1;
        _textTheme.SelectedIndex = (int)s.TextTheme;
        _fontSize.Value          = (decimal)Math.Max(7f, Math.Min(16f, s.FontSize));
        _bold.Checked            = s.BoldValues;
        _graph.Checked           = s.ShowGraph;
        _downColor.BackColor     = s.DownloadColor;
        _upColor.BackColor       = s.UploadColor;
        _offset.Value            = Math.Min(_offset.Maximum, s.Offset);
        _trayIcon.Checked        = s.ShowTrayIcon;
        _fullscreen.Checked      = s.HideOnFullscreen;
        _autostart.Checked       = autostart;
        _loading = false;
    }

    private void OnChanged(object sender, EventArgs e)
    {
        if (_loading) return;

        Result.Layout           = _layout.SelectedIndex == 0 ? WidgetLayout.TwoLines : WidgetLayout.OneLine;
        Result.Unit             = _unit.SelectedIndex == 0 ? SpeedUnit.Bytes : SpeedUnit.Bits;
        Result.TextTheme        = (TextTheme)_textTheme.SelectedIndex;
        Result.FontSize         = (float)_fontSize.Value;
        Result.BoldValues       = _bold.Checked;
        Result.ShowGraph        = _graph.Checked;
        Result.DownloadColor    = _downColor.BackColor;
        Result.UploadColor      = _upColor.BackColor;
        Result.Offset           = (int)_offset.Value;
        Result.ShowTrayIcon     = _trayIcon.Checked;
        Result.HideOnFullscreen = _fullscreen.Checked;

        if (Preview != null) Preview(this, EventArgs.Empty);
    }

    /// <summary>Keeps the offset field in sync when the user drags the widget meanwhile.</summary>
    public void SyncOffset(int offset)
    {
        _loading = true;
        _offset.Value = Math.Min(_offset.Maximum, Math.Max(0, offset));
        Result.Offset = offset;
        _loading = false;
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    private static TableLayoutPanel AddGroup(TableLayoutPanel root, string title)
    {
        return AddGroup(root, title, 2);
    }

    private static TableLayoutPanel AddGroup(TableLayoutPanel root, string title, int columns)
    {
        var box = new GroupBox
        {
            Text         = title,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor       = AnchorStyles.Left | AnchorStyles.Right,
            Padding      = new Padding(10, 6, 10, 8),
            Margin       = new Padding(0, 0, 0, 8),
        };
        var table = new TableLayoutPanel
        {
            ColumnCount  = columns,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location     = new Point(10, 20),
        };
        for (int i = 0; i < columns; i++)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        box.Controls.Add(table);
        root.Controls.Add(box);
        return table;
    }

    private static Label AddLabel(TableLayoutPanel t, string text)
    {
        var label = new Label
        {
            Text     = text,
            AutoSize = true,
            Anchor   = AnchorStyles.Left,
            Margin   = new Padding(3, 6, 12, 3),
        };
        t.Controls.Add(label);
        return label;
    }

    private static ComboBox AddCombo(TableLayoutPanel t, string label, params string[] items)
    {
        AddLabel(t, label);
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
        combo.Items.AddRange(items);
        t.Controls.Add(combo);
        return combo;
    }

    private static NumericUpDown AddNumeric(TableLayoutPanel t, string label, decimal min, decimal max,
        decimal step, int decimals)
    {
        AddLabel(t, label);
        var num = new NumericUpDown
        {
            Minimum       = min,
            Maximum       = max,
            Increment     = step,
            DecimalPlaces = decimals,
            Width         = 80,
        };
        t.Controls.Add(num);
        return num;
    }

    private static CheckBox AddCheck(TableLayoutPanel t, string text)
    {
        var check = new CheckBox { Text = text, AutoSize = true, Margin = new Padding(3, 5, 3, 3) };
        t.Controls.Add(check);
        if (t.ColumnCount > 1) t.SetColumnSpan(check, t.ColumnCount);
        return check;
    }

    private Button AddColorButton(TableLayoutPanel t, string label)
    {
        AddLabel(t, label);
        var button = new Button
        {
            Width     = 80,
            Height    = 24,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
        };
        button.FlatAppearance.BorderColor = SystemColors.ControlDark;
        button.Click += delegate
        {
            using (var dlg = new ColorDialog { Color = button.BackColor, FullOpen = true })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                button.BackColor = dlg.Color;
                OnChanged(button, EventArgs.Empty);
            }
        };
        t.Controls.Add(button);
        return button;
    }
}
