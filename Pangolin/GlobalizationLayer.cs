using System;
using System.Globalization;
using static System.Math;

namespace Pangolin
{
    class GlobalizationLayer
    {
        private const double _one = 1D;
        private const double _two = 2D;
        private const double _five = 5D;
        private const double _nine = 9D;
        private const double _thirtyTwo = 32D;

        private const double _celsiusToKelvin = 273.15D;
        private const double _feetToMeters = 0.3048D;

        private const int _seven = 7;
        private const int _ten = 10;

        private const double _earthRadiusMeters = 6371008.8D;

        public static readonly TimeZoneInfo _localTimeZoneInfo = TimeZoneInfo.Local;
        public static readonly TimeZoneInfo _utcTimeZoneInfo = TimeZoneInfo.Utc;

        #region Currency
        public static decimal CurrencyConversion(decimal baseCurrency, decimal exchangeRate)
        {
            return baseCurrency * exchangeRate;
        }
        #endregion

        #region DateTime
        public static int GetWeekOfYearISO(DateTime givenDate)
        {
            GregorianCalendar gregorianCalendar = new GregorianCalendar();

            int dayOfYear = gregorianCalendar.GetDayOfYear(givenDate);

            DayOfWeek dayOfWeek = gregorianCalendar.GetDayOfWeek(givenDate);
            int dayOfWeekIndex = dayOfWeek == DayOfWeek.Sunday ? _seven : (int)dayOfWeek;

            return (dayOfYear - dayOfWeekIndex + _ten) / _seven;
        }
        public static DateTime GetFirstDayOfLastMonth(DateTime givenDate)
        {
            return givenDate.AddDays(-(givenDate.Day - 1)).AddMonths(-1);
        }
        public static DateTime GetFirstDayOfMonthPrevMonth(DateTime givenDate)
        {
            return new DateTime(givenDate.Year, givenDate.Month, 1).AddMonths(-1);
        }
        public static DateTime GetLastDayOfLastMonth(DateTime givenDate)
        {
            return givenDate.AddDays(-(givenDate.Day));
        }

        public static DayOfWeek DoomsdayAlgorithm(DateTime givenDate)
        {
            string yearString = givenDate.Year.ToString();
            if (!string.IsNullOrWhiteSpace(yearString) && yearString.Length == 4)
            {
                string yearFirstTwoDigits = yearString.Substring(0, 2);
                string yearLastTwoDigits = yearString.Substring(2, 2);

                switch (yearFirstTwoDigits)
                {
                    case "18":
                        return DoomsdayAlgorithm(yearLastTwoDigits, DayOfWeek.Friday);
                    case "19":
                        return DoomsdayAlgorithm(yearLastTwoDigits, DayOfWeek.Wednesday);
                    case "20":
                        return DoomsdayAlgorithm(yearLastTwoDigits, DayOfWeek.Tuesday);
                    case "21":
                        return DoomsdayAlgorithm(yearLastTwoDigits, DayOfWeek.Sunday);
                    default:
                        break;
                }
            }

            throw new NotImplementedException();
        }

        private static DayOfWeek DoomsdayAlgorithm(string yearLastTwoDigits, DayOfWeek anchorDay)
        {
            int y = int.TryParse(yearLastTwoDigits, out y) ? y : default;
            int a = y / 12;
            int b = y % 12;
            int c = b / 4;
            int d = ((a + b + c) % 7) + (int)anchorDay;
            return (DayOfWeek)d;
        }
        #endregion

        #region Distance
        public static double FeetToMeters(double feet)
        {
            return feet * _feetToMeters;
        }

        public static double MetersToFeet(double meters)
        {
            return meters / _feetToMeters;
        }
        #endregion

        #region Location
        public static double HaversineFormula(double xLatitude, double xLongitude, double yLatitude, double yLongitude)
        {
            var deltaLongitude = yLongitude - xLongitude;
            var deltaLatitude = yLatitude - xLatitude;

            var a = Pow(Sin(deltaLatitude / _two), _two) * Cos(xLatitude) * Cos(yLatitude) * Pow(Sin(deltaLongitude / _two), _two);
            var c = _two * Asin(Min(_one, Sqrt(a)));

            return _earthRadiusMeters * c;
        }
        #endregion

        #region Temperature
        public static double CelsiusToFahrenheit(double degrees)
        {
            return (degrees * (_nine / _five)) + _thirtyTwo;
        }
        public static double CelsiusToKelvin(double degreesClesius)
        {
            return degreesClesius + _celsiusToKelvin;
        }
        public static double FahrenheitToCelsius(double degrees)
        {
            return (degrees - _thirtyTwo) * (_five / _nine);
        }
        public static double FahrenheitToKelvin(double degrees)
        {
            return FahrenheitToCelsius(degrees) + _celsiusToKelvin;
        }
        #endregion
    }
}
