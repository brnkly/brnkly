using System.Collections.ObjectModel;
using System.Diagnostics;
using Brnkly.Framework.Configuration;
using Newtonsoft.Json;

namespace Brnkly.Framework.Logging
{
    public sealed class LoggingSettings
    {
        public static readonly string StorageId = "brnkly/config/loggingsettings";
        private SourceLevels? currentLoggingLevel;

        public string Id { get; private set; }
        public Collection<Setting<SourceLevels>> LoggingLevels { get; private set; }

        [JsonIgnore]
        public SourceLevels CurrentLoggingLevel
        {
            get
            {
                if (!currentLoggingLevel.HasValue)
                {
                    currentLoggingLevel = this.LoggingLevels
                        .GetCurrentOrDefault(Log.DefaultLevel);
                }

                return currentLoggingLevel.Value;
            }
        }

        public LoggingSettings()
        {
            this.Id = StorageId;
            this.LoggingLevels = new Collection<Setting<SourceLevels>>();
        }
    }
}
