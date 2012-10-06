using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.ExtraInformation;
using EntLib = Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;

namespace Brnkly.Framework.Logging
{
    internal class LogImplementation
    {
        private static string sourceName = PlatformApplication.Current.Name;
        private bool loggingEnabled;
        private LogWriter logWriter;
        private EntLib.LoggingSettings entLibSettings;

        public LogImplementation(SourceLevels level)
        {
            var factory = new LoggingConfigurationSourceFactory();
            var configurationSource = factory.Create(level);
            this.entLibSettings =
                configurationSource.GetSection(EntLib.LoggingSettings.SectionName)
                as EntLib.LoggingSettings;
            this.logWriter = new LogWriterFactory(configurationSource).Create();
            this.loggingEnabled = true;
        }

        internal bool ShouldLog(TraceEventType severity, LogPriority priority, IEnumerable<LogCategory> categories)
        {
            return
                ShouldLog(severity, "All") ||
                ShouldLog(severity, priority.ToString()) ||
                categories.Any(c => ShouldLog(severity, c.ToString()));
        }

        private bool ShouldLog(TraceEventType severity, LogPriority priority, IEnumerable<string> categories)
        {
            return
                ShouldLog(severity, priority.ToString()) ||
                categories.Any(c => ShouldLog(severity, c));
        }

        private bool ShouldLog(TraceEventType severity, string traceSourceName)
        {
            if (!this.loggingEnabled ||
                !this.logWriter.IsLoggingEnabled())
            {
                return false;
            }

            var traceSource = this.entLibSettings.TraceSources
                .Where(source => string.Equals(source.Name, traceSourceName, StringComparison.OrdinalIgnoreCase))
                .SingleOrDefault();

            traceSource = traceSource ?? this.entLibSettings.SpecialTraceSources.AllEventsTraceSource;

            if (traceSource == null)
            {
                return false;
            }

            return ((int)severity & (int)traceSource.DefaultLevel) != 0;
        }

        internal void Write(
            Exception exception,
            string title,
            string message,
            TraceEventType severity,
            LogPriority logPriority,
            IDictionary<string, object> extendedProperties,
            params LogCategory[] categories)
        {
            try
            {
                if (!this.loggingEnabled ||
                    !this.logWriter.IsLoggingEnabled())
                {
                    return;
                }

                bool isException = exception != null;

                LogEntry logEntry = new LogEntry()
                {
                    Title = title ?? string.Empty,
                    Message = message,
                    Severity = severity,
                    Priority = (int)logPriority,
                    ExtendedProperties = extendedProperties,
                    Categories = GetCategoryStringArray(logPriority, categories, isException)
                };

                if (!ShouldLog(logEntry.Severity, logPriority, logEntry.Categories))
                {
                    return;
                }

                logEntry.ExtendedProperties.Add("Source", sourceName);
                new ManagedSecurityContextInformationProvider().PopulateDictionary(logEntry.ExtendedProperties);
                new HttpContextInformationProvider().PopulateDictionary(logEntry.ExtendedProperties);

                if (isException)
                {
                    AddExceptionInfoToLogEntry(exception, logEntry);
                }

                logWriter.Write(logEntry);
            }
            catch (Exception ex)
            {
                LogErrorDirectlyInEventLog(ex.ToString());
            }
        }

        internal static string[] GetCategoryStringArray(LogPriority logPriority, LogCategory[] categories, bool addException)
        {
            if (categories == null)
            {
                categories = new LogCategory[] { };
            }
            string[] categoryValues = (from category in categories select category.Value).ToArray();
            string[] allCategories = null;

            if (addException)
            {
                allCategories = new string[categories.Length + 2];
                allCategories[0] = logPriority.ToString();
                allCategories[1] = LogCategory.Exception.ToString();
                categoryValues.CopyTo(allCategories, 2);
            }
            else
            {
                allCategories = new string[categories.Length + 1];
                allCategories[0] = logPriority.ToString();
                categoryValues.CopyTo(allCategories, 1);
            }

            return allCategories;
        }

        private static void AddExceptionInfoToLogEntry(Exception exception, LogEntry logEntry)
        {
            string errorMessage = exception.Message;

            Exception innerException = exception.InnerException;
            while (innerException != null)
            {
                string innerMessage = innerException.Message;
                if (!string.IsNullOrEmpty(innerMessage))
                {
                    //it is ok if we are not using a stringBuilder for string concat
                    //within the loop since this loop will be rarely very deep
                    errorMessage = errorMessage + "\n\n" + innerMessage;
                }

                innerException = innerException.InnerException;
            }

            if (string.IsNullOrEmpty(logEntry.Message))
            {
                logEntry.Message = exception.ToString();
            }
            else
            {
                logEntry.Message = logEntry.Message + "\n\n" + exception.ToString();
                errorMessage = logEntry.Message + "\n\n" + errorMessage;
            }

            //add just error messages without stack trace information here.
            logEntry.ExtendedProperties.Add("Message", errorMessage);
        }

        private static void LogErrorDirectlyInEventLog(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(sourceName))
                {
                    EventLog.WriteEntry(sourceName, message, EventLogEntryType.Error);
                }
                else
                {
                    EventLog.WriteEntry("Brnkly.Platform", message, EventLogEntryType.Error);
                }
            }
            catch
            {
            }
        }
    }
}
