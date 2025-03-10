using System;

namespace CoreResources.Utils
{
    public static class TimeUtils
    {
        /// <summary>
        /// Returns Unix Time in seconds
        /// </summary>
        public static int GetUnixTime()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// Returns current day number
        /// </summary>
        public static int GetCurrentDayNumber()
        {
            return DateTime.Now.Day;
        }

        /// <summary>
        /// Returns current day name
        /// </summary>
        public static string GetCurrentDayName()
        {
            return DateTime.Now.DayOfWeek.ToString();
        }

        /// <summary>
        /// Returns current month number
        /// </summary>
        public static int GetCurrentMonthNumber()
        {
            return DateTime.Now.Month;
        }

        /// <summary>
        /// Returns last two digits of the current year number
        /// </summary>
        public static int GetCurrentYearNumber()
        {
            var year = DateTime.Now.Year % 100;
            return year;
        }

        /// <summary>
        /// Returns current hour and minutes
        /// </summary>
        public static string GetCurrentTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }

        /// <summary>
        /// Returns current hour and minutes without formatting
        /// </summary>
        public static string GetCurrentTimeWithoutFormatting()
        {
            var time = DateTime.Now.ToString("HH") + DateTime.Now.ToString("mm");
            return time;
        }
    }
}
