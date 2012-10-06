using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using Brnkly.Framework.Web;

namespace Brnkly.Framework.Logging
{
    public class LogBuffer
    {
        [ThreadStatic]
        private static LogBuffer currentBuffer;
        private Collection<BufferedLogEntry> entries;

        public LogPriority? LogPriority { get; set; }
        public TraceEventType? Severity { get; set; }

        public static LogBuffer Current
        {
            get
            {
                LogBuffer logBuffer;
                if (TryGetFromHttpContext(out logBuffer))
                {
                    return logBuffer;
                }

                if (currentBuffer == null)
                {
                    currentBuffer = new LogBuffer();
                }

                return currentBuffer;
            }
        }

        public LogBuffer()
        {
            this.entries = new Collection<BufferedLogEntry>();
        }

        private static bool TryGetFromHttpContext(out LogBuffer logBuffer)
        {
            if (HttpContext.Current == null)
            {
                logBuffer = null;
                return false;
            }

            logBuffer = HttpContext.Current.GetOrCreateItem<LogBuffer>(
                typeof(LogBuffer).FullName);
            return true;
        }

        private TraceEventType GetMaximumSeverity()
        {
            // Higher severities have lower integer values.
            var severity = this.entries.Min(e => e.Severity);

            if (this.Severity.HasValue &&
                (int)this.Severity.Value < (int)severity)
            {
                severity = this.Severity.Value;
            }

            return severity;
        }

        private LogPriority GetMaximumPriority(LogPriority logPriority)
        {
            return this.LogPriority.HasValue ?
                (LogPriority)Math.Max((int)this.LogPriority.Value, (int)logPriority) :
                logPriority;
        }

        private string GetMessage()
        {
            var message = new StringBuilder();
            foreach (var entry in this.entries)
            {
                if (entry == null)
                {
                    continue;
                }

                message.Append(entry.Message);
                message.AppendLine();

                if (entry.Exception != null)
                {
                    message.Append(entry.Exception.ToString());
                    message.AppendLine();
                }
            }

            return message.ToString();
        }

        public void FlushToLog(LogPriority logPriority, params LogCategory[] categories)
        {
            this.FlushToLog(string.Empty, logPriority, categories);
        }

        public void FlushToLog(string title, LogPriority logPriority, params LogCategory[] categories)
        {
            if (!this.entries.Any())
            {
                return;
            }

            try
            {
                var severity = this.GetMaximumSeverity();
                logPriority = this.GetMaximumPriority(logPriority);

                if (Log.ShouldLog(severity, logPriority, categories) ||
                    this.entries.All(entry => entry.Exception == null))
                {
                    Log.Write(
                        title,
                        this.GetMessage(),
                        severity,
                        logPriority,
                        null,
                        categories);
                }
            }
            finally
            {
                this.entries.Clear();
            }
        }

        public void Critical(string message, params object[] args)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Critical,
                    Message = string.Format(CultureInfo.InvariantCulture, message, args)
                });
        }

        public void Critical(Exception exception)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Critical,
                    Exception = exception
                });
        }

        public void Error(string message, params object[] args)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Error,
                    Message = string.Format(CultureInfo.InvariantCulture, message, args)
                });
        }

        public void Error(Exception exception)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Error,
                    Exception = exception
                });
        }

        public void Warning(string message, params object[] args)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Warning,
                    Message = string.Format(CultureInfo.InvariantCulture, message, args)
                });
        }

        public void Warning(Exception exception)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Warning,
                    Exception = exception
                });
        }

        public void Information(string message, params object[] args)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Information,
                    Message = string.Format(CultureInfo.InvariantCulture, message, args)
                });
        }

        public void Information(Exception exception)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Information,
                    Exception = exception
                });
        }

        public void Verbose(string message, params object[] args)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Verbose,
                    Message = string.Format(CultureInfo.InvariantCulture, message, args)
                });
        }

        public void Verbose(Exception exception)
        {
            this.entries.Add(
                new BufferedLogEntry()
                {
                    Severity = TraceEventType.Verbose,
                    Exception = exception
                });
        }
    }
}
