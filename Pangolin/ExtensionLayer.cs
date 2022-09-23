using System;
using System.Data;
using System.Linq;
using System.Text;

namespace Pangolin
{
    public static class ExtensionLayer
    {
        #region DataSet
        public static DataTable GetProcessTable(this DataSet thisDataSet, int index = 0, string filter = null, string sort = null, DataViewRowState dataViewRowState = DataViewRowState.Added | DataViewRowState.ModifiedCurrent | DataViewRowState.Unchanged, bool isDistinct = false)
        {
            if (thisDataSet == null)
            {
                throw new ArgumentNullException(nameof(thisDataSet));
            }
            else if (thisDataSet.Tables.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataSet)} has no tables.");
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} cannot be negative.");
            }
            else if (index >= thisDataSet.Tables.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} exceeds the number of tables in the set.");
            }

            return thisDataSet.Tables[index].GetProcessTable(filter, sort, dataViewRowState, isDistinct);
        }

        public static DataRow GetProcessTableRow(this DataSet thisDataSet, int index = 0, int rowIndex = 0, string filter = null, string sort = null, DataViewRowState dataViewRowState = DataViewRowState.Added | DataViewRowState.ModifiedCurrent | DataViewRowState.Unchanged, bool isDistinct = false)
        {
            if (thisDataSet == null)
            {
                throw new ArgumentNullException(nameof(thisDataSet));
            }
            else if (thisDataSet.Tables.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataSet)} has no tables.");
            }
            else if (index < 0 || index >= thisDataSet.Tables.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} cannot be negative.");
            }
            else if (index >= thisDataSet.Tables.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} exceeds the number of tables in the set.");
            }

            return thisDataSet.Tables[index].GetProcessRow(rowIndex ,filter, sort, dataViewRowState, isDistinct);
        }
        #endregion

        #region DataTable
        public static DataRow GetProcessRow(this DataTable thisDataTable, int index = 0, string filter = null, string sort = null, DataViewRowState dataViewRowState = DataViewRowState.Added | DataViewRowState.ModifiedCurrent | DataViewRowState.Unchanged, bool isDistinct = false)
        {
            if (thisDataTable == null)
            {
                throw new ArgumentNullException(nameof(thisDataTable));
            }
            else if (thisDataTable.Columns.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataTable)} has no columns.");
            }
            else if (thisDataTable.Rows.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataTable)} has no rows.");
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "index cannot be negative.");
            }

            using (DataView dView = new DataView(thisDataTable, filter ?? string.Empty, sort ?? string.Empty, dataViewRowState))
            {
                if (dView.Count >= 1 && index < dView.Count)
                {
                    return dView.ToTable(isDistinct).Rows[index];
                }
                else
                {
                    throw new InvalidOperationException("There are no rows to return.");
                }
            }
        }

        public static DataTable GetProcessTable(this DataTable thisDataTable, string filter = null, string sort = null, DataViewRowState dataViewRowState = DataViewRowState.Added | DataViewRowState.ModifiedCurrent | DataViewRowState.Unchanged, bool isDistinct = false)
        {
            if (thisDataTable == null)
            {
                throw new ArgumentNullException(nameof(thisDataTable));
            }
            else if (thisDataTable.Columns.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataTable)} has no columns.");
            }
            else if (thisDataTable.Rows.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataTable)} has no rows.");
            }

            using (DataView dView = new DataView(thisDataTable, filter ?? string.Empty, sort ?? string.Empty, dataViewRowState))
            {
                return dView.ToTable(isDistinct);
            }
        }

        public static string ToCSV(this DataTable thisDataTable, string delimiter, bool includeHeader = true)
        {
            if (thisDataTable == null)
            {
                throw new ArgumentNullException(nameof(thisDataTable));
            }
            else if (thisDataTable.Columns.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataTable)} has no columns.");
            }
            else if (thisDataTable.Rows.Count < 1)
            {
                throw new InvalidOperationException($"{nameof(thisDataTable)} has no rows.");
            }
            else if (string.IsNullOrWhiteSpace(delimiter))
            {
                throw new ArgumentNullException(nameof(delimiter));
            }

            StringBuilder csvString = new StringBuilder();

            if (includeHeader)
            {
                try
                {
                    for (int i = 0; i < thisDataTable.Columns.Count; i++)
                    {
                        csvString.Append(thisDataTable.Columns[i].ColumnName);
                        csvString.Append(i == thisDataTable.Columns.Count - 1 ? Environment.NewLine : delimiter);
                    }
                }
                catch (ArgumentOutOfRangeException exc) { ExceptionLayer.CoreHandle(exc); throw; }
            }

            try
            {
                foreach (DataRow dataRow in thisDataTable.Rows)
                {
                    for (int i = includeHeader ? 0 : 1; i < thisDataTable.Columns.Count; i++)
                    {
                        csvString.Append(dataRow[i].ToString());
                        csvString.Append(i == thisDataTable.Columns.Count - 1 ? Environment.NewLine : delimiter);
                    }
                }
            }
            catch (ArgumentOutOfRangeException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return csvString.ToString();
        }
        #endregion

        #region DateTime
        public static bool IsBetween(this DateTime thisDateTime, DateTime start, DateTime end)
        {
            return thisDateTime.Ticks >= start.Ticks && thisDateTime.Ticks <= end.Ticks;
        }

        public static bool IsWeekday(this DateTime thisDateTime)
        {
            return thisDateTime.DayOfWeek != DayOfWeek.Sunday && thisDateTime.DayOfWeek != DayOfWeek.Saturday;
        }

        public static bool IsWeekend(this DateTime thisDateTime)
        {
            return thisDateTime.DayOfWeek == DayOfWeek.Sunday || thisDateTime.DayOfWeek == DayOfWeek.Saturday;
        }

        public static DateTime[] GetWeek(this DateTime thisDateTime, DayOfWeek dayOfWeek = DayOfWeek.Sunday)
        {
            int offsetDays = dayOfWeek - thisDateTime.DayOfWeek;
            DateTime startOfWeek = thisDateTime.AddDays(offsetDays).Date;
            DateTime[] week = new DateTime[7];

            for (int i = 0; i < week.Length; i++)
            {
                week[i] = startOfWeek.AddDays(i);
            }

            return week;
        }

        public static DateTime Next(this DateTime thisDateTime, DayOfWeek dayOfWeek)
        {
            int offsetDays = dayOfWeek - thisDateTime.DayOfWeek;

            if (offsetDays <= 0)
            {
                offsetDays += 7;
            }

            return thisDateTime.AddDays(offsetDays);
        }

        public static DateTime Previous(this DateTime thisDateTime, DayOfWeek dayOfWeek)
        {
            int offsetDays = dayOfWeek - thisDateTime.DayOfWeek;

            if (offsetDays >= 0)
            {
                offsetDays -= 7;
            }

            return thisDateTime.AddDays(offsetDays);
        }

        public static long ToUnixTime(this DateTime thisDateTime, bool isForJavascript = false)
        {
            DateTimeOffset dateTimeOffset = new DateTimeOffset(thisDateTime);

            return isForJavascript ? dateTimeOffset.ToUnixTimeMilliseconds() : dateTimeOffset.ToUnixTimeSeconds();
        }
        #endregion

        #region int
        public static char[] GetDigits(this int thisInt)
        {
            return thisInt.ToString().ToCharArray();
        }
        #endregion

        #region object
        public static bool IsIn<T>(this T source, params T[] list)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return list.Contains(source);
        }

        public static bool IsNull(this object thisObject)
        {
            return thisObject == null;
        }

        public static object ToDBNull(this object thisObject)
        {
            return thisObject ?? DBNull.Value;
        }
        #endregion

        #region string
        public static string FromBase64String(this string thisString)
        {
            if (thisString == null)
            {
                throw new ArgumentNullException(nameof(thisString));
            }

            try
            {
                byte[] textBytes = Convert.FromBase64String(thisString);

                try
                {
                    thisString = ConfigurationLayer.DefaultEncoding.GetString(textBytes);
                }
                catch (DecoderFallbackException exc) { ExceptionLayer.CoreHandle(exc); throw; }
                catch (ArgumentException exc) { ExceptionLayer.CoreHandle(exc); throw; }
            }
            catch (FormatException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return thisString;
        }

        public static string Left(this string thisString, int length)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }
            else if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} cannot be less than zero.");
            }
            else if (length > thisString.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} exceeds the length of the string.");
            }

            return thisString.Substring(0, length);
        }
        
        public static string Mid(this string thisString, int index)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} cannot be less than zero.");
            }
            else if (index > thisString.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} exceeds the length of the string.");
            }

            return thisString.Substring(index);
        }

        public static string Mid(this string thisString, int index, int length)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} cannot be less than zero.");
            }
            else if (index > thisString.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"{nameof(index)} exceeds the length of the string.");
            }
            else if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} cannot be less than zero.");
            }
            else if (length > thisString.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} exceeds the length of the string.");
            }
            else if ((index + length) > thisString.Length)
            {
                throw new ArgumentOutOfRangeException($"({nameof(index)} + {nameof(length)})", (index + length), $"({nameof(index)} + {nameof(length)}) refers to a position not inside the string.");
            }

            return thisString.Substring(index, length);
        }

        public static string Repeat(this string thisString, int count)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }
            else if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, $"{nameof(count)} cannot be less than one.");
            }

            StringBuilder stringBuilder = new StringBuilder();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    stringBuilder.Append(thisString);
                }
            }
            catch (ArgumentOutOfRangeException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return stringBuilder.ToString();
        }

        public static string Right(this string thisString, int length)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }
            else if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} cannot be less than zero.");
            }
            else if (length > thisString.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} exceeds the length of the string.");
            }

            return thisString.Substring(thisString.Length - length, length);
        }

        public static string ToBase64String(this string thisString)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }

            byte[] textBytes = null;
            try
            {
                textBytes = Encoding.UTF8.GetBytes(thisString);
            }
            catch (EncoderFallbackException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return Convert.ToBase64String(textBytes);
        }

        public static int ToInt(this string thisString)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }

            int.TryParse(thisString, out int result);

            return result;
        }

        public static long ToLong(this string thisString)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }

            long.TryParse(thisString, out long result);

            return result;
        }

        public static short ToShort(this string thisString)
        {
            if (string.IsNullOrWhiteSpace(thisString))
            {
                throw new ArgumentNullException(nameof(thisString));
            }

            short.TryParse(thisString, out short result);

            return result;
        }
        #endregion

        #region StringBuilder
        public static bool IsEmpty(this StringBuilder thisStringBuilder)
        {
            return thisStringBuilder?.Length == 0;
        }
        #endregion

        #region TimeSpan
        public static string ToHumanReadable(this TimeSpan thisTimeSpan)
        {
            StringBuilder stringBuilder = new StringBuilder(64);

            try
            {
                stringBuilder.Append(thisTimeSpan.Days.ToString());
                stringBuilder.Append(thisTimeSpan.Days == 1 ? " day, " : " days, ");

                stringBuilder.Append(thisTimeSpan.Hours.ToString());
                stringBuilder.Append(thisTimeSpan.Hours == 1 ? " hour, " : " hours, ");

                stringBuilder.Append(thisTimeSpan.Minutes.ToString());
                stringBuilder.Append(thisTimeSpan.Minutes == 1 ? " minute, " : " minutes, ");

                stringBuilder.Append(thisTimeSpan.Seconds.ToString());
                stringBuilder.Append(thisTimeSpan.Seconds == 1 ? " second, and " : " seconds, and ");

                stringBuilder.Append(thisTimeSpan.Milliseconds.ToString());
                stringBuilder.Append(thisTimeSpan.Milliseconds == 1 ? " millisecond." : " milliseconds.");
            }
            catch (ArgumentOutOfRangeException exc) { ExceptionLayer.CoreHandle(exc); throw; }

            return stringBuilder.ToString();
        }
        #endregion
    }
}
