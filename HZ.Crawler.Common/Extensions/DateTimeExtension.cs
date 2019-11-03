using System;

namespace HZ.Crawler.Common.Extensions
{
    public static partial class Extension
    {
        public static DateTime NextMonthFirstDay(this DateTime dateTime)
        {
            DateTime minValue = DateTime.MinValue;
            return minValue.AddYears(dateTime.Year - minValue.Year).AddMonths(dateTime.Month - minValue.Month + 1);
        }

        public static long GetMilliseconds(this DateTime dateTime)
        {
            return (dateTime.ToUniversalTime().Ticks - 621355968000000000L) / 10000L;
        }

        public static long GetSeconds(this DateTime dateTime)
        {
            return (dateTime.ToUniversalTime().Ticks - 621355968000000000L) / 10000000L;
        }
    }
}
