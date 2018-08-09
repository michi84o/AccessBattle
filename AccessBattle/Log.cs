using System;
using System.Diagnostics;

namespace AccessBattle
{
    /// <summary>
    /// Logging mode used by the Log class.
    /// </summary>
    public enum LogMode
    {
        /// <summary>Use console debug output.</summary>
        Debug,
        /// <summary>Use console trace output.</summary>
        Trace,
        /// <summary>Use console output.</summary>
        Console,
        /// <summary>Use file output (NOT IMPLEMENTED).</summary>
        File
    }

    /// <summary>Priority for logs.</summary>
    public enum LogPriority
    {
        /// <summary>Print all messages.</summary>
        Debug = 0,
        /// <summary>Print all messages exceot debug</summary>
        Verbose,
        /// <summary>No verbose or debug output.</summary>
        Information,
        /// <summary>Only warnings and errors.</summary>
        Warning,
        /// <summary>Only errors.</summary>
        Error,
        /// <summary>Only critical errors.</summary>
        Critical
    }

    // TODO: Implement mode file, use TraceSource class
    /// <summary>
    /// Helper class for logging error and debug messages.
    /// </summary>
    public static class Log
    {
        private static LogMode Mode = LogMode.Debug;

        /// <summary>Log priority.</summary>
        public static LogPriority Priority = LogPriority.Verbose;

        /// <summary>
        /// Set logging mode.
        /// </summary>
        /// <param name="mode">Mode to use.</param>
        /// <param name="filename">Filename. Only required when file mode is used (NOT IMPLEMENTED).</param>
        public static void SetMode(LogMode mode, string filename = null)
        {
            Mode = mode;
        }

        /// <summary>
        /// Adds a new line to the log.
        /// </summary>
        /// <param name="prio">Log priority.</param>
        public static void WriteLine(LogPriority prio)
        {
            if (prio < Priority) return;
            switch (Mode)
            {
                case LogMode.Console: Console.WriteLine(); break;
                case LogMode.Debug: Debug.WriteLine(""); break;
                default:
                    Trace.WriteLine(""); break;
            }
        }

        /// <summary>
        /// Writes a line to the log.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="prio">Log priority.</param>
        public static void WriteLine(LogPriority prio, string message)
        {
            if (prio < Priority) return;
            switch (Mode)
            {
                case LogMode.Console: Console.WriteLine(message); break;
                case LogMode.Debug: Debug.WriteLine(message); break;
                default:
                    Trace.WriteLine(message); break;
            }
        }

        /// <summary>
        /// Writes a line to the log.
        /// </summary>
        /// <param name="format">Format string with message to write.</param>
        /// <param name="args">Values for format string.</param>
        /// <param name="prio">Log priority.</param>
        public static void WriteLine(LogPriority prio, string format, params object[] args)
        {
            if (prio < Priority) return;
            switch (Mode)
            {
                case LogMode.Console: Console.WriteLine(format, args); break;
                case LogMode.Debug: Debug.WriteLine(format, args); break;
                default:
                    Trace.WriteLine(string.Format(format, args)); break;
            }
        }

        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="prio">Log priority.</param>
        public static void Write(LogPriority prio, string message)
        {
            if (prio < Priority) return;
            switch (Mode)
            {
                case LogMode.Console: Console.Write(message); break;
                case LogMode.Debug: Debug.Write(message); break;
                default:
                    Trace.Write(message); break;
            }
        }

        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        /// <param name="format">Format string with message to write.</param>
        /// <param name="args">Values for format string.</param>
        /// <param name="prio">Log priority.</param>
        public static void Write(LogPriority prio, string format, params object[] args)
        {
            if (prio < Priority) return;
            switch (Mode)
            {
                case LogMode.Console: Console.Write(format, args); break;
                case LogMode.Debug: Debug.Write(string.Format(format, args)); break;
                default:
                    Trace.Write(string.Format(format, args)); break;
            }
        }
    }
}
