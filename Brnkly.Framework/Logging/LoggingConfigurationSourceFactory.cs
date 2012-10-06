using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using EntLib = Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;

namespace Brnkly.Framework.Logging
{
    internal class LoggingConfigurationSourceFactory
    {
        private const string ApplicationEventsCategory = "Application";
        private const SourceLevels ApplicationLoggingLevel = SourceLevels.Information;

        private const string DefaultEventSource = "Brnkly.Platform";
        private const string EventLogTraceListenerName = "EventLog";
        private const string TextFormatterName = "TextFormatter";

        private static readonly char[] TraceListenerNameDelimiters = new char[] { ';' };

        public IConfigurationSource Create(SourceLevels level)
        {
            var loggingConfigSource = new DictionaryConfigurationSource();
            var entLibSettings = this.CreateLoggingSettings(level);
            if (entLibSettings != null)
            {
                loggingConfigSource.Add(EntLib.LoggingSettings.SectionName, entLibSettings);
            }

            return loggingConfigSource;
        }

        private EntLib.LoggingSettings CreateLoggingSettings(SourceLevels level)
        {
            var entLibSettings = new EntLib.LoggingSettings();

            this.AddFormatters(entLibSettings);
            this.AddEventLogTraceListener(entLibSettings);
            this.AddApplicationCategorySource(entLibSettings);
            this.AddSpecialSources(level, entLibSettings);

            return entLibSettings;
        }

        private void AddFormatters(EntLib.LoggingSettings entLibSettings)
        {
            entLibSettings.Formatters.Add(this.GetTextFormatter());
        }

        private EntLib.FormatterData GetTextFormatter()
        {
            return new EntLib.TextFormatterData(
                TextFormatterName,
                @"Timestamp: {timestamp}
Message: {message}
Category: {category}
Priority: {priority}
EventId: {eventid}
Severity: {severity}
Title: {title}
Machine: {machine}
Application Domain: {appDomain}
Process Id: {processId}
Process Name: {processName}
Win32 Thread Id: {win32ThreadId}
Thread Name: {threadName}
Extended Properties: {dictionary({key} - {value}
)}");
        }

        private void AddEventLogTraceListener(EntLib.LoggingSettings entLibSettings)
        {
            entLibSettings.TraceListeners.Add(this.GetEventLogTraceListener());
        }

        private EntLib.TraceListenerData GetEventLogTraceListener()
        {
            var eventLogListener = new EntLib.FormattedEventLogTraceListenerData();
            eventLogListener.Name = EventLogTraceListenerName;
            eventLogListener.TraceOutputOptions = TraceOptions.Callstack;
            eventLogListener.Type = typeof(FormattedEventLogTraceListener);
            eventLogListener.ListenerDataType = typeof(EntLib.FormattedEventLogTraceListenerData);
            eventLogListener.Filter = SourceLevels.All;
            eventLogListener.Formatter = TextFormatterName;

            if (string.IsNullOrEmpty(PlatformApplication.Current.Name))
            {
                eventLogListener.Source = DefaultEventSource;
            }
            else
            {
                eventLogListener.Source = PlatformApplication.Current.Name;
            }

            return eventLogListener;
        }

        private void AddApplicationCategorySource(EntLib.LoggingSettings entLibSettings)
        {
            var source = new EntLib.TraceSourceData(ApplicationEventsCategory, ApplicationLoggingLevel);
            this.AddEventLogTraceListenerReference(source);
            entLibSettings.TraceSources.Add(source);
        }

        private void AddSpecialSources(SourceLevels level, EntLib.LoggingSettings entLibSettings)
        {
            entLibSettings.SpecialTraceSources.AllEventsTraceSource = this.GetAllEventsSource(level);
            entLibSettings.SpecialTraceSources.ErrorsTraceSource = this.GetErrorsSource(entLibSettings);
        }

        private EntLib.TraceSourceData GetAllEventsSource(SourceLevels level)
        {
            var source = new EntLib.TraceSourceData();
            source.DefaultLevel = level;
            this.AddEventLogTraceListenerReference(source);

            return source;
        }

        private EntLib.TraceSourceData GetErrorsSource(EntLib.LoggingSettings entLibSettings)
        {
            var source = new EntLib.TraceSourceData();
            source.DefaultLevel = SourceLevels.Warning;
            this.AddEventLogTraceListenerReference(source);

            return source;
        }

        private void AddEventLogTraceListenerReference(EntLib.TraceSourceData source)
        {
            source.TraceListeners.Add(new EntLib.TraceListenerReferenceData(EventLogTraceListenerName));
        }
    }
}
