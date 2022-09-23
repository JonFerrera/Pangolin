using System;
using System.Globalization;
using static System.Math;

namespace Pangolin
{
    public static class GlobalizationLayer
    {
        private const double
            _one = 1D,
            _two = 2D,
            _three = 3D,
            _four = 4D,
            _five = 5D,
            _nine = 9D,
            _twelve = 12D,
            _sixteen = 16D,
            _thirtyTwo = 32D;

        private const double 
            _celsiusToKelvin = 273.15D,
            _feetToMeters = 0.3048D;

        private const int 
            _seven = 7,
            _ten = 10;

        private const double
            _earthRadiusMeters = 6371008.8D,        // Mean radius.
            _earthRadiusEquatorMeters = 6378137.0D, // Semi-major axis.
            _earthRadiusPolarMeters = 6356752.3D,   // Semi-minor axis.
            _earthFlattening = _one / 298.257223563D;

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
            int x = givenDate.Year % 400;

            DayOfWeek anchorDay = DayOfWeek.Tuesday;

            if (x >= 0 && x < 100)
            {
                anchorDay = DayOfWeek.Tuesday;
            }
            else if (x >= 100 && x < 200)
            {
                anchorDay = DayOfWeek.Sunday;
            }
            else if (x >= 200 && x < 300)
            {
                anchorDay = DayOfWeek.Friday;
            }
            else
            {
                anchorDay = DayOfWeek.Wednesday;
            }

            int yearLastTwoDigits = givenDate.Year % 100;

            return DoomsdayAlgorithm(yearLastTwoDigits, anchorDay);
        }

        private static DayOfWeek DoomsdayAlgorithm(int yearLastTwoDigits, DayOfWeek anchorDay)
        {
            int a = yearLastTwoDigits / 12;
            int b = yearLastTwoDigits % 12;
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
            var deltaLongitude = MathLayer.ConvertToRadians(yLongitude - xLongitude);
            var deltaLatitude = MathLayer.ConvertToRadians(yLatitude - xLatitude);

            var a = Sin(deltaLatitude / _two) * Sin(deltaLatitude / _two) + Cos(MathLayer.ConvertToRadians(xLatitude)) * Cos(MathLayer.ConvertToRadians(yLatitude)) * Pow(Sin(deltaLongitude / _two), _two);
            var c = _two * Atan2(Sqrt(a), Sqrt(_one - a));

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
