using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Project_Data_Mining.ObjectClass
{
    public static class CSVPreprocessor
    {
        public static DataTable CSVtoDataTable(string strFilePath, ref DataTable testSet, Random rnd)
        {
            DataTable dt = new DataTable();
            var lines = File.ReadAllLines(strFilePath).ToList();

            // Preserve coma inside quotes
            for (int i = 0; i < lines.Count; i++)
            {
                var s = lines[i];
                if (Regex.IsMatch(s, "\"[^\"]+\""))
                {
                    foreach (Match m in Regex.Matches(s, "\"[^\"]+\""))
                    {
                        var ori = m.Value;
                        var replace = ori.Replace(",", "_COMA_").Replace("\"", "");
                        lines[i] = s.Replace(ori, replace);
                    }
                }
            }

            // get headers and exclude header row
            string[] headers = lines[0].Split(',');
            lines.RemoveAt(0);

            // Randomize data order, using Fisher-Yate algorithm
            int n = lines.Count;
            for (int i = 0; i < n; i++)
            {
                int cc = i + rnd.Next(n - i);
                string t = lines[cc];
                lines[cc] = lines[i];
                lines[i] = t;
            }

            string[,] tableVals = new string[lines.Count, headers.Length];
            string[] modes = new string[headers.Length];

            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            int r = 0;
            for (int q = 0; q < lines.Count; q++)
            {
                string[] rowVals = lines[q].Split(',');
                for (int i = 0; i < headers.Length; i++)
                {
                    tableVals[q, i] = rowVals[i].Replace("_COMA_", ",");
                }
                r++;
            }
            
            // Calculate mode for each column, for missing data subtitution
            for (int i = 0; i < headers.Length; i++)
            {
                string[] vals = new string[tableVals.GetLength(0)];
                for (int j = 0; j < tableVals.GetLength(0); j++)
                {
                    vals[j] = tableVals[j, i];
                }
                modes[i] = vals.Where(a => a != "?").GroupBy(a => a).OrderByDescending(a => a.Count()).First().Key;
            }

            // Replace missing data with Mode
            int l = tableVals.GetLength(0);
            for (int i = 0; i < l; i++)
            {
                DataRow dr = dt.NewRow();
                for (int j = 0; j < headers.Length; j++)
                {
                    var s = tableVals[i, j];
                    if (IsMissingValue(s))
                    {
                        s = modes[j];
                    }
                    dr[j] = s;
                }
                dt.Rows.Add(dr);
            }

            // Take 30% as Test Set, return the rest
            testSet = dt.Clone();
            var count = (int)Math.Round(dt.Rows.Count * 0.3, 0);
            for (int i = 0; i < count; i++)
            {
                testSet.Rows.Add(dt.Rows[i].ItemArray);
                dt.Rows.RemoveAt(i);
            }

            // Create descriptor for numerical values
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var distinctVals = Feature.GetDistinctValuesOfColumn(dt, i);
                if (distinctVals.Count > 10 && distinctVals.All(a => double.TryParse(a, out double ou)))
                {
                    // values are numerical, create descriptor
                    var descriptor = CategoricalFactory.GenerateEqualWidthBins(dt.Columns[i].ColumnName, distinctVals.Select(a => double.Parse(a)).ToArray());
                    for (int s = 0; s < dt.Rows.Count; s++)
                    {
                        dt.Rows[s][i] = descriptor.DescriptNumericalValue(dt.Rows[s][i].ToString());
                    }
                }
            }

            return dt;
        }

        private static bool IsMissingValue(string s)
        {
            return s.Contains("?") || s.Replace(" ", "") == "";
        }
    }
}
