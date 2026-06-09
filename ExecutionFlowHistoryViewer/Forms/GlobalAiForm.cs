using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json;

namespace ExecutionFlowHistoryViewer.Forms
{
    public partial class GlobalAiForm : Form
    {
        private readonly List<FlowRun> _allRuns;
        private readonly IChatService _chatService;
        private List<ChatMessage> _chatHistory = new List<ChatMessage>();

        private TextBox _txtAiInput;
        private Button _btnAiSend;
        private RichTextBox _rtbAiChat;

        private static readonly Color HeaderBackColor = Color.FromArgb(45, 55, 72);
        private static readonly Color SucceededColor = Color.FromArgb(56, 161, 105);
        private static readonly Color RunningColor = Color.FromArgb(49, 130, 206);

        public GlobalAiForm(List<FlowRun> allRuns, IChatService chatService)
        {
            _allRuns = allRuns ?? new List<FlowRun>();
            _chatService = chatService;
            
            InitializeComponent();
            ThemeManager.Apply(this, ThemeManager.IsDarkMode);
            BuildUi();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(600, 700);
            this.Name = "GlobalAiForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Global AI Assistant ✨";
            this.ResumeLayout(false);
        }

        private void BuildUi()
        {
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10), BackColor = Color.White };
            
            _txtAiInput = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                ScrollBars = ScrollBars.Vertical
            };
            
            _btnAiSend = new Button
            {
                Text = "Send",
                Dock = DockStyle.Right,
                Width = 80,
                BackColor = HeaderBackColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10F),
                Cursor = Cursors.Hand
            };
            _btnAiSend.FlatAppearance.BorderSize = 0;
            _btnAiSend.Click += async (s, e) => await SendAiMessageAsync();

            pnlBottom.Controls.Add(_txtAiInput);
            pnlBottom.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 }); // spacing
            pnlBottom.Controls.Add(_btnAiSend);

            _rtbAiChat = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(247, 250, 252),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                Padding = new Padding(10)
            };

            this.Controls.Add(_rtbAiChat);
            this.Controls.Add(pnlBottom);

            AppendAiMessage("System", $"Hi! I'm your Global AI Assistant. I have loaded the basic information for {_allRuns.Count} flow runs. Ask me anything about them!");
        }

        private async Task SendAiMessageAsync()
        {
            var question = _txtAiInput.Text.Trim();
            if (string.IsNullOrEmpty(question)) return;

            _txtAiInput.Clear();
            AppendAiMessage("You", question);
            _chatHistory.Add(new ChatMessage { Role = "user", Content = question });

            _btnAiSend.Enabled = false;
            _txtAiInput.Enabled = false;
            AppendAiMessage("System", "Thinking...");

            try
            {
                // Summarize the runs to avoid exceeding token limits
                var summaryList = _allRuns.Select(r => new { 
                    r.FlowName, 
                    r.Status, 
                    r.Duration, 
                    r.StartDate, 
                    r.Id 
                }).ToList();

                var systemContext = "NOTE: You are the Global AI Assistant. You only have access to the high-level summary of all runs. You DO NOT have the specific error messages or action details. If the user asks for the error message of a failed run or how to fix it, you MUST tell them: 'I only have the high-level summary here. To see the specific error message and get fix suggestions, please double-click the failed run in the grid to open its Details window, and ask the AI Assistant there.'\n\nRuns Data: " + JsonConvert.SerializeObject(summaryList, Formatting.None);

                var answer = await _chatService.AskQuestionAsync(question, systemContext, _chatHistory);
                
                var lastIndex = _rtbAiChat.Text.LastIndexOf("System:\nThinking...");
                if (lastIndex >= 0)
                {
                    _rtbAiChat.Text = _rtbAiChat.Text.Substring(0, lastIndex);
                }

                AppendAiMessage("Gemini", answer);
                _chatHistory.Add(new ChatMessage { Role = "model", Content = answer });
            }
            catch (Exception ex)
            {
                AppendAiMessage("System", $"Error: {ex.Message}");
            }
            finally
            {
                _btnAiSend.Enabled = true;
                _txtAiInput.Enabled = true;
                _txtAiInput.Focus();
            }
        }

        private void AppendAiMessage(string sender, string message)
        {
            _rtbAiChat.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Bold);
            _rtbAiChat.SelectionColor = sender == "You" ? RunningColor : (sender == "Gemini" ? SucceededColor : Color.Gray);
            _rtbAiChat.AppendText($"{sender}:\n");

            _rtbAiChat.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Regular);
            _rtbAiChat.SelectionColor = Color.FromArgb(45, 55, 72);
            _rtbAiChat.AppendText($"{message}\n\n");
            
            _rtbAiChat.ScrollToCaret();
        }
    }
}
