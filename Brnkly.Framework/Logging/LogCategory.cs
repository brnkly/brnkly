
namespace Brnkly.Framework.Logging
{
    /// <summary>
    /// Represents a set of standard categories for log entries.  
    /// </summary>
    /// <remarks>
    /// Log categories are used to filter what is logged, and to direct logs to specific locations.
    /// See Enterprise Libary Logging Application Block documentation for details.
    /// At least one log category should be added to each log entry.
    /// Derive from LogCategory to add your own categories. 
    /// </remarks>
    public class LogCategory
    {
        internal string Value { get; private set; }

        internal static readonly LogCategory Configuration = new LogCategory("Configuration");
        internal static readonly LogCategory ServiceBus = new LogCategory("ServiceBus");
        internal static readonly LogCategory Framework = new LogCategory("Framework");
        internal static readonly LogCategory Exception = new LogCategory("Exception");

        protected LogCategory(string value)
        {
            this.Value = value;
        }

        public override string ToString()
        {
            return this.Value;
        }
    }
}
