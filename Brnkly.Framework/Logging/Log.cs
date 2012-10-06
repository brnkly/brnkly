using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Brnkly.Framework.Logging
{
    /// <summary>
    /// Static facade class used to log events to one or more trace listeners.
    /// </summary>
    /// <remarks>This class is intended to simplify and standardize how we write to the log.</remarks>
    public static class Log
    {
        internal static readonly SourceLevels DefaultLevel =
            PlatformApplication.Current.IsDebuggingEnabled ||
            PlatformApplication.Current.EnvironmentType == EnvironmentType.Development ?
                SourceLevels.All :
                SourceLevels.Error;

        private static LogImplementation LogImplementation = new LogImplementation(DefaultLevel);

        /// <summary>
        /// Logs a fatal error or application crash.  
        /// Use this method when the application cannot continue.
        /// </summary>
        public static void Critical(string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(null, null, message, TraceEventType.Critical, logPriority, null, categories);
        }

        /// <summary>
        /// Logs a fatal error or application crash.  
        /// Use this method when the application cannot continue.
        /// </summary>
        public static void Critical(Exception exception, string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(exception, null, message, TraceEventType.Critical, logPriority, null, categories);
        }

        /// <summary>
        /// Logs a recoverable error.  
        /// Use this method when the application is able to recover from the exception.
        /// </summary>
        public static void Error(string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(null, null, message, TraceEventType.Error, logPriority, null, categories);
        }

        /// <summary>
        /// Logs a recoverable error.    
        /// Use this method when the application is able to recover from the exception.
        /// </summary>
        public static void Error(Exception exception, string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(exception, null, message, TraceEventType.Error, logPriority, null, categories);
        }

        /// <summary>
        /// Logs a noncritical problem.  
        /// Use this method when a single request cannot continue.
        /// </summary>
        public static void Warning(string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(null, null, message, TraceEventType.Warning, logPriority, null, categories);
        }

        /// <summary>
        /// Logs a noncritical problem.  
        /// Use this method when a single request cannot continue.
        /// </summary>
        public static void Warning(Exception exception, string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(exception, null, message, TraceEventType.Warning, logPriority, null, categories);
        }

        /// <summary>
        /// Logs an informational message.
        /// Use this method to log normal, expected processing that might be helpful to operations.
        /// </summary>
        public static void Information(string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(null, null, message, TraceEventType.Information, logPriority, null, categories);
        }

        /// <summary>
        /// Logs an informational message.
        /// Use this method to log normal, expected processing that might be helpful to operations.
        /// </summary>
        public static void Information(Exception exception, string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(exception, null, message, TraceEventType.Information, logPriority, null, categories);
        }

        /// <summary>
        /// Logs verbose debugging information.
        /// Use this method to log detailed information to assist in debugging.
        /// </summary>
        public static void Verbose(string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(null, null, message, TraceEventType.Verbose, logPriority, null, categories);
        }

        /// <summary>
        /// Logs verbose debugging information.
        /// Use this method to log detailed information to assist in debugging.
        /// </summary>
        public static void Verbose(Exception exception, string message, LogPriority logPriority, params LogCategory[] categories)
        {
            Log.Write(exception, null, message, TraceEventType.Verbose, logPriority, null, categories);
        }

        /// <summary>
        /// Writes a log entry.
        /// </summary>
        public static void Write(string title, string message, TraceEventType severity, LogPriority logPriority, IDictionary<string, object> extendedProperties, params LogCategory[] categories)
        {
            Write(null, title, message, severity, logPriority, extendedProperties, categories);
        }

        /// <summary>
        /// Writes a log entry with an exception.
        /// </summary>
        public static void Write(Exception exception, string title, string message, TraceEventType severity, LogPriority logPriority, IDictionary<string, object> extendedProperties, params LogCategory[] categories)
        {
            LogImplementation.Write(exception, title, message, severity, logPriority, extendedProperties, categories);
        }

        internal static bool ShouldLog(TraceEventType severity, LogPriority priority, IEnumerable<LogCategory> categories)
        {
            return LogImplementation.ShouldLog(severity, priority, categories);
        }

        internal static void UpdateLoggingLevel(SourceLevels? level)
        {
            LogBuffer.Current.LogPriority = LogPriority.Application;

            var newLevel = level ?? DefaultLevel;
            LogImplementation = new LogImplementation(newLevel);

            LogBuffer.Current.Information("Logging level updated to '{0}'", newLevel);
        }
    }
}
