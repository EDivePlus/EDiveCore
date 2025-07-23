using System;
using EDIVE.Time.DateTimeUtils;
using UnityEngine;

namespace EDIVE.DataStructures.DateTimeStructures
{
    public static class DateTimeExtensions
    {
        /// <summary>
        ///     Converts date time to UTC.
        ///     If  <see cref="P:System.DateTime.Kind" /> is
        ///     <see cref="F:System.DateTimeKind.Unspecified" />, set it to UTC.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime ConvertToUtc(this DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Unspecified ? dateTime.WithKind(DateTimeKind.Utc) : dateTime.ToUniversalTime();
        }

        /// <summary>
        ///     Converts date time to UTC.
        ///     If  <see cref="P:System.DateTime.Kind" /> is
        ///     <see cref="F:System.DateTimeKind.Unspecified" />, set it to UTC.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static UDateTime ConvertToUtc(this UDateTime dateTime)
        {
            return dateTime.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.UTC;
        }

        public static bool IsInInterval(this DateTime dateTime, DateTime start, DateTime end)
        {
            if (start > end)
            {
                Debug.LogError("Start time is after end time!");
                return false;
            }

            return start <= dateTime && dateTime <= end;
        }

        public static DateTime OnMidnight(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, dt.Kind);
        }

        public static DateTime WithKind(this DateTime dt, DateTimeKind kind)
        {
            return DateTime.SpecifyKind(dt, kind);
        }
    }
}
