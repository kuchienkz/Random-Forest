using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Data_Mining.ObjectClass
{
    public class CategoricalFactory
    {
        public List<EqualWidthBin> Bins;
        
        public static EqualWidthBin[] GenerateEqualWidthBins(DataTable dt, int columnIndex, int numberOfBin)
        {
            var rowCount = dt.Rows.Count;

            var w = new double[rowCount];
            var splitCandidates = new double[rowCount - 1];
            for (int i = 0; i < rowCount; i++)
            {
                w[i] = double.Parse((string)dt.Rows[i][columnIndex]);
            }

            double min = w.Min(), max = w.Max();
            double width = (max - min) / numberOfBin;

            var bins = new List<EqualWidthBin>(numberOfBin);

            var interval = new double[numberOfBin * 2];
            interval[0] = min;
            interval[1] = min + width;
            for (int i = 2; i < interval.Length - 1; i += 2)
            {
                interval[i] = interval[i - 1];
                interval[i + 1] = interval[i] + width;
            }
            interval[0] = double.MinValue;
            interval[interval.Length - 1] = double.MaxValue;

            for (int i = 0; i < interval.Length; i+=2)
            {
                bins.Add(new EqualWidthBin(interval[i], interval[i + 1]));
            }

            return bins.ToArray();
        }


        public class EqualWidthBin
        {
            public double MIN, MAX;

            public EqualWidthBin(double minimum, double maximum)
            {
                MIN = minimum;
                MAX = maximum;
            }

            public bool Check(string input)
            {
                double ou = 0;
                return Check(double.TryParse(input, out ou) ? ou : 0);
            }
            public bool Check(double input)
            {
                return input >= MIN && input <= MAX;
            }

            public string GetLabel()
            {
                var s = "";
                if (MIN == double.MinValue)
                {
                    s = "< " + MAX;
                }
                else if (MAX == double.MaxValue)
                {
                    s = MIN + " >";
                }
                else
                {
                    s = MIN + " <--> " + MAX;
                }

                return s;
            }
        }


        //public static List<EqualWidthBin> GenerateBins(DataTable oriSet, int columnIndex, double baseEntropy, int maxLevel, int level)
        //{
        //    var rowCount = oriSet.Rows.Count;
        //    var dataSetLabels =
        //        Attribute.GetDistinctAttributeValuesOfColumn(oriSet, oriSet.Columns.Count - 1);

        //    var w = new double[rowCount];
        //    var splitCandidates = new double[rowCount - 1];
        //    for (int i = 0; i < rowCount; i++)
        //    {
        //        w[i] = double.Parse((string)oriSet.Rows[i][columnIndex]);
        //    }
        //    w = w.OrderBy(a => a).ToArray();
        //    for (int i = 0; i < w.Length - 1; i++)
        //    {
        //        splitCandidates[i] = (w[i] + w[i + 1]) / 2;
        //    }
        //    splitCandidates = splitCandidates.Distinct().ToArray();

        //    List<EqualWidthBin> bins = new List<EqualWidthBin>(5);

        //    for (int k = 0; k < 5; k++)
        //    {

        //        var SplitValue_InfoGains_pair = new List<KeyValuePair<double, double>>(w.Length - 1); // key: splitValue || value: InfoGain
        //        for (int i = 0; i < splitCandidates.Length; i++)
        //        {
        //            var label_lessOrEqualThanCount = new int[dataSetLabels.Count];
        //            var label_moreThanCount = new int[dataSetLabels.Count];

        //            for (int r = 0; r < w.Length; r++)
        //            {
        //                for (int q = 0; q < dataSetLabels.Count; q++)
        //                {
        //                    if ((oriSet.Rows[r][oriSet.Columns.Count - 1].ToString() == dataSetLabels[q]))
        //                    {
        //                        // less than or equal
        //                        if (w[r] <= splitCandidates[i])
        //                        {
        //                            label_lessOrEqualThanCount[q]++;
        //                        }
        //                        // more than
        //                        else if (w[r] > splitCandidates[i])
        //                        {
        //                            label_moreThanCount[q]++;
        //                        }
        //                    }
        //                }
        //            }

        //            var qq = label_lessOrEqualThanCount.Sum();
        //            var entropy_LessOrEqualThan =
        //                Tree.CalculateEntropy(label_lessOrEqualThanCount.Select(a => (double)a / qq).ToArray());
        //            var ww = label_moreThanCount.Sum();
        //            var entropy_MoreThan =
        //                Tree.CalculateEntropy(label_moreThanCount.Select(a => (double)a / ww).ToArray());

        //            var total = label_lessOrEqualThanCount.Sum() + label_moreThanCount.Sum();

        //            var netEntropy = ((label_lessOrEqualThanCount.Sum() / (double)total) * entropy_LessOrEqualThan)
        //                + ((label_moreThanCount.Sum() / (double)total) * entropy_MoreThan);

        //            SplitValue_InfoGains_pair
        //                .Add(new KeyValuePair<double, double>(
        //                    splitCandidates[i],
        //                    baseEntropy - netEntropy));
        //        }

        //        SplitValue_InfoGains_pair = SplitValue_InfoGains_pair
        //            .OrderByDescending(a => a.Value)
        //            .ToList(); // order by InfoGain, high to low

        //        var highestSplit = SplitValue_InfoGains_pair[0].Key;

        //        GenerateBins(oriSet)
        //    }

        //    return bins;
        //}
    }
}
