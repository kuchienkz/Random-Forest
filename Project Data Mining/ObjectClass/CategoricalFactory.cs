using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Data_Mining.ObjectClass
{
    public static class CategoricalFactory
    {
        public static SortedSet<NumericalDescriptor> Descriptors;

        static CategoricalFactory()
        {
            Descriptors = new SortedSet<NumericalDescriptor>();
        }

        public static bool IsDescriptorExists(string columnName)
        {
            return Descriptors.DefaultIfEmpty(null).FirstOrDefault(a => a.ColumName == columnName) != null;
        }

        public static string DescriptNumericalValue(string value, string columnName)
        {
            return Descriptors.First(a => a.ColumName.Equals(columnName)).DescriptNumericalValue(value);
        }

        public static NumericalDescriptor GenerateEqualWidthBins(string columnName, params double[] vals)
        {
            int numOfBin = (int)Math.Ceiling(vals.Length / 7d);
            return GenerateEqualWidthBins(numOfBin, columnName, vals);
        }

        public static NumericalDescriptor GenerateEqualWidthBins(int numOfBin, string columnName, params double[] vals)
        {
            double max = Math.Floor(vals.Max()), min = Math.Floor(vals.Min());
            double width = Math.Floor((max - min) / numOfBin);

            var bins = new Bin[numOfBin];
            bins[0] = new Bin(double.MinValue, min);
            var lastMax = 0.0;
            for (int i = 1; i < bins.Length - 1; i++)
            {
                var newMin = bins[i - 1].MAX;
                bins[i] = new Bin(newMin, newMin + width);
                lastMax = newMin + width;
            }
            bins[bins.Length - 1] = new Bin(lastMax, double.MaxValue);

            var dsc = new NumericalDescriptor(bins, columnName);
            Descriptors.Add(dsc);

            return dsc;
        }

        public class Bin
        {
            public double MIN, MAX;
            public Bin(double min, double max)
            {
                MIN = min;
                MAX = max;
            }

            public bool IsMatch(double input)
            {
                return input > MIN & input <= MAX;
            }

            public string Label
            {
                get
                {
                    var s = "";
                    if (MIN == double.MinValue)
                    {
                        s = "[LOWER THAN " + Math.Floor(MAX) + "]";
                    }
                    else if (MAX == double.MaxValue)
                    {
                        s = "[MORE THAN " + Math.Floor(MIN) + "]";
                    }
                    else
                    {
                        s = "[BETWEEN " + (Math.Floor(MIN) + 1) + " and " + Math.Floor(MAX) + "]";
                    }
                    return s;
                }
            }
        }

        public class NumericalDescriptor : IComparable
        {
            public Bin[] Categories;
            public string ColumName;

            public NumericalDescriptor(Bin[] bins, string columnName)
            {
                Categories = bins;
                ColumName = columnName;
            }

            public int CompareTo(object obj)
            {
                return ColumName.CompareTo(((NumericalDescriptor)obj).ColumName);
            }

            public string DescriptNumericalValue(double value)
            {
                foreach (var bin in Categories)
                {
                    if (bin.IsMatch(value))
                    {
                        return bin.Label;
                    }
                }

                return "NOT_FOUND"; // should be impossible;
            }

            public string DescriptNumericalValue(string value)
            {
                return DescriptNumericalValue(double.Parse(value));
            }
        }
    }
}
