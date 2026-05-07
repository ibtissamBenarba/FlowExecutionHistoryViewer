using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Helpers
{
    public static class ThemeManager
    {
        // Power Automate Modern Colors
        public static readonly Color PrimaryBlue = Color.FromArgb(0, 102, 255); // #0066FF
        public static readonly Color PrimaryBlueHover = Color.FromArgb(0, 82, 204);
        public static readonly Color LightBackground = Color.FromArgb(243, 242, 241); // #F3F2F1
        public static readonly Color WhiteBackground = Color.White;
        public static readonly Color TextPrimary = Color.FromArgb(50, 49, 48);
        public static readonly Color TextSecondary = Color.FromArgb(96, 94, 92);
        public static readonly Color BorderLight = Color.FromArgb(237, 235, 233);
        public static readonly Color AlternatingRow = Color.FromArgb(250, 250, 250);

        public static readonly Font HeaderFont = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        public static readonly Font RegularFont = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

        public static void ApplyTheme(Control parent)
        {
            parent.BackColor = LightBackground;
            parent.ForeColor = TextPrimary;
            parent.Font = RegularFont;

            ApplyToControls(parent.Controls);
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Button btn) StyleButton(btn);
                else if (control is DataGridView dgv) StyleDataGridView(dgv);
                else if (control is ToolStrip ts) StyleToolStrip(ts);
                else if (control is SplitContainer sc) StyleSplitContainer(sc);
                else if (control is GroupBox gb) StyleGroupBox(gb);
                else if (control is CheckedListBox clb) StyleCheckedListBox(clb);
                else if (control is TextBox tb) StyleTextBox(tb);
                else if (control is ComboBox cb) StyleComboBox(cb);
                else if (control is Label lbl) StyleLabel(lbl);

                // Recursively apply to children
                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls);
                }
            }
        }

        public static void StyleButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = PrimaryBlue;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI Semibold", 9F);
            btn.Cursor = Cursors.Hand;
            btn.Padding = new Padding(5, 2, 5, 2);

            btn.MouseEnter += (s, e) => btn.BackColor = PrimaryBlueHover;
            btn.MouseLeave += (s, e) => btn.BackColor = PrimaryBlue;
        }

        public static void StyleSecondaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = PrimaryBlue;
            btn.BackColor = Color.White;
            btn.ForeColor = PrimaryBlue;
            btn.Font = new Font("Segoe UI Semibold", 9F);
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter += (s, e) => { btn.BackColor = PrimaryBlue; btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.White; btn.ForeColor = PrimaryBlue; };
        }

        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = WhiteBackground;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(237, 235, 233);
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = PrimaryBlue;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(4);
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.DefaultCellStyle.BackColor = WhiteBackground;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 240, 255); // Light Blue selection
            dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgv.DefaultCellStyle.Padding = new Padding(4);
            dgv.DefaultCellStyle.Font = RegularFont;
            dgv.RowTemplate.Height = 35;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = AlternatingRow;
        }

        public static void StyleToolStrip(ToolStrip ts)
        {
            ts.Renderer = new ModernToolStripRenderer();
            ts.BackColor = WhiteBackground;
            ts.GripStyle = ToolStripGripStyle.Hidden;
            ts.RenderMode = ToolStripRenderMode.Professional;
            ts.Font = RegularFont;
            ts.Padding = new Padding(5);
        }

        public static void StyleSplitContainer(SplitContainer sc)
        {
            sc.BorderStyle = BorderStyle.None;
            sc.BackColor = Color.FromArgb(225, 223, 221); // Border color for splitter
            sc.Panel1.BackColor = LightBackground;
            sc.Panel2.BackColor = LightBackground;
            sc.SplitterWidth = 4;
        }

        public static void StyleGroupBox(GroupBox gb)
        {
            gb.BackColor = LightBackground;
            gb.ForeColor = PrimaryBlue;
            gb.Font = HeaderFont;
            gb.FlatStyle = FlatStyle.Flat;
            
            // Apply padding to content inside groupbox if possible, handled by layout usually
            gb.Padding = new Padding(10, 5, 10, 10);
            
            // To make it pop more, we ensure children have the right font/color
            foreach (Control c in gb.Controls)
            {
                 if (!(c is Button) && !(c is DataGridView))
                 {
                      c.BackColor = gb.BackColor;
                      if (c is Label || c is CheckBox || c is RadioButton)
                      {
                           c.ForeColor = TextPrimary;
                           c.Font = RegularFont;
                      }
                 }
            }
        }

        public static void StyleCheckedListBox(CheckedListBox clb)
        {
            clb.BorderStyle = BorderStyle.None;
            clb.BackColor = WhiteBackground;
            clb.Font = RegularFont;
            clb.ItemHeight = 24;
        }

        public static void StyleTextBox(TextBox tb)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = RegularFont;
        }

        public static void StyleComboBox(ComboBox cb)
        {
            cb.FlatStyle = FlatStyle.Flat;
            cb.Font = RegularFont;
        }
        
        public static void StyleLabel(Label lbl)
        {
            lbl.ForeColor = TextPrimary;
            lbl.Font = RegularFont;
        }

        private class ModernToolStripRenderer : ToolStripProfessionalRenderer
        {
            public ModernToolStripRenderer() : base(new ModernColorTable()) { }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // Remove border
                e.Graphics.DrawLine(new Pen(Color.FromArgb(237, 235, 233)), 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }
        }

        private class ModernColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => WhiteBackground;
            public override Color ToolStripGradientMiddle => WhiteBackground;
            public override Color ToolStripGradientEnd => WhiteBackground;
            public override Color MenuStripGradientBegin => WhiteBackground;
            public override Color MenuStripGradientEnd => WhiteBackground;
            public override Color ToolStripBorder => Color.Transparent;
            public override Color ButtonSelectedHighlight => Color.FromArgb(237, 235, 233);
            public override Color ButtonSelectedBorder => Color.FromArgb(237, 235, 233);
            public override Color ButtonPressedHighlight => Color.FromArgb(225, 223, 221);
            public override Color ButtonPressedBorder => Color.FromArgb(225, 223, 221);
        }
    }
}
