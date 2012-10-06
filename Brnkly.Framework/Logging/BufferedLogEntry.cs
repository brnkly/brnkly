using System;
using System.Diagnostics;

namespace Brnkly.Framework.Logging
{
    internal class BufferedLogEntry
    {
        public TraceEventType Severity { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public BufferedLogEntry()
        {
            this.Severity = TraceEventType.Information;
        }
    }
}
