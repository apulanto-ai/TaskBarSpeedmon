using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

class SettingsForm : Form
{
    // ── Palette ──────────────────────────────────────────────────────────────
    private static readonly Color BgDark    = Color.FromArgb(45, 45, 48);
    private static readonly Color BgMid     = Color.FromArgb(62, 62, 66);
    private static readonly Color BgLight   = Color.FromArgb(80, 80, 84);
    private static readonly Color FgNormal  = Color.FromArgb(220, 220, 220);
    private static readonly Color FgAccent  = Color.FromArgb(99, 168, 255);
    private static readonly Color FgDim     = Color.FromArgb(130, 130, 134);
    private static readonly Color BtnSave   = Color.FromArgb(0, 122, 204);

    private static readonly int[] IconSizes = { 16, 24, 32, 48, 64 };

    // ── Fields ────────────────────────────────────────────────────────────────
    private AppSettings _settings;

    private Panel         _dlColorPanel;
    private Panel         _ulColorPanel;
    private Panel         _bgColorPanel;
    private NumericUpDown _fontSizeInput;
    private ComboBox      _iconSizeCombo;
    private PictureBox    _previewBox;

    public AppSettings Result { get { return _settings; } }

    // ── Constructor ───────────────────────────────────────────────────────────
    public SettingsForm(AppSettings current)
    {
        _settings = current.Clone();
        BuildUI();
        UpdatePreview();
    }

    // ── Layout ────────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        Text             = "Einstellungen – TaskBarSpeedmon";
        ClientSize       = new Size(340, 452);
        FormBorderStyle  = FormBorderStyle.FixedDialog;
        MaximizeBox      = false;
        MinimizeBox      = false;
        StartPosition    = FormStartPosition.CenterScreen;
        BackColor        = BgDark;
        ForeColor        = FgNormal;
        Font             = new Font("Segoe UI", 9f);

        int y = 10;

        // ── Farben ──────────────────────────────────────────────────────────
        SectionLabel("Farben", ref y);
        _dlColorPanel = ColorRow("Download-Farbe:",  _settings.DownloadColor,   ref y);
        _ulColorPanel = ColorRow("Upload-Farbe:",    _settings.UploadColor,     ref y);
        _bgColorPanel = ColorRow("Hintergrund:",     _settings.BackgroundColor, ref y);
        Separator(ref y);

        // ── Schrift & Icon ───────────────────────────────────────────────────
        SectionLabel("Schrift & Icon", ref y);

        RowLabel("Schriftgröße:", y);
        _fontSizeInput = new NumericUpDown
        {
            Location      = new Point(170, y),
            Size          = new Size(62, 24),
            Minimum       = 5m,
            Maximum       = 20m,
            DecimalPlaces = 1,
            Increment     = 0.5m,
            Value         = (decimal)_settings.FontSize,
            BackColor     = BgMid,
            ForeColor     = FgNormal
        };
        _fontSizeInput.ValueChanged += OnSettingChanged;
        Controls.Add(_fontSizeInput);
        DimLabel("pt", 237, y + 5);
        y += 32;

        RowLabel("Icon-Größe:", y);
        _iconSizeCombo = new ComboBox
        {
            Location      = new Point(170, y),
            Size          = new Size(100, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor     = BgMid,
            ForeColor     = FgNormal
        };
        _iconSizeCombo.Items.AddRange(new object[] { "16 × 16", "24 × 24", "32 × 32", "48 × 48", "64 × 64" });
        int selIdx = 2;
        for (int i = 0; i < IconSizes.Length; i++)
            if (IconSizes[i] == _settings.IconSize) { selIdx = i; break; }
        _iconSizeCombo.SelectedIndex = selIdx;
        _iconSizeCombo.SelectedIndexChanged += OnSettingChanged;
        Controls.Add(_iconSizeCombo);
        y += 32;

        Separator(ref y);

        // ── Vorschau ─────────────────────────────────────────────────────────
        SectionLabel("Vorschau", ref y);

        // Bordered container
        var frame = new Panel
        {
            Location  = new Point((ClientSize.Width - 100) / 2, y),
            Size      = new Size(100, 100),
            BackColor = BgLight
        };
        Controls.Add(frame);

        _previewBox = new PictureBox
        {
            Location  = new Point(2, 2),
            Size      = new Size(96, 96),
            BackColor = _settings.BackgroundColor,
            SizeMode  = PictureBoxSizeMode.Normal
        };
        frame.Controls.Add(_previewBox);
        y += 108;

        DimLabel("Beispiel bei 1.2 MB/s ↓  0.3 MB/s ↑", 0, y);
        // Center the hint label by making it full-width
        var hint = (Label)Controls[Controls.Count - 1];
        hint.Size      = new Size(ClientSize.Width, 18);
        hint.TextAlign = ContentAlignment.MiddleCenter;
        y += 22;

        Separator(ref y);

        // ── Buttons ──────────────────────────────────────────────────────────
        y += 4;
        var btnSave = new Button
        {
            Text      = "Speichern",
            Location  = new Point(ClientSize.Width - 222, y),
            Size      = new Size(104, 30),
            BackColor = BtnSave,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += OnSave;
        Controls.Add(btnSave);

        var btnCancel = new Button
        {
            Text      = "Abbrechen",
            Location  = new Point(ClientSize.Width - 112, y),
            Size      = new Size(104, 30),
            BackColor = BgMid,
            ForeColor = FgNormal,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderColor = BgLight;
        btnCancel.Click += OnCancel;
        Controls.Add(btnCancel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    // ── Layout helpers ────────────────────────────────────────────────────────
    private void SectionLabel(string text, ref int y)
    {
        var lbl = new Label
        {
            Text      = text,
            Location  = new Point(12, y),
            Size      = new Size(300, 20),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = FgAccent,
            BackColor = Color.Transparent
        };
        Controls.Add(lbl);
        y += 26;
    }

    private void Separator(ref int y)
    {
        y += 6;
        Controls.Add(new Panel
        {
            Location  = new Point(0, y),
            Size      = new Size(ClientSize.Width, 1),
            BackColor = BgLight
        });
        y += 9;
    }

    private void RowLabel(string text, int y)
    {
        Controls.Add(new Label
        {
            Text      = text,
            Location  = new Point(20, y + 5),
            AutoSize  = true,
            ForeColor = FgNormal,
            BackColor = Color.Transparent
        });
    }

    private void DimLabel(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            AutoSize  = true,
            ForeColor = FgDim,
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 8f)
        });
    }

    private Panel ColorRow(string labelText, Color color, ref int y)
    {
        RowLabel(labelText, y);

        var swatch = new Panel
        {
            Location    = new Point(170, y + 2),
            Size        = new Size(52, 22),
            BackColor   = color,
            BorderStyle = BorderStyle.FixedSingle,
            Cursor      = Cursors.Hand
        };
        swatch.Click += OnSwatchClick;
        Controls.Add(swatch);

        var btn = new Button
        {
            Text      = "Ändern",
            Location  = new Point(228, y),
            Size      = new Size(70, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = BgMid,
            ForeColor = FgNormal,
            Tag       = swatch
        };
        btn.FlatAppearance.BorderColor = BgLight;
        btn.Click += OnChangeColorClick;
        Controls.Add(btn);

        y += 32;
        return swatch;
    }

    // ── Color picking ─────────────────────────────────────────────────────────
    private void OnSwatchClick(object sender, EventArgs e)
    {
        PickColor((Panel)sender);
    }

    private void OnChangeColorClick(object sender, EventArgs e)
    {
        PickColor((Panel)((Button)sender).Tag);
    }

    private void PickColor(Panel swatch)
    {
        using (var dlg = new ColorDialog { Color = swatch.BackColor, FullOpen = true })
        {
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            swatch.BackColor = dlg.Color;

            if (swatch == _dlColorPanel)
                _settings.DownloadColor = dlg.Color;
            else if (swatch == _ulColorPanel)
                _settings.UploadColor = dlg.Color;
            else if (swatch == _bgColorPanel)
            {
                _settings.BackgroundColor = dlg.Color;
                _previewBox.BackColor = dlg.Color;
            }
            UpdatePreview();
        }
    }

    // ── Settings changed ──────────────────────────────────────────────────────
    private void OnSettingChanged(object sender, EventArgs e)
    {
        _settings.FontSize = (float)_fontSizeInput.Value;
        int idx = _iconSizeCombo.SelectedIndex;
        if (idx >= 0 && idx < IconSizes.Length)
            _settings.IconSize = IconSizes[idx];
        UpdatePreview();
    }

    // ── Preview rendering ─────────────────────────────────────────────────────
    private void UpdatePreview()
    {
        int sz = _settings.IconSize;

        // Render at native icon size
        var native = new Bitmap(sz, sz);
        using (var g = Graphics.FromImage(native))
        {
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            g.Clear(_settings.BackgroundColor);

            using (var font = new Font("Segoe UI", _settings.FontSize, FontStyle.Bold))
            using (var dlBr = new SolidBrush(_settings.DownloadColor))
            using (var ulBr = new SolidBrush(_settings.UploadColor))
            {
                string dlText = "↓" + "1.2M";
                string ulText = "↑" + "0.3M";

                SizeF dlSz = g.MeasureString(dlText, font);
                SizeF ulSz = g.MeasureString(ulText, font);

                g.DrawString(dlText, font, dlBr, Math.Max(0f, (sz - dlSz.Width) / 2f), 1f);
                g.DrawString(ulText, font, ulBr, Math.Max(0f, (sz - ulSz.Width) / 2f), sz / 2f + 1f);
            }
        }

        // Scale up to 96×96 for display (NearestNeighbor = pixel-accurate preview)
        var preview = new Bitmap(96, 96);
        using (var g = Graphics.FromImage(preview))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode   = PixelOffsetMode.Half;
            g.DrawImage(native, 0, 0, 96, 96);
        }
        native.Dispose();

        if (_previewBox.Image != null)
        {
            var old = _previewBox.Image;
            _previewBox.Image = null;
            old.Dispose();
        }
        _previewBox.Image = preview;
    }

    // ── Dialog buttons ────────────────────────────────────────────────────────
    private void OnSave(object sender, EventArgs e)
    {
        OnSettingChanged(null, EventArgs.Empty);
        _settings.Save();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancel(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _previewBox != null && _previewBox.Image != null)
            _previewBox.Image.Dispose();
        base.Dispose(disposing);
    }
}
