using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer
{
    /// <summary>
    /// This class can help you to store settings for your plugin
    /// </summary>
    /// <remarks>
    /// This class must be XML serializable
    /// </remarks>
    public class Settings
    {
        public string LastUsedOrganizationWebappUrl { get; set; }
        public List<string> VisibleColumns { get; set; } = new List<string>();
        public List<CustomTriggerColumnSetting> CustomTriggerColumns { get; set; } = new List<CustomTriggerColumnSetting>();
    }

    public class CustomTriggerColumnSetting
    {
        public string HeaderText { get; set; }
        public string JsonPath { get; set; } // e.g. outputs/body/prioritycode
    }
}