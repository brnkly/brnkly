using System;

namespace Brnkly.Framework
{
    public static class UtcConversionExtensions
    {
        private static readonly TimeZoneInfo EasternTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        public static DateTimeOffset GetUtcFromEasternTime(this DateTime easternDateTime)
        {
            return new DateTimeOffset(
                TimeZoneInfo.ConvertTimeToUtc(easternDateTime, EasternTimeZone))
                .ToUniversalTime();
        }

        public static DateTimeOffset GetEasternTimeFromUtc(this DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToOffset(EasternTimeZone.GetUtcOffset(dateTimeOffset));
        }
    }
}
