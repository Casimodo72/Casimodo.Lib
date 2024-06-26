﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Casimodo.Lib
{
    public enum DefaultPeriods
    {
        Today,
        ThisWeek,
        LastWeek,
        ThisMonth,
        LastMonth,
        ThisQuarter,
        LastQuarter,
        ThisSemester,
        LastSemester,
        ThisYear,
        LastYear
    }

    public static class DateTimeHelper
    {
        const string DefaultTimeZoneId = "W. Europe Standard Time";
        static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);

        public static Func<TimeZoneInfo> GetCurrentTimeZone { get; set; } = () => DefaultTimeZone;

        public static DateTimeOffset ToLocalDateTimeOffset(this DateOnly date)
            => new(date.Year, date.Month, date.Day, 0, 0, 0,
                TimeZoneInfo.Local.GetUtcOffset(new DateTime(date.Year, date.Month, date.Day)));

        public static DateTimeOffset ToUtcDateTimeOffset(this DateOnly date)
            => new(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);

        public static string ToDateString(this DateTimeOffset? value, string format = null)
        {
            if (value == null) return "";

            return ToDateString(value.Value, format);
        }

        public static string ToDateString(this DateTimeOffset value, string format = null)
        {
            return value.Date.ToString(format ?? "d");
        }

        private static string ToZonedString(object value, object p)
        {
            throw new NotImplementedException();
        }

        public static string ToZonedString(this DateTimeOffset? value, string format = null)
        {
            if (value == null) return "";

            return ToZonedString(value.Value, format);
        }

        public static string ToZonedString(this DateTimeOffset value, string format = null)
        {
            value = ApplyTimeZone(value);
            return format != null ? value.ToString(format) : value.ToString();
        }

        public static string ToZonedString(this DateTimeOffset value, IFormatProvider formatProvider)
        {
            value = ApplyTimeZone(value);
            return formatProvider != null ? value.ToString(formatProvider) : value.ToString();
        }

        public static DateTimeOffset? ZeroOffset(this DateTimeOffset? value)
        {
            if (value == null)
                return value;

            return ZeroOffset(value.Value);
        }

        public static DateTimeOffset ZeroOffset(this DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day,
                value.Hour, value.Minute, value.Second, value.Millisecond,
                TimeSpan.Zero);
        }

        public static DateTimeOffset SetOffset(this DateTimeOffset value, TimeSpan offset)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day,
                value.Hour, value.Minute, value.Second, value.Millisecond,
                offset);
        }

        public static DateTimeOffset SetDate(this DateTimeOffset value, DateOnly date)
        {
            return value.Year == date.Year && value.Month == date.Month && value.Day == date.Day
                ? value
                : new DateTimeOffset(
                    date.Year, date.Month, date.Day,
                    value.Hour, value.Minute, value.Second, value.Millisecond, value.Offset);
        }

        public static DateTimeOffset? SetTime(this DateTimeOffset? value, TimeSpan time)
        {
            return value != null ? SetTime(value.Value, time) : null;
        }

        public static DateTimeOffset SetTime(this DateTimeOffset value, TimeSpan time)
        {
            return new DateTimeOffset(
                value.Year, value.Month, value.Day,
                time.Hours, time.Minutes, time.Seconds, time.Milliseconds, value.Offset);
        }

        public static DateTimeOffset? TruncateTime(this DateTimeOffset? value)
        {
            return value != null ? TruncateTime(value.Value) : value;
        }

        public static DateTimeOffset TruncateTime(this DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, 0, value.Offset);
        }

        public static DateTimeOffset TruncateMinutes(this DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, 0, 0, 0, value.Offset);
        }

        public static DateTimeOffset TruncateSeconds(this DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, 0, value.Offset);
        }

        static DateTimeOffset ApplyTimeZone(DateTimeOffset value)
        {
            return TimeZoneInfo.ConvertTime(value, GetCurrentTimeZone());
        }

        public static DateTimeOffset? SetDate(this DateTimeOffset? value, DateTimeOffset? date)
        {
            if (value == null || date == null)
                return value;

            var val = value.Value;
            var dat = date.Value;

            return new DateTimeOffset(dat.Year, dat.Month, dat.Day,
                val.Hour, val.Minute, val.Second, val.Millisecond,
                val.Offset);
        }

        public static DateTimeOffset? ConvertToZone(this DateTimeOffset? value, TimeZoneInfo tzi)
        {
            if (value == null)
                return value;

            return TimeZoneInfo.ConvertTime(value.Value, tzi);
        }

        public static DateTimeOffset ConvertToZone(this DateTimeOffset value, TimeZoneInfo tzi)
        {
            return TimeZoneInfo.ConvertTime(value, tzi);
        }

        public static DateTime StartOfMonth(this DateTime value)
        {
            return new DateTime(value.Year, value.Month, 1);
        }

        public static DateTimeOffset? LocalStartOfDayUtc(this DateTimeOffset? value)
        {
            if (value == null) return null;

            return LocalStartOfDayUtc(value.Value);
        }

        public static DateTimeOffset LocalStartOfDayUtc(this DateTimeOffset value)
        {
            var localDateTime = value.LocalDateTime;
            return new DateTimeOffset(localDateTime.Year, localDateTime.Month, localDateTime.Day,
                0, 0, 0, 0,
                TimeSpan.Zero);
        }

        public static DateTimeOffset? StartOfDayUtc(this DateTimeOffset? value)
        {
            if (value == null) return null;

            return StartOfDayUtc(value.Value);
        }

        public static DateTimeOffset StartOfDayUtc(this DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, 0, TimeSpan.Zero);
        }

        public static IEnumerable<DefaultPeriods> GetPeriods()
        {
            foreach (DefaultPeriods period in Enum.GetValues(typeof(DefaultPeriods)))
                yield return period;

            yield break;
        }

        public static bool GetRange(DefaultPeriods period, out DateTime from, out DateTime till)
        {
            bool result = true;
            from = DateTime.Today;
            till = DateTime.Today;
            switch (period)
            {
                case DefaultPeriods.Today:
                    from = DateTime.Today;
                    till = DateTime.Today.AddDays(1).AddSeconds(-1);
                    break;

                case DefaultPeriods.ThisWeek:
                    from = DateTime.Now.AddDays(-(GetDayOfWeek() - GetFirstDayOfWeek())).Date;
                    till = from.AddDays(7).AddSeconds(-1);
                    break;

                case DefaultPeriods.LastWeek:
                    from = DateTime.Now.AddDays(-(GetDayOfWeek() - GetFirstDayOfWeek())).Date.AddDays(-7);
                    till = from.AddDays(7).AddSeconds(-1);
                    break;

                case DefaultPeriods.ThisMonth:
                    from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    till = from.AddMonths(1).AddSeconds(-1);
                    break;

                case DefaultPeriods.LastMonth:
                    from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                    till = from.AddMonths(1).AddSeconds(-1);
                    break;

                case DefaultPeriods.ThisQuarter:
                    from = new DateTime(DateTime.Today.Year, (GetQuarter() - 1) * 3 + 1, 1);
                    till = from.AddMonths(3).AddSeconds(-1);
                    break;

                case DefaultPeriods.LastQuarter:
                    from = new DateTime(DateTime.Today.Year, (GetQuarter() - 1) * 3 + 1, 1).AddMonths(-3);
                    till = from.AddMonths(3).AddSeconds(-1);
                    break;

                case DefaultPeriods.ThisSemester:
                    from = new DateTime(DateTime.Today.Year, (GetSemester() - 1) * 6 + 1, 1);
                    till = from.AddMonths(6).AddSeconds(-1);
                    break;

                case DefaultPeriods.LastSemester:
                    from = new DateTime(DateTime.Today.Year, (GetSemester() - 1) * 6 + 1, 1).AddMonths(-6);
                    till = from.AddMonths(6).AddSeconds(-1);
                    break;

                case DefaultPeriods.ThisYear:
                    from = new DateTime(DateTime.Today.Year, 1, 1);
                    till = from.AddYears(1).AddSeconds(-1);
                    break;

                case DefaultPeriods.LastYear:
                    from = new DateTime(DateTime.Today.Year, 1, 1).AddYears(-1);
                    till = from.AddYears(1).AddSeconds(-1);
                    break;

                default:
                    result = false;
                    break;
            }
            return result;
        }

        private static int GetSemester()
        {
            return (DateTime.Today.Month - 1) / 6 + 1;
        }

        private static int GetQuarter()
        {
            return (DateTime.Today.Month - 1) / 3 + 1;
        }

        public static DateTimeOffset MoveBackwardsToMonday(this DateTimeOffset source)
        {
            var delta = DayOfWeek.Monday - source.DayOfWeek;
            if (delta > 0)
                delta -= 7;
            return delta != 0 ? source.AddDays(delta) : source;
        }

        private static DayOfWeek GetDayOfWeek()
        {
            return DateTime.Today.DayOfWeek;
        }

        private static DayOfWeek GetFirstDayOfWeek()
        {
            return System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            // KABU TODO: REMOVE: return Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        }

        public static IEnumerable<TimeZoneInfo> GetTimeZones(DateTimeOffset time)
        {
            TimeSpan offset = time.Offset;
            return TimeZoneInfo.GetSystemTimeZones().Where(x => x.GetUtcOffset(time) == offset);
        }
    }
}