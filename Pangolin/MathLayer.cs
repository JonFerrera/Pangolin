using System;
using System.Collections.Generic;
using System.Linq;

namespace Pangolin
{
    public static class MathLayer
    {
        private const double _kelvin = 273.15D;

        private const int _annual = 1;
        private const int semiAnnual = 2;
        private const int _quarterly = 4;
        private const int _monthly = 12;
        private const int _daily = 365;

        private static readonly double _goldenRatio = (1 + Math.Sqrt(5)) / 2;

        private static readonly Random _random = new Random();

        #region Basic
        public static bool IsEven(int element)
        {
            return element % 2 == 0;
        }

        public static bool IsOdd(int element)
        {
            return element % 2 == 1;
        }

        public static bool IsWhole(decimal element)
        {
            return element % decimal.One == decimal.Zero;
        }

        public static bool IsWhole(double element)
        {
            return element % 1D == 0D;
        }

        public static decimal Product(decimal[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            decimal product = decimal.One;

            for (int i = 0; i < elements.Length; i++)
            {
                product *= elements[i];
            }

            return product;
        }

        public static double Product(double[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            double product = 1D;

            for (int i = 0; i < elements.Length; i++)
            {
                product *= elements[i];
            }

            return product;
        }

        public static int Product(int[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            int product = 1;

            for (int i = 0; i < elements.Length; i++)
            {
                product *= elements[i];
            }

            return product;
        }

        public static decimal Sum(decimal[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            decimal sum = decimal.Zero;

            for (int i = 0; i < elements.Length; i++)
            {
                sum += elements[i];
            }

            return sum;
        }

        public static double Sum(double[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            double sum = 0D;

            for (int i = 0; i < elements.Length; i++)
            {
                sum += elements[i];
            }

            return sum;
        }

        public static int Sum(int[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            int sum = 0;

            for (int i = 0; i < elements.Length; i++)
            {
                sum += elements[i];
            }

            return sum;
        }
        #endregion

        #region Basic - Decimal Math
        private static decimal Pow(decimal givenBase, decimal givenExponent)
        {
            double x = Convert.ToDouble(givenBase);
            double y = Convert.ToDouble(givenExponent);
            double z = Math.Pow(x, y);

            try
            {
                return Convert.ToDecimal(z);
            }
            catch (OverflowException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        private static decimal Sqrt(decimal givenNumber)
        {
            double d = Convert.ToDouble(givenNumber);

            double principalRoot = Math.Sqrt(d);

            try
            {
                return Convert.ToDecimal(principalRoot);
            }
            catch (OverflowException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region Basic - Geometry
        #region Shapes
        public static (double area, double circumference) Circle(double r)
        {
            return (CircleArea(r), Circumference(r));
        }

        public static (double surfaceArea, double volume) Sphere(double r)
        {
            return (SphereSurfaceArea(r), SphereVolume(r));
        }
        #endregion

        #region Area
        public static double CircleArea(double r)
        {
            return Math.PI * Math.Pow(r, 2D);
        }

        public static double ParallelogramArea(double b, double h)
        {
            return b * h;
        }

        public static double RectangleArea(double l, double w)
        {
            return l * w;
        }

        public static double TrapezoidArea(double b1, double b2, double h)
        {
            return 0.5D * (b1 + b2) * h;
        }

        public static double TriangleArea(double b, double h)
        {
            return 0.5D * b * h;
        }
        #endregion

        #region Perimeter
        public static double Circumference(double r)
        {
            return 2D * Math.PI * r;
        }
        #endregion

        #region Surface Area
        public static double CircularCylinderSurfaceArea(double r, double h)
        {
            return (2D * CircleArea(r)) + (Circumference(r) * h);
        }

        public static double RectangularPrismSurfaceArea(double l, double w, double h)
        {
            return (2D * (l * w)) + (2D * (h * w)) + (2D * (l * h));
        }

        public static double SphereSurfaceArea(double r)
        {
            return (4D / 3D) * Math.PI * Math.Pow(r, 3D);
        }
        #endregion

        #region Volume
        public static double CircularConeVolume(double r, double h)
        {
            return (1D / 3D) * CircleArea(r) * h;
        }

        public static double CircularCylinderVolume(double r, double h)
        {
            return CircleArea(r) * h;
        }

        public static double RectangularPrismVolume(double l, double w, double h)
        {
            return l * w * h;
        }

        public static double SphereVolume(double r)
        {
            return (4D / 3D) * Math.PI * r * r * r;
        }

        public static double SquarePyramidVolume(double w, double h)
        {
            return (1D / 3D) * Math.Pow(w, 2D) * h;
        }
        #endregion
        #endregion

        #region General
        public static (decimal arithmeticMean, decimal geometricMean, decimal harmonicMean) Mean(decimal[] elements)
        {
            return (ArithmeticMean(elements), GeometricMean(elements), HarmonicMean(elements));
        }

        public static (double arithmeticMean, double geometricMean, double harmonicMean) Mean(double[] elements)
        {
            return (ArithmeticMean(elements), GeometricMean(elements), HarmonicMean(elements));
        }

        public static decimal ArithmeticMean(decimal[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            decimal sum = decimal.Zero;
            for (int i = 0; i < elements.Length; i++)
            {
                sum += elements[i];
            }

            return sum / elements.Length;
        }

        public static double ArithmeticMean(double[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            double sum = 0D;
            for (int i = 0; i < elements.Length; i++)
            {
                sum += elements[i];
            }

            return sum / elements.Length;
        }

        public static double ArithmeticMean(int[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            double sum = 0D;
            for (int i = 0; i < elements.Length; i++)
            {
                sum += elements[i];
            }

            return sum / elements.Length;
        }

        public static decimal ArithmeticMeanWeighted(Dictionary<decimal, decimal> orderedPairs)
        {
            decimal sum = decimal.Zero;
            decimal sumWeights = decimal.Zero;

            if (orderedPairs?.Count > 1)
            {
                foreach (KeyValuePair<decimal, decimal> orderedPair in orderedPairs)
                {
                    sum += orderedPair.Key * orderedPair.Value;
                    sumWeights += orderedPair.Value;
                }

                if (sumWeights != decimal.One)
                {
                    return decimal.Zero;
                }
            }

            return sum / orderedPairs.Count;
        }

        public static double ArithmeticMeanWeighted(Dictionary<double, double> orderedPairs)
        {
            double sum = 0D;
            double sumWeights = 0D;

            if (orderedPairs?.Count > 1)
            {
                foreach (KeyValuePair<double, double> orderedPair in orderedPairs)
                {
                    sum += orderedPair.Key * orderedPair.Value;
                    sumWeights += orderedPair.Value;
                }

                if (sumWeights != 1D)
                {
                    return 0D;
                }
            }

            return sum / orderedPairs.Count;
        }

        public static decimal GeometricMean(decimal[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            decimal product = Product(elements);

            return Pow(product, decimal.One / elements.Length);
        }

        public static double GeometricMean(double[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            double product = Product(elements);

            return Math.Pow(product, 1D / elements.Length);
        }

        public static decimal HarmonicMean(decimal[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            decimal sum = decimal.Zero;

            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == decimal.Zero)
                {
                    return decimal.Zero;
                }

                sum += (decimal.One / elements[i]);
            }

            if (sum != decimal.Zero)
            {
                return elements.Length / sum;
            }
            else
            {
                return decimal.Zero;
            }
        }

        public static double HarmonicMean(double[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            double sum = default;

            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == 0D)
                {
                    return default;
                }

                sum += (1D / elements[i]);
            }

            if (sum != 0D)
            {
                return elements.Length / sum;
            }
            else
            {
                return default;
            }
        }

        public static decimal Median(decimal[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            QuickSort(elements);

            if (IsOdd(elements.Length))
            {
                return elements[(elements.Length - 1) / 2];
            }
            else
            {
                decimal low = elements[(elements.Length - 1) / 2];
                decimal high = elements[((elements.Length - 1) / 2) + 1];

                return (low + high) / 2;
            }
        }

        public static double Median(double[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            QuickSort(elements);

            if (IsOdd(elements.Length))
            {
                return elements[(elements.Length - 1) / 2];
            }
            else
            {
                double low = elements[(elements.Length - 1) / 2];
                double high = elements[((elements.Length - 1) / 2) + 1];

                return (low + high) / 2D;
            }
        }

        public static double Median(int[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            QuickSort(elements);

            if (IsOdd(elements.Length))
            {
                return elements[(elements.Length - 1) / 2];
            }
            else
            {
                int low = elements[(elements.Length - 1) / 2];
                int high = elements[((elements.Length - 1) / 2) + 1];

                return (low + high) / 2D;
            }
        }

        public static decimal[] Mode(decimal[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            Dictionary<decimal, int> modeCounts = new Dictionary<decimal, int>();

            bool noMode = true;
            foreach (decimal element in elements)
            {
                if (modeCounts.Keys.Contains(element))
                {
                    modeCounts[element]++;
                    noMode = false;
                }
                else
                {
                    modeCounts.Add(element, 1);
                }
            }

            if (noMode) { return null; }

            List<decimal> modes = new List<decimal>();
            int maxFrecuency = 1;
            foreach (KeyValuePair<decimal, int> modeCount in modeCounts)
            {
                if (maxFrecuency < modeCount.Value)
                {
                    maxFrecuency = modeCount.Value;
                    modes.Clear();
                    modes.Add(modeCount.Key);
                }
                else if (maxFrecuency == modeCount.Value)
                {
                    modes.Add(modeCount.Key);
                }
            }

            return modes.ToArray();
        }

        public static double[] Mode(double[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            Dictionary<double, int> modeCounts = new Dictionary<double, int>();

            bool noMode = true;
            foreach (double element in elements)
            {
                if (modeCounts.Keys.Contains(element))
                {
                    modeCounts[element]++;
                    noMode = false;
                }
                else
                {
                    modeCounts.Add(element, 1);
                }
            }

            if (noMode) { return null; }

            List<double> modes = new List<double>();
            int maxFrecuency = 1;
            foreach (KeyValuePair<double, int> modeCount in modeCounts)
            {
                if (maxFrecuency < modeCount.Value)
                {
                    maxFrecuency = modeCount.Value;
                    modes.Clear();
                    modes.Add(modeCount.Key);
                }
                else if (maxFrecuency == modeCount.Value)
                {
                    modes.Add(modeCount.Key);
                }
            }

            return modes.ToArray();
        }

        public static int[] Mode(int[] elements)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }

            Dictionary<int, int> modeCounts = new Dictionary<int, int>();

            bool noMode = true;
            foreach (int element in elements)
            {
                if (modeCounts.Keys.Contains(element))
                {
                    modeCounts[element]++;
                    noMode = false;
                }
                else
                {
                    modeCounts.Add(element, 1);
                }
            }

            if (noMode) { return null; }

            List<int> modes = new List<int>();
            int maxFrecuency = 1;
            foreach (KeyValuePair<int, int> modeCount in modeCounts)
            {
                if (maxFrecuency < modeCount.Value)
                {
                    maxFrecuency = modeCount.Value;
                    modes.Clear();
                    modes.Add(modeCount.Key);
                }
                else if (maxFrecuency == modeCount.Value)
                {
                    modes.Add(modeCount.Key);
                }
            }

            return modes.ToArray();
        }

        public static decimal Variance(decimal[] elements, bool population = true)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }
            else if (population && elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} must contain at least one element when calculating a population.", nameof(elements)); }
            else if (!population && elements.Length < 2) { throw new ArgumentException($"{nameof(elements)} must contain at least two element when not calculating a population.", nameof(elements)); }
            
            decimal sum = decimal.Zero;

            decimal average = ArithmeticMean(elements);
            
            for (int i = 0; i < elements.Length; i++)
            {
                decimal difference = elements[i] - average;
                sum += difference * difference;
            }

            return population ? sum / elements.Length : sum / (elements.Length - decimal.One);
        }

        public static double Variance(double[] elements, bool population = true)
        {
            if (elements == null) { throw new ArgumentNullException(nameof(elements)); }
            else if (elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} contains no elements.", nameof(elements)); }
            else if (population && elements.Length < 1) { throw new ArgumentException($"{nameof(elements)} must contain at least one element when calculating a population.", nameof(elements)); }
            else if (!population && elements.Length < 2) { throw new ArgumentException($"{nameof(elements)} must contain at least two element when not calculating a population.", nameof(elements)); }

            double sum = 0D;

            double average = ArithmeticMean(elements);

            for (int i = 0; i < elements.Length; i++)
            {
                sum += Math.Pow(elements[i] - average, 2D);
            }

            return population ? sum / elements.Length : sum / (elements.Length - 1D);
        }

        public static decimal StandardDeviation(decimal[] elements, bool population = true)
        {
            return Pow(Variance(elements, population), 0.5M);
        }

        public static double StandardDeviation(double[] elements, bool population = true)
        {
            return Math.Pow(Variance(elements, population), 0.5D);
        }
        #endregion

        #region General - Conversion
        public static double CelsiusToKelvin(double degreesCelsius)
        {
            return degreesCelsius + _kelvin;
        }
        public static double FahrenheitToKelvin(double degreesFahrenheit)
        {
            return CelsiusToKelvin(GlobalizationLayer.FahrenheitToCelsius(degreesFahrenheit));
        }
        #endregion

        #region General - Physics
        public static double Distance(double x1, double x2)
        {
            return Math.Abs(x2 - x1);
        }

        public static double VelocityAverage(double x1, double x2, TimeSpan timeSpan)
        {
            try
            {
                return Distance(x1, x2) / timeSpan.TotalSeconds;
            }
            catch (DivideByZeroException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region General DateTime
        public static int Days(DateTime startDate, DateTime endDate)
        {
            if (DateTime.Compare(startDate, endDate) == 0)
            {
                return 0;
            }

            return (endDate - startDate).Days;
        }

        public static int WeekDays(DateTime startDate, DateTime endDate)
        {
            int compareValue = DateTime.Compare(startDate, endDate);
            int count = default;
            if (compareValue == 0)
            {
                return 0;
            }
            else if (compareValue < 0)
            {
                DateTime tempDate = startDate;
                while (DateTime.Compare(tempDate, endDate) != 0)
                {
                    if (!tempDate.IsWeekend())
                    {
                        count++;
                    }

                    tempDate = tempDate.AddDays(1);
                }
            }
            else
            {
                DateTime tempDate = endDate;
                while (DateTime.Compare(tempDate, startDate) != 0)
                {
                    if (!tempDate.IsWeekend())
                    {
                        count++;
                    }

                    tempDate = tempDate.AddDays(1);
                }
            }

            return count;
        }

        public static DateTime ConvertToUTCDate(long milliseconds)
        {
            try
            {
                return new DateTime(milliseconds * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
            
        }
        #endregion

        #region General - Sorting
        #region BubbleSort
        public static void BubbleSort(decimal[] elements)
        {
            if (elements?.Length > 1)
            {
                DoBubbleSort(elements);
            }
        }

        public static void BubbleSort(double[] elements)
        {
            if (elements?.Length > 1)
            {
                DoBubbleSort(elements);
            }
        }

        public static void BubbleSort(int[] elements)
        {
            if (elements?.Length > 1)
            {
                DoBubbleSort(elements);
            }
        }

        private static void DoBubbleSort(decimal[] elements)
        {
            for (int i = elements.Length - 1; i > 0; i--)
            {
                for (int j = 0; j <= i - 1; j++)
                {
                    if (elements[j] > elements[j + 1])
                    {
                        decimal temp = elements[j];
                        elements[j] = elements[j + 1];
                        elements[j + 1] = temp;
                    }
                }
            }
        }

        private static void DoBubbleSort(double[] elements)
        {
            for (int i = elements.Length - 1; i > 0; i--)
            {
                for (int j = 0; j <= i - 1; j++)
                {
                    if (elements[j] > elements[j + 1])
                    {
                        double temp = elements[j];
                        elements[j] = elements[j + 1];
                        elements[j + 1] = temp;
                    }
                }
            }
        }

        private static void DoBubbleSort(int[] elements)
        {
            for (int i = elements.Length - 1; i > 0; i--)
            {
                for (int j = 0; j <= i - 1; j++)
                {
                    if (elements[j] > elements[j + 1])
                    {
                        int temp = elements[j];
                        elements[j] = elements[j + 1];
                        elements[j + 1] = temp;
                    }
                }
            }
        }
        #endregion

        #region InsertionSort
        public static void InsertionSort(decimal[] elements)
        {
            if (elements?.Length > 1)
            {
                DoInsertionSort(elements);
            }
        }

        public static void InsertionSort(double[] elements)
        {
            if (elements?.Length > 1)
            {
                DoInsertionSort(elements);
            }
        }

        public static void InsertionSort(int[] elements)
        {
            if (elements?.Length > 1)
            {
                DoInsertionSort(elements);
            }
        }

        private static void DoInsertionSort(decimal[] elements)
        {
            int i = 1;
            while (i < elements.Length)
            {
                int j = i;
                while (j > 0 && elements[j - 1] > elements[j])
                {
                    decimal temp = elements[j];
                    elements[j] = elements[j - 1];
                    elements[j - 1] = temp;

                    j--;
                }

                i++;
            }
        }

        private static void DoInsertionSort(double[] elements)
        {
            int i = 1;
            while (i < elements.Length)
            {
                int j = i;
                while (j > 0 && elements[j - 1] > elements[j])
                {
                    double temp = elements[j];
                    elements[j] = elements[j - 1];
                    elements[j - 1] = temp;

                    j--;
                }

                i++;
            }
        }

        private static void DoInsertionSort(int[] elements)
        {
            int i = 1;
            while (i < elements.Length)
            {
                int j = i;
                while (j > 0 && elements[j - 1] > elements[j])
                {
                    int temp = elements[j];
                    elements[j] = elements[j - 1];
                    elements[j - 1] = temp;

                    j--;
                }

                i++;
            }
        }
        #endregion

        #region MergeSort
        public static void MergeSort(decimal[] elements)
        {
            if (elements?.Length > 1)
            {
                DoMergeSort(elements, 0, elements.Length - 1);
            }
        }

        public static void MergeSort(double[] elements)
        {
            if (elements?.Length > 1)
            {
                DoMergeSort(elements, 0, elements.Length - 1);
            }
        }

        public static void MergeSort(int[] elements)
        {
            if (elements?.Length > 1)
            {
                DoMergeSort(elements, 0, elements.Length - 1);
            }
        }

        private static void DoMergeSort(decimal[] elements, int low, int high)
        {
            if (low < high)
            {
                int mid = (low + high) / 2;
                DoMergeSort(elements, low, mid);
                DoMergeSort(elements, mid + 1, high);
                DoMergeSort(elements, low, mid, high);
            }
        }

        private static void DoMergeSort(double[] elements, int low, int high)
        {
            if (low < high)
            {
                int mid = (low + high) / 2;
                DoMergeSort(elements, low, mid);
                DoMergeSort(elements, mid + 1, high);
                DoMergeSort(elements, low, mid, high);
            }
        }

        private static void DoMergeSort(int[] elements, int low, int high)
        {
            if (low < high)
            {
                int mid = (low + high) / 2;
                DoMergeSort(elements, low, mid);
                DoMergeSort(elements, mid + 1, high);
                DoMergeSort(elements, low, mid, high);
            }
        }

        private static void DoMergeSort(decimal[] elements, int low, int mid, int high)
        {
            int left = low;
            int right = mid + 1;
            decimal[] tempElements = new decimal[(high - low) + 1];
            int tempIndex = 0;

            while ((left <= mid) && (right <= high))
            {
                if (elements[left] < elements[right])
                {
                    tempElements[tempIndex] = elements[left];
                    left++;
                }
                else
                {
                    tempElements[tempIndex] = elements[right];
                    right++;
                }

                tempIndex++;
            }

            while (left <= mid)
            {
                tempElements[tempIndex] = elements[left];
                left++;
                tempIndex++;
            }

            while (right <= high)
            {
                tempElements[tempIndex] = elements[right];
                right++;
                tempIndex++;
            }

            for (int i = 0; i < tempElements.Length; i++)
            {
                elements[low + i] = tempElements[i];
            }
        }

        private static void DoMergeSort(double[] elements, int low, int mid, int high)
        {
            int left = low;
            int right = mid + 1;
            double[] tempElements = new double[(high - low) + 1];
            int tempIndex = 0;

            while ((left <= mid) && (right <= high))
            {
                if (elements[left] < elements[right])
                {
                    tempElements[tempIndex] = elements[left];
                    left++;
                }
                else
                {
                    tempElements[tempIndex] = elements[right];
                    right++;
                }

                tempIndex++;
            }

            while (left <= mid)
            {
                tempElements[tempIndex] = elements[left];
                left++;
                tempIndex++;
            }

            while (right <= high)
            {
                tempElements[tempIndex] = elements[right];
                right++;
                tempIndex++;
            }

            for (int i = 0; i < tempElements.Length; i++)
            {
                elements[low + i] = tempElements[i];
            }
        }

        private static void DoMergeSort(int[] elements, int low, int mid, int high)
        {
            int left = low;
            int right = mid + 1;
            int[] tempElements = new int[(high - low) + 1];
            int tempIndex = 0;

            while((left <= mid) && (right <= high))
            {
                if(elements[left] < elements[right])
                {
                    tempElements[tempIndex] = elements[left];
                    left++;
                }
                else
                {
                    tempElements[tempIndex] = elements[right];
                    right++;
                }

                tempIndex++;
            }

            while (left <= mid)
            {
                tempElements[tempIndex] = elements[left];
                left++;
                tempIndex++;
            }

            while (right <= high)
            {
                tempElements[tempIndex] = elements[right];
                right++;
                tempIndex++;
            }

            for(int i = 0; i < tempElements.Length; i++)
            {
                elements[low + i] = tempElements[i];
            }
        }
        #endregion

        #region QuickSort
        public static void QuickSort(decimal[] elements)
        {
            if (elements?.Length > 1)
            {
                DoQuickSort(elements, 0, elements.Length - 1);
            }
        }

        public static void QuickSort(double[] elements)
        {
            if (elements?.Length > 1)
            {
                DoQuickSort(elements, 0, elements.Length - 1);
            }
        }

        public static void QuickSort(int[] elements)
        {
            if (elements?.Length > 1)
            {
                DoQuickSort(elements, 0, elements.Length - 1);
            }
        }

        private static void DoQuickSort(decimal[] elements, int low, int high)
        {
            if (low < high)
            {
                int partitionIndex = DoQuickSortPartition(elements, low, high);
                DoQuickSort(elements, low, partitionIndex - 1);
                DoQuickSort(elements, partitionIndex + 1, high);
            }
        }

        private static void DoQuickSort(double[] elements, int low, int high)
        {
            if (low < high)
            {
                int partitionIndex = DoQuickSortPartition(elements, low, high);
                DoQuickSort(elements, low, partitionIndex - 1);
                DoQuickSort(elements, partitionIndex + 1, high);
            }
        }

        private static void DoQuickSort(int[] elements, int low, int high)
        {
            if (low < high)
            {
                int partitionIndex = DoQuickSortPartition(elements, low, high);
                DoQuickSort(elements, low, partitionIndex - 1);
                DoQuickSort(elements, partitionIndex + 1, high);
            }
        }

        private static int DoQuickSortPartition(decimal[] elements, int low, int high)
        {
            decimal pivot = elements[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (elements[j] < pivot)
                {
                    i++;

                    decimal temp = elements[j];
                    elements[j] = elements[i];
                    elements[i] = temp;
                }
            }

            if (elements[high] < elements[i + 1])
            {
                decimal temp = elements[high];
                elements[high] = elements[i + 1];
                elements[i + 1] = temp;
            }

            return i + 1;
        }

        private static int DoQuickSortPartition(double[] elements, int low, int high)
        {
            double pivot = elements[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (elements[j] < pivot)
                {
                    i++;

                    double temp = elements[j];
                    elements[j] = elements[i];
                    elements[i] = temp;
                }
            }

            if (elements[high] < elements[i + 1])
            {
                double temp = elements[high];
                elements[high] = elements[i + 1];
                elements[i + 1] = temp;
            }

            return i + 1;
        }

        private static int DoQuickSortPartition(int[] elements, int low, int high)
        {
            int pivot = elements[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (elements[j] < pivot)
                {
                    i++;

                    int temp = elements[j];
                    elements[j] = elements[i];
                    elements[i] = temp;
                }
            }

            if (elements[high] < elements[i + 1])
            {
                int temp = elements[high];
                elements[high] = elements[i + 1];
                elements[i + 1] = temp;
            }

            return i + 1;
        }
        #endregion

        #region SelectionSort
        public static void SelectionSort(decimal[] elements)
        {
            if (elements?.Length > 1)
            {
                DoSelectionSort(elements);
            }
        }

        public static void SelectionSort(double[] elements)
        {
            if (elements?.Length > 1)
            {
                DoSelectionSort(elements);
            }
        }

        public static void SelectionSort(int[] elements)
        {
            if (elements?.Length > 1)
            {
                DoSelectionSort(elements);
            }
        }

        private static void DoSelectionSort(decimal[] elements)
        {
            for (int i = 0; i < elements.Length - 1; i++)
            {
                int min = i;
                for (int j = i + 1; j < elements.Length; j++)
                {
                    if (elements[j] < elements[min])
                    {
                        min = j;
                    }
                }

                if (min != i)
                {
                    decimal temp = elements[i];
                    elements[i] = elements[min];
                    elements[min] = temp;
                }
            }
        }

        private static void DoSelectionSort(double[] elements)
        {
            for (int i = 0; i < elements.Length - 1; i++)
            {
                int min = i;
                for (int j = i + 1; j < elements.Length; j++)
                {
                    if (elements[j] < elements[min])
                    {
                        min = j;
                    }
                }

                if (min != i)
                {
                    double temp = elements[i];
                    elements[i] = elements[min];
                    elements[min] = temp;
                }
            }
        }

        private static void DoSelectionSort(int[] elements)
        {
            for (int i = 0; i < elements.Length - 1; i++)
            {
                int min = i;
                for (int j = i + 1; j < elements.Length; j++)
                {
                    if (elements[j] < elements[min])
                    {
                        min = j;
                    }
                }

                if (min != i)
                {
                    int temp = elements[i];
                    elements[i] = elements[min];
                    elements[min] = temp;
                }
            }
        }
        #endregion

        #region ShellSort
        public static void ShellSort(decimal[] elements)
        {
            if (elements?.Length > 1)
            {
                DoShellSort(elements);
            }
        }

        public static void ShellSort(double[] elements)
        {
            if (elements?.Length > 1)
            {
                DoShellSort(elements);
            }
        }

        public static void ShellSort(int[] elements)
        {
            if (elements?.Length > 1)
            {
                DoShellSort(elements);
            }
        }

        private static void DoShellSort(decimal[] elements)
        {
            int[] gaps = new int[] { 701, 301, 132, 57, 23, 10, 4, 1 };

            foreach (int gap in gaps)
            {
                for (int i = gap; i < elements.Length; i++)
                {
                    decimal temp = elements[i];

                    int j = default;
                    for (j = i; j >= gap && elements[j - gap] > temp; j -= gap)
                    {
                        elements[j] = elements[j - gap];
                    }

                    elements[j] = temp;
                }
            }
        }

        private static void DoShellSort(double[] elements)
        {
            int[] gaps = new int[] { 701, 301, 132, 57, 23, 10, 4, 1 };

            foreach (int gap in gaps)
            {
                for (int i = gap; i < elements.Length; i++)
                {
                    double temp = elements[i];

                    int j = default;
                    for (j = i; j >= gap && elements[j - gap] > temp; j -= gap)
                    {
                        elements[j] = elements[j - gap];
                    }

                    elements[j] = temp;
                }
            }
        }

        private static void DoShellSort(int[] elements)
        {
            int[] gaps = new int[] { 701, 301, 132, 57, 23, 10, 4, 1 };

            foreach (int gap in gaps)
            {
                for (int i = gap; i < elements.Length; i++)
                {
                    int temp = elements[i];

                    int j = default;
                    for (j = i; j>= gap && elements[j - gap] > temp; j -= gap)
                    {
                        elements[j] = elements[j - gap];
                    }

                    elements[j] = temp;
                }
            }
        }
        #endregion
        #endregion

        #region Random Number Generator
        public static int RandomInteger()
        {
            return _random.Next();
        }

        public static int RandomInteger(int maxValue)
        {
            try
            {
                return _random.Next(maxValue + 1);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static int RandomInteger(int minValue, int maxValue)
        {
            try
            {
                return _random.Next(minValue, maxValue + 1);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }

        public static void RandomIntegers(int[] values)
        {
            if (values?.Length < 1)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = RandomInteger();
                }
            }
        }

        public static void RandomIntegers(int[] values, int maxValue)
        {
            if (values?.Length < 1 && maxValue <= 0)
            {
                maxValue = maxValue + 1;

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = RandomInteger(maxValue);
                }
            }
        }

        public static void RandomIntegers(int[] values, int minValue, int maxValue)
        {
            if (values?.Length < 1 && minValue > maxValue)
            {
                maxValue = maxValue + 1;
            
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = RandomInteger(minValue, maxValue);
                }
            }
        }

        public static double RandomDouble()
        {
            return _random.NextDouble();
        }

        public static void RandomDoubles(double[] values)
        {
            if (values?.Length < 1)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = RandomDouble();
                }
            }
        }

        public static void RandomBytes(byte[] byteBuffer)
        {
            try
            {
                _random.NextBytes(byteBuffer);
            }
            catch (ArgumentNullException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        #region Algorithm
        public static int[] SieveOfAtkin(int limit)
        {
            List<int> resultList = new List<int>();

            if (limit < 1)
            {
                return resultList.ToArray();
            }
            else if (limit >= 1)
            {
                resultList.Add(2);

                if (limit == 1)
                {
                    return resultList.ToArray();
                }
            }
            else if (limit >= 2)
            {
                resultList.Add(3);

                if (limit == 2)
                {
                    return resultList.ToArray();
                }
            }
            else if (limit >= 3)
            {
                resultList.Add(5);

                if (limit == 3)
                {
                    return resultList.ToArray();
                }
            }

            Dictionary<int, bool> sieveList = new Dictionary<int, bool>();
            for (int i = 5; i <= limit; i++)
            {
                sieveList.Add(i, false);
            }

            double squareLimit = Math.Sqrt(limit);
            for (int i = 0; i < sieveList.Count; i++)
            {
                KeyValuePair<int, bool> entry = sieveList.ElementAt(i);
                int remainder = entry.Key % 60;

                switch (remainder)
                {
                    case 1:
                    case 13:
                    case 17:
                    case 29:
                    case 37:
                    case 41:
                    case 49:
                    case 53:
                        for (int x = 1; x < squareLimit; x++)
                        {
                            for (int y = 1; y < squareLimit; y++)
                            {
                                double z = 4 * Math.Pow(x, 2) + Math.Pow(y, 2);

                                if ((z % 1 == 0) && ((double)entry.Key == z))
                                {
                                    sieveList[entry.Key] = !sieveList[entry.Key];
                                }
                            }
                        }
                        break;
                    case 7:
                    case 19:
                    case 31:
                    case 43:
                        for (int x = 1; x < squareLimit; x++)
                        {
                            for (int y = 1; y < squareLimit; y++)
                            {
                                double z = 3 * Math.Pow(x, 2) + Math.Pow(y, 2);

                                if ((z % 1 == 0) && ((double)entry.Key == z))
                                {
                                    sieveList[entry.Key] = !sieveList[entry.Key];
                                }
                            }
                        }
                        break;
                    case 11:
                    case 23:
                    case 47:
                    case 59:
                        for (int x = 1; x < squareLimit; x++)
                        {
                            for (int y = 1; (y < squareLimit) && (x > y); y++)
                            {
                                double z = 3 * Math.Pow(x, 2) - Math.Pow(y, 2);

                                if ((z % 1 == 0) && ((double)entry.Key == z))
                                {
                                    sieveList[entry.Key] = !sieveList[entry.Key];
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            foreach (KeyValuePair<int, bool> entry in sieveList)
            {
                if (entry.Value)
                {
                    resultList.Add(entry.Key);
                }
            }

            return resultList.ToArray();
        }

        public static bool LuhnAlgorithm(string idNumber)
        {
            char[] digitChars = idNumber.ToCharArray();
            List<int> digits = new List<int>();

            for (int i = 0; i < digitChars.Length; i++)
            {
                if (char.IsDigit(digitChars[i]))
                {
                    digits.Add((int)char.GetNumericValue(digitChars[i]));
                }
                else
                {
                    return false;
                }
            }

            int checkSum = digits[digits.Count - 1];
            digits.RemoveAt(digits.Count - 1);

            digits.Reverse();
            int counter = 1;
            int sum = 0;

            List<int> checkedAdd = new List<int>();

            foreach (int singleDigit in digits)
            {
                if (IsEven(counter))
                {
                    sum += singleDigit;
                    checkedAdd.Add(singleDigit);
                }
                else
                {
                    int checkDigit = 2 * singleDigit;

                    if (checkDigit >= 10)
                    {
                        int tensDigit = checkDigit / 10;
                        int onesDigit = checkDigit % 10;

                        checkDigit = tensDigit + onesDigit;
                    }

                    sum += checkDigit;

                    checkedAdd.Add(checkDigit);
                }

                counter++;
            }

            return sum % 10 == checkSum;
        }

        public static bool LuhnAlgorithm(double idNumber)
        {
            if (idNumber != Math.Abs(idNumber))
            {
                throw new ArgumentException("Only whole numbers.");
            }

            return LuhnAlgorithm(idNumber.ToString());
        }

        public static int FibonacciNumber(int n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException("n should be zero or greater.");
            }

            int fibonacciNumber = default;

            if (n == 0) { fibonacciNumber = 0; }
            else if (n == 1) { fibonacciNumber = 1; }
            else
            {
                fibonacciNumber = FibonacciNumber(n - 1) + FibonacciNumber(n - 2);
            }

            return fibonacciNumber;
        }

        public static int[] FibonacciSequence(int n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException("n should be zero or greater.");
            }

            int index = 0;

            int[] fibonacciSequence = new int[n];

            if (n >= 0)
            {
                fibonacciSequence[index] = 0;
            }

            if (n >= 1)
            {
                index++;
                fibonacciSequence[index] = 1;
            }

            while (index >= 1 && index < n - 1)
            {
                index++;
                fibonacciSequence[index] = fibonacciSequence[index - 1] + fibonacciSequence[index - 2];
            }

            return fibonacciSequence;
        }

        public static int KadanesAlgorithm(int[] elements, int n)
        {
            int currentMax = 0, loopMax = 0;

            for (int i = 0; i < n; i++)
            {
                loopMax = loopMax + elements[i];
                loopMax = loopMax > 0 ? loopMax : 0;
                currentMax = currentMax > loopMax ? currentMax : loopMax;
            }

            return currentMax;
        }
        #endregion

        #region Accounting
        #region Fixed APR
        public static decimal FixedAnnualPayment(decimal principal, decimal annualInterestRate, int years)
        {
            return FixedPayment(principal, annualInterestRate, _annual, years);
        }

        public static decimal FixedSemiAnnualPayment(decimal principal, decimal annualInterestRate, int years)
        {
            return FixedPayment(principal, annualInterestRate, semiAnnual, years);
        }

        public static decimal FixedQuarterlyPayment(decimal principal, decimal annualInterestRate, int years)
        {
            return FixedPayment(principal, annualInterestRate, _quarterly, years);
        }

        public static decimal FixedMonthlyPayment(decimal principal, decimal annualInterestRate, int years)
        {
            return FixedPayment(principal, annualInterestRate, _monthly, years);
        }

        public static decimal FixedDailyPayment(decimal principal, decimal annualInterestRate, int years)
        {
            return FixedPayment(principal, annualInterestRate, _daily, years);
        }

        private static decimal FixedPayment(decimal principal, decimal annualInterestRate, int compoundingFrequency, int years)
        {
            decimal periodInterestRate = annualInterestRate / compoundingFrequency;
            decimal numberOfPayments = compoundingFrequency * years;
            decimal effectiveInterestRate = Pow(decimal.One + periodInterestRate, numberOfPayments);

            try
            {
                return principal * ((periodInterestRate * effectiveInterestRate) / (effectiveInterestRate - decimal.One));
            }
            catch (DivideByZeroException exc)
            {
                ExceptionLayer.Handle(exc);
                throw;
            }
        }
        #endregion

        public static decimal CashRatio(decimal cash, decimal marketableSecurities, decimal currentLiabilities)
        {
            return cash + marketableSecurities / currentLiabilities;
        }

        public static decimal CompoundInterest(decimal principal, decimal annualInterestRate, int timesAnnuallyCompounded, decimal years)
        {
            if (timesAnnuallyCompounded == 0)
            {
                return default;
            }

            try
            {
                return principal * Convert.ToDecimal(Math.Pow((double)(decimal.One + annualInterestRate / timesAnnuallyCompounded), (double)(timesAnnuallyCompounded * years)));
            }
            catch (OverflowException exc)
            {
                ExceptionLayer.Handle(exc);
                return default;
            }
        }

        public static decimal CompoundInterestTotal(decimal principal, decimal annualInterestRate, int timesAnnuallyCompounded, decimal years)
        {
            if (timesAnnuallyCompounded == 0)
            {
                return default;
            }

            return CompoundInterest(principal, annualInterestRate, timesAnnuallyCompounded, years) - principal;
        }

        public static decimal EffectiveInterestRate(decimal annualInterestRate, int compoundingFrequency)
        {
            if (compoundingFrequency > _daily)
            {
                return (decimal)Math.Exp((double)annualInterestRate) - decimal.One;
            }

            return Pow(decimal.One + (annualInterestRate / compoundingFrequency), compoundingFrequency) - decimal.One;
        }

        public static decimal OperatingIncome(decimal grossProfit, decimal sellingExpenses, decimal generalExpenses, decimal administrativeExpenses)
        {
            return grossProfit - sellingExpenses - generalExpenses - administrativeExpenses;
        }

        public static decimal ProfitMargin(decimal revenue, decimal costOfGoodsSold)
        {
            if (revenue == decimal.Zero) { return decimal.Zero; }
            return (revenue - costOfGoodsSold) / revenue;
        }

        public static decimal SimpleInterest(decimal principal, decimal annualInterestRate, decimal years)
        {
            return principal * (decimal.One + annualInterestRate * years);
        }
        #endregion
    }
}