using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer
{
    public static class ThemeManager
    {
        private static void EnableDoubleBuffer(Control control)
        {
            typeof(Control)
                .GetProperty(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }
        private class DrawingSuspender : IDisposable
        {
            private const int WM_SETREDRAW = 11;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern IntPtr SendMessage(
                IntPtr hWnd,
                int msg,
                bool wParam,
                IntPtr lParam);

            private readonly Control _control;

            public DrawingSuspender(Control control)
            {
                _control = control;

                SendMessage(control.Handle, WM_SETREDRAW, false, IntPtr.Zero);
            }

            public void Dispose()
            {
                SendMessage(_control.Handle, WM_SETREDRAW, true, IntPtr.Zero);

                _control.Invalidate(true);
                _control.Update();
            }
        }
        public static bool IsDarkMode { get; private set; }

        // ─────────────────────────────────────────────
        // Palette
        // ─────────────────────────────────────────────
        public static Color Background => IsDarkMode
            ? Color.FromArgb(30, 30, 30)
            : SystemColors.Control;

        public static Color Surface => IsDarkMode
            ? Color.FromArgb(37, 37, 38)
            : Color.White;

        public static Color Text => IsDarkMode
            ? Color.FromArgb(204, 204, 204)
            : SystemColors.ControlText;

        public static Color Accent => Color.FromArgb(37, 99, 235);

        public static Color Border => IsDarkMode
            ? Color.FromArgb(62, 62, 64)
            : Color.LightGray;

        public static Color GridHeader => IsDarkMode
            ? Color.FromArgb(51, 51, 52)
            : Color.LightGray;

        public static Color GridBg => IsDarkMode
            ? Color.FromArgb(30, 30, 30)
            : Color.White;

        public static Color GridAlt => IsDarkMode
            ? Color.FromArgb(37, 37, 38)
            : Color.WhiteSmoke;

        // ─────────────────────────────────────────────
        // Internal State
        // ─────────────────────────────────────────────
        private static readonly Dictionary<Control, Snapshot> _snapshots =
            new Dictionary<Control, Snapshot>();

        private static readonly HashSet<GroupBox> _paintedGroupBoxes =
            new HashSet<GroupBox>();

        // ─────────────────────────────────────────────
        // Snapshot Classes
        // ─────────────────────────────────────────────
        private class Snapshot
        {
            public Color BackColor;
            public Color ForeColor;

            public BorderStyle BorderStyle;

            public FlatStyle FlatStyle;
            public Color FlatBorderColor;
            public int FlatBorderSize;

            public bool EnableHeadersVisualStyles;

            // DataGridView
            public DataGridViewCellStyle DefaultCellStyle;
            public DataGridViewCellStyle AltRowsCellStyle;
            public DataGridViewCellStyle ColumnHeadersCellStyle;
            public DataGridViewCellStyle RowHeadersCellStyle;

            public Color GridBackgroundColor;

            // ToolStrip
            public ToolStripRenderer Renderer;
            public Dictionary<ToolStripItem, ItemSnapshot> ToolStripItems;
        }

        private class ItemSnapshot
        {
            public Color BackColor;
            public Color ForeColor;
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────
        public static void Apply(Control parent, bool darkMode)
        {
            if (parent == null || parent.IsDisposed)
                return;

            SnapshotTree(parent);

            IsDarkMode = darkMode;

            using (new DrawingSuspender(parent))
            {
                parent.SuspendLayout();

                ApplyRecursive(parent);

                parent.ResumeLayout(true);
            }
        }

        // ─────────────────────────────────────────────
        // Snapshot Tree
        // ─────────────────────────────────────────────
        private static void SnapshotTree(Control c)
        {
            if (!_snapshots.ContainsKey(c))
                _snapshots[c] = TakeSnapshot(c);

            foreach (Control child in c.Controls)
                SnapshotTree(child);
        }

        // ─────────────────────────────────────────────
        // Apply Recursively
        // ─────────────────────────────────────────────
        private static void ApplyRecursive(Control c)
        {
            EnableDoubleBuffer(c);

            if (IsDarkMode)
                ApplyDarkTheme(c);
            else
                Restore(c);

            foreach (Control child in c.Controls.Cast<Control>().ToArray())
                ApplyRecursive(child);
        }

        // ─────────────────────────────────────────────
        // DARK MODE
        // ─────────────────────────────────────────────
        private static void ApplyDarkTheme(Control c)
        {
            switch (c)
            {
                case Form form:
                    form.BackColor = Background;
                    form.ForeColor = Text;
                    break;

                case UserControl uc:
                    uc.BackColor = Background;
                    uc.ForeColor = Text;
                    break;

                case Panel panel:
                    panel.BackColor = Background;
                    break;

                case SplitContainer split:
                    split.BackColor = Border;
                    break;

                case GroupBox gb:
                    ApplyDarkGroupBox(gb);
                    break;

                case Label lbl:
                    lbl.BackColor = Color.Transparent;
                    lbl.ForeColor = Text;
                    break;

                case Button btn:
                    ApplyDarkButton(btn);
                    break;

                case TextBox tb:
                    tb.BackColor = Surface;
                    tb.ForeColor = Text;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case RichTextBox rtb:
                    rtb.BackColor = Surface;
                    rtb.ForeColor = Text;
                    rtb.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox cb when !(cb.Parent is ToolStrip):
                    cb.BackColor = Surface;
                    cb.ForeColor = Text;
                    cb.FlatStyle = FlatStyle.Flat;
                    break;

                case CheckBox chk:
                    chk.BackColor = Color.Transparent;
                    chk.ForeColor = Text;
                    break;

                case RadioButton rad:
                    rad.BackColor = Color.Transparent;
                    rad.ForeColor = Text;
                    break;

                case CheckedListBox clb:
                    clb.BackColor = Surface;
                    clb.ForeColor = Text;
                    clb.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ListBox lb:
                    lb.BackColor = Surface;
                    lb.ForeColor = Text;
                    break;

                case TreeView tv:
                    tv.BackColor = Surface;
                    tv.ForeColor = Text;
                    tv.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ListView lv:
                    lv.BackColor = Surface;
                    lv.ForeColor = Text;
                    break;

                case DateTimePicker dtp:
                    dtp.BackColor = Surface;
                    dtp.ForeColor = Text;
                    break;

                case ProgressBar pb:
                    pb.BackColor = Surface;
                    break;

                case TabControl tc:
                    tc.BackColor = Background;
                    tc.ForeColor = Text;

                    foreach (TabPage page in tc.TabPages)
                    {
                        page.BackColor = Background;
                        page.ForeColor = Text;
                    }
                    break;

                case DataGridView dgv:
                    ThemeDataGridView(dgv);
                    break;

                case ToolStrip strip:
                    ThemeToolStrip(strip);
                    break;

                case PictureBox pb:
                    pb.BackColor = Color.Transparent;   // let the dark GroupBox show through
                    break;

            }

        }

        // ─────────────────────────────────────────────
        // Restore Original State
        // ─────────────────────────────────────────────
        private static void Restore(Control c)
        {
            if (!_snapshots.TryGetValue(c, out var snap))
                return;

            switch (c)
            {
                case DataGridView grid:
                    RestoreDataGridView(grid, snap);
                    break;

                case ToolStrip strip:
                    RestoreToolStrip(strip, snap);
                    break;

                case Button btn:
                    btn.BackColor = snap.BackColor;
                    btn.ForeColor = snap.ForeColor;
                    btn.FlatStyle = snap.FlatStyle;
                    btn.FlatAppearance.BorderColor = snap.FlatBorderColor;
                    btn.FlatAppearance.BorderSize = snap.FlatBorderSize;
                    break;

                case TextBox tb:
                    tb.BackColor = snap.BackColor;
                    tb.ForeColor = snap.ForeColor;
                    tb.BorderStyle = snap.BorderStyle;
                    break;

                case RichTextBox rtb:
                    rtb.BackColor = snap.BackColor;
                    rtb.ForeColor = snap.ForeColor;
                    rtb.BorderStyle = snap.BorderStyle;
                    break;

                case ComboBox cb when !(cb.Parent is ToolStrip):
                    cb.BackColor = snap.BackColor;
                    cb.ForeColor = snap.ForeColor;
                    cb.FlatStyle = snap.FlatStyle;
                    break;

                case CheckedListBox clb:
                    clb.BackColor = snap.BackColor;
                    clb.ForeColor = snap.ForeColor;
                    clb.BorderStyle = snap.BorderStyle;
                    break;

                case GroupBox gb:
                    RestoreGroupBox(gb, snap);
                    break;

                case PictureBox pb:
                    pb.BackColor = snap.BackColor;
                    break;

                default:
                    c.BackColor = snap.BackColor;
                    c.ForeColor = snap.ForeColor;
                    break;
            }

        }

        // ─────────────────────────────────────────────
        // Snapshot Logic
        // ─────────────────────────────────────────────
        private static Snapshot TakeSnapshot(Control c)
        {
            var snap = new Snapshot
            {
                BackColor = c.BackColor,
                ForeColor = c.ForeColor
            };

            if (c is TextBoxBase tb)
                snap.BorderStyle = tb.BorderStyle;
            else if (c is DataGridView dgv)
                snap.BorderStyle = dgv.BorderStyle;
            else if (c is CheckedListBox clb)
                snap.BorderStyle = clb.BorderStyle;

            if (c is Button btn)
            {
                snap.FlatStyle = btn.FlatStyle;
                snap.FlatBorderColor = btn.FlatAppearance.BorderColor;
                snap.FlatBorderSize = btn.FlatAppearance.BorderSize;
            }
            else if (c is ComboBox cb)
            {
                snap.FlatStyle = cb.FlatStyle;
            }

            if (c is DataGridView grid)
            {
                snap.EnableHeadersVisualStyles = grid.EnableHeadersVisualStyles;
                snap.GridBackgroundColor = grid.BackgroundColor;

                snap.DefaultCellStyle = CloneStyle(grid.DefaultCellStyle);
                snap.AltRowsCellStyle = CloneStyle(grid.AlternatingRowsDefaultCellStyle);
                snap.ColumnHeadersCellStyle = CloneStyle(grid.ColumnHeadersDefaultCellStyle);
                snap.RowHeadersCellStyle = CloneStyle(grid.RowHeadersDefaultCellStyle);
            }

            if (c is ToolStrip strip)
            {
                snap.Renderer = strip.Renderer;
                snap.ToolStripItems = new Dictionary<ToolStripItem, ItemSnapshot>();

                foreach (ToolStripItem item in strip.Items)
                {
                    snap.ToolStripItems[item] = new ItemSnapshot
                    {
                        BackColor = item.BackColor,
                        ForeColor = item.ForeColor
                    };
                }
            }

            return snap;
        }

        // ─────────────────────────────────────────────
        // DataGridView
        // ─────────────────────────────────────────────
        private static void ThemeDataGridView(DataGridView grid)
        {
            if (!_snapshots.TryGetValue(grid, out var snap))
                return;

            grid.BackgroundColor = GridBg;
            grid.GridColor = Border;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GridBg,
                ForeColor = Text,
                SelectionBackColor = Accent,
                SelectionForeColor = Color.White,
                Font = snap.DefaultCellStyle?.Font,
                Alignment = snap.DefaultCellStyle?.Alignment ??
                            DataGridViewContentAlignment.MiddleLeft,
                WrapMode = snap.DefaultCellStyle?.WrapMode ??
                           DataGridViewTriState.False
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GridAlt,
                ForeColor = Text,
                Font = snap.AltRowsCellStyle?.Font,
                Alignment = snap.AltRowsCellStyle?.Alignment ??
                            DataGridViewContentAlignment.MiddleLeft
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GridHeader,
                ForeColor = Text,
                SelectionBackColor = GridHeader,
                SelectionForeColor = Text,
                Font = snap.ColumnHeadersCellStyle?.Font,
                Alignment = snap.ColumnHeadersCellStyle?.Alignment ??
                            DataGridViewContentAlignment.MiddleLeft,
                WrapMode = snap.ColumnHeadersCellStyle?.WrapMode ??
                           DataGridViewTriState.True
            };

            grid.RowHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GridHeader,
                ForeColor = Text,
                Font = snap.RowHeadersCellStyle?.Font
            };

        }

        private static void RestoreDataGridView(DataGridView grid, Snapshot snap)
        {
            grid.BackgroundColor = snap.GridBackgroundColor;
            grid.GridColor = SystemColors.ControlDark;
            grid.BorderStyle = snap.BorderStyle;
            grid.EnableHeadersVisualStyles = snap.EnableHeadersVisualStyles;

            grid.DefaultCellStyle = CloneStyle(snap.DefaultCellStyle);
            grid.AlternatingRowsDefaultCellStyle = CloneStyle(snap.AltRowsCellStyle);
            grid.ColumnHeadersDefaultCellStyle = CloneStyle(snap.ColumnHeadersCellStyle);
            grid.RowHeadersDefaultCellStyle = CloneStyle(snap.RowHeadersCellStyle);

        }

        // ─────────────────────────────────────────────
        // ToolStrip
        // ─────────────────────────────────────────────
        private static void ThemeToolStrip(ToolStrip strip)
        {
            strip.BackColor = Surface;
            strip.ForeColor = Text;
            strip.Renderer = new DarkToolStripRenderer();

            foreach (ToolStripItem item in strip.Items)
            {
                item.BackColor = Surface;
                item.ForeColor = Text;
            }
        }

        private static void RestoreToolStrip(ToolStrip strip, Snapshot snap)
        {
            strip.BackColor = snap.BackColor;
            strip.ForeColor = snap.ForeColor;

            strip.Renderer = snap.Renderer ??
                             new ToolStripProfessionalRenderer();

            if (snap.ToolStripItems != null)
            {
                foreach (ToolStripItem item in strip.Items)
                {
                    if (snap.ToolStripItems.TryGetValue(item, out var itemSnap))
                    {
                        item.BackColor = itemSnap.BackColor;
                        item.ForeColor = itemSnap.ForeColor;
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        // Button
        // ─────────────────────────────────────────────
        private static void ApplyDarkButton(Button btn)
        {
            btn.BackColor = Color.FromArgb(0, 90, 158);
            btn.ForeColor = Color.White;

            btn.FlatStyle = FlatStyle.Flat;

            btn.FlatAppearance.BorderColor = Border;
            btn.FlatAppearance.BorderSize = 1;
        }

        // ─────────────────────────────────────────────
        // GroupBox
        // ─────────────────────────────────────────────
        private static void ApplyDarkGroupBox(GroupBox gb)
        {
            gb.BackColor = Background;
            gb.ForeColor = Text;

            if (!_paintedGroupBoxes.Contains(gb))
            {
                gb.Paint += GroupBox_OnPaint;
                _paintedGroupBoxes.Add(gb);
            }
        }

        private static void RestoreGroupBox(GroupBox gb, Snapshot snap)
        {
            gb.BackColor = snap.BackColor;
            gb.ForeColor = snap.ForeColor;

            if (_paintedGroupBoxes.Contains(gb))
            {
                gb.Paint -= GroupBox_OnPaint;
                _paintedGroupBoxes.Remove(gb);
            }
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────
        private static DataGridViewCellStyle CloneStyle(DataGridViewCellStyle style)
        {
            if (style == null)
                return null;

            return new DataGridViewCellStyle(style);
        }

        // ─────────────────────────────────────────────
        // GroupBox Paint
        // ─────────────────────────────────────────────
        private static void GroupBox_OnPaint(object sender, PaintEventArgs e)
        {
            if (!(sender is GroupBox gb))
                return;

            e.Graphics.Clear(gb.BackColor);

            using (var borderPen = new Pen(Border))
            {
                var textSize = TextRenderer.MeasureText(
                    e.Graphics,
                    gb.Text,
                    gb.Font);

                int textWidth = textSize.Width + 16;
                int top = textSize.Height / 2;

                TextRenderer.DrawText(
                    e.Graphics,
                    gb.Text,
                    gb.Font,
                    new Point(8, 0),
                    gb.ForeColor);

                e.Graphics.DrawLine(borderPen, textWidth, top, gb.Width - 2, top);
                e.Graphics.DrawLine(borderPen, 0, top, 8, top);
                e.Graphics.DrawLine(borderPen, 0, top, 0, gb.Height - 2);
                e.Graphics.DrawLine(borderPen, 0, gb.Height - 2, gb.Width - 2, gb.Height - 2);
                e.Graphics.DrawLine(borderPen, gb.Width - 2, top, gb.Width - 2, gb.Height - 2);
            }
        }

        // ─────────────────────────────────────────────
        // ToolStrip Renderer
        // ─────────────────────────────────────────────
        private class DarkToolStripRenderer : ToolStripProfessionalRenderer
        {
            public DarkToolStripRenderer()
                : base(new DarkColorTable())
            {
            }

            protected override void OnRenderItemText(
                ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = Text;
                base.OnRenderItemText(e);
            }
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripBorder => Border;

            public override Color ToolStripContentPanelGradientBegin => Surface;
            public override Color ToolStripContentPanelGradientEnd => Surface;

            public override Color ToolStripPanelGradientBegin => Surface;
            public override Color ToolStripPanelGradientEnd => Surface;

            public override Color ToolStripGradientBegin => Surface;
            public override Color ToolStripGradientMiddle => Surface;
            public override Color ToolStripGradientEnd => Surface;

            public override Color ButtonSelectedBorder => Accent;

            public override Color ButtonSelectedHighlight =>
                Color.FromArgb(62, 62, 64);

            public override Color ButtonSelectedHighlightBorder => Accent;

            public override Color ButtonPressedBorder => Accent;

            public override Color ButtonPressedHighlight =>
                Color.FromArgb(62, 62, 64);

            public override Color ButtonPressedHighlightBorder => Accent;

            public override Color CheckBackground => Surface;

            public override Color CheckPressedBackground => Accent;

            public override Color CheckSelectedBackground => Accent;

            public override Color GripDark => Border;

            public override Color GripLight => Border;

            public override Color MenuBorder => Border;

            public override Color MenuItemBorder => Accent;

            public override Color MenuItemSelected =>
                Color.FromArgb(62, 62, 64);

            public override Color MenuItemSelectedGradientBegin =>
                Color.FromArgb(62, 62, 64);

            public override Color MenuItemSelectedGradientEnd =>
                Color.FromArgb(62, 62, 64);

            public override Color SeparatorDark => Border;

            public override Color SeparatorLight => Border;
        }
    }


}

