using System;
using System.Windows.Forms;

namespace ExecutionFlowHistoryViewer.Forms
{
    public class ApiKeyForm : Form
    {
        private readonly Settings _settings;
        private TextBox tbKey;

        public ApiKeyForm(Settings settings)
        {
            _settings = settings;
            BuildUi();
            tbKey.Text = settings.GeminiApiKey ?? "";
        }

        private void BuildUi()
        {
            this.Text = "Enter Free Gemini API Key";
            this.Size = new System.Drawing.Size(520, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lbl = new Label
            {
                Text = "Get a free key at: https://aistudio.google.com/app/apikey\nPaste it below (1,500 free requests/day):",
                Location = new System.Drawing.Point(20, 15),
                Size = new System.Drawing.Size(460, 40)
            };

            tbKey = new TextBox
            {
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(460, 25),
                PasswordChar = '●'
            };

            var btnSave = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(280, 110),
                Size = new System.Drawing.Size(90, 30),
                BackColor = System.Drawing.Color.FromArgb(37, 99, 235),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Popup
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(380, 110),
                Size = new System.Drawing.Size(90, 30)
            };

            this.Controls.Add(lbl);
            this.Controls.Add(tbKey);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _settings.GeminiApiKey = tbKey.Text.Trim();
        }
    }
}