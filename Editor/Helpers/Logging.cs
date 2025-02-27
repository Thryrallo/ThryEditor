using System.Text;
using UnityEngine;

namespace Thry.ThryEditor.Helpers
{
    public enum LoggingLevel { None, Normal, Detailed, StackTraced }

    public class ThryDebug
    {
        private static string GetPrefixFromStackTrace()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame stackFrame = stackTrace.GetFrame(2);
            return stackFrame.GetMethod().DeclaringType.Name;
        }

        public static void Log(string message)
        {
            Log(GetPrefixFromStackTrace(), message);
        }

        public static void Log(string prefix, string message)
        {
            if (Config.Singleton.loggingLevel == LoggingLevel.None) return;
            Print(prefix, "#ff78e0", message);
        }

        public static void Detail(string message)
        {
            Detail(GetPrefixFromStackTrace(), message);
        }

        public static void Detail(string prefix, string message)
        {
            if ((int)Config.Singleton.loggingLevel < (int)LoggingLevel.Detailed) return;
            Print(prefix, "#d778ff", message);
        }

        private static void Print(string prefix, string color, string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[<color=");
            sb.Append(color);
            sb.Append(">");
            sb.Append(prefix);
            sb.Append("</color>] ");
            sb.Append(message);
            if (Config.Singleton.loggingLevel == LoggingLevel.StackTraced)
                sb.Append("\n" + new System.Diagnostics.StackTrace().ToString());
            Debug.Log(sb.ToString());
        }

    }
}