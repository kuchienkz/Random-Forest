using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Project_Data_Mining.ObjectClass;
using Attribute = Project_Data_Mining.ObjectClass.Attribute;

namespace Project_Data_Mining
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RandomForest forest;
        DataTable dataset;
        Random r;

        public MainWindow()
        {
            InitializeComponent();
            r = new Random();
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            dataset = CSVtoDataTable(System.Environment.CurrentDirectory + @"\suicide.csv");
            forest = new RandomForest(dataset, 500, 500);
        }

        private void Test2_Click(object sender, RoutedEventArgs e)
        {
            var idx = r.Next(dataset.Rows.Count);
            Console.WriteLine("Testing data #" + (idx + 1));
            Console.WriteLine("\nRESULT:" + forest.Predict(dataset.Rows[idx]) + "\n\n");
        }

        private DataTable CSVtoDataTable(string strFilePath)
        {
            DataTable dt = new DataTable();
            var lines = File.ReadAllLines(strFilePath);

            // Preserve coma inside quotes
            for (int i = 0; i < lines.Length; i++)
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

            string[] headers = lines[0].Split(',');
            string[,] tableVals = new string[lines.Length - 1, headers.Length];
            string[] modes = new string[headers.Length];

            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            int r = 0;
            for (int q = 1; q < lines.Length; q++)
            {
                string[] rowVals = lines[q].Split(',');
                for (int i = 0; i < headers.Length; i++)
                {
                    tableVals[q-1,i] = rowVals[i];
                }
                r++;
            }

            // Calculate mode for each column
            for (int i = 0; i < headers.Length; i++)
            {
                string[] vals = new string[tableVals.GetLength(0)];
                for (int j = 0; j < tableVals.GetLength(0); j++)
                {
                    vals[j] = tableVals[j, i];
                }
                modes[i] = vals.Where(a => a != "?").GroupBy(a => a).OrderByDescending(a => a.Count()).First().Key;
            }

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

            return dt;
        }

        private bool IsMissingValue(string s)
        {
            return s.Contains("?")
                || s.Replace(" ", "") == "";
        }
        
    }
}