using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Contracts
{
    public interface IChatService
    {
        Task<string> AskQuestionAsync(string question, string systemContext, List<ChatMessage> history);
    }

    public class ChatMessage
    {
        public string Role { get; set; } // "user" or "model"
        public string Content { get; set; }
    }
}
