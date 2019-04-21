using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Project_Data_Mining.ObjectClass;

namespace Project_Data_Mining
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RandomForest forest;
        DataTable TrainingSet;
        DataTable TestSet;
        Random r;
        string filepath = "";
        OpenFileDialog fd;

        public MainWindow()
        {
            r = new Random();

            fd = new OpenFileDialog();
            fd.Filter = "CSV file (*.csv)|*.csv";
            fd.InitialDirectory = Environment.CurrentDirectory;

            InitializeComponent();

            var path = Path.Combine(Environment.CurrentDirectory, "bg.jpg");
            var uri = new Uri(path);
            this.Background = new ImageBrush(new BitmapImage(uri));
        }

        private void Test2_Click(object sender, RoutedEventArgs e)
        {
            var idx = r.Next(TrainingSet.Rows.Count);
            Console.WriteLine("Testing data #" + (idx + 1));
            Console.WriteLine("\nRESULT:" + forest.Predict(TrainingSet.Rows[idx]) + "\n\n");
        }

        private DataTable CSVtoDataTable(string strFilePath)
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
                int cc = i + this.r.Next(n - i);
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
                    tableVals[q,i] = rowVals[i].Replace("_COMA_", ",");
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
            TestSet = dt.Clone();
            var count = (int)Math.Round(dt.Rows.Count * 0.3, 0);
            for (int i = 0; i < count; i++)
            {
                TestSet.Rows.Add(dt.Rows[i].ItemArray);
                dt.Rows.RemoveAt(i);
            }

            return dt;
        }

        private bool IsMissingValue(string s)
        {
            return s.Contains("?")
                || s.Replace(" ", "") == "";
        }

        private void Tbx_numTrees_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]");
        }

        private void Tbx_dataset_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (fd.ShowDialog() == true)
            {
                try
                {
                    var testDt = CSVtoDataTable(fd.FileName);
                    tbx_numSamples.Text = Math.Round(testDt.Rows.Count * 0.7, 0).ToString();
                    tbx_dataset.Text = Path.GetFileName(fd.FileName);
                    filepath = fd.FileName;
                    
                    btn_rebuild.IsEnabled = true;
                }
                catch (Exception er)
                {
                    MessageBox.Show("Failed to load CSV file. Try another file.\n\nError msg: " + er.Message);
                }
            }
        }

        private void GenerateUI()
        {
            // Generate Input Controls 
            sp_inputs.Children.Clear();
            int tabIndex = 30;

            for (int c = 0; c < TrainingSet.Columns.Count - 1; c++)
            {
                var a = TrainingSet.Columns[c];
                var tb = new TextBlock();
                tb.Text = a.Caption.ToUpper() + ":";
                tb.FontSize = 15;

                Control cn;
                var distinctValues = TrainingSet.AsEnumerable().Select(q => q.Field<string>(a.ColumnName)).OrderBy(q => q).Distinct().ToList();
                if (distinctValues.All(x => !Regex.IsMatch(x, "[A-Za-z]")))
                {
                    cn = new TextBox();
                    var tip = "Example of valid value:";
                    int tipCount = 0;
                    foreach (var d in distinctValues)
                    {
                        tip += "\n" + d;
                        tipCount++;
                        if (tipCount > 6)
                        {
                            break;
                        }
                    }
                    cn.ToolTip = tip;
                    ((TextBox)cn).FontSize = 15;
                    ((TextBox)cn).HorizontalAlignment = HorizontalAlignment.Stretch;
                }
                else
                {
                    cn = new ComboBox();
                    foreach (var da in distinctValues)
                    {
                        ((ComboBox)cn).Items.Add(da);
                    }
                    ((ComboBox)cn).SelectedIndex = 0;
                    ((ComboBox)cn).FontSize = 15;
                    ((ComboBox)cn).HorizontalAlignment = HorizontalAlignment.Stretch;
                }
                cn.MinWidth = 150;
                cn.Height = 30;
                cn.Background = new SolidColorBrush(Color.FromRgb(206, 255, 147));
                cn.HorizontalAlignment = HorizontalAlignment.Left;
                cn.Margin = new Thickness(0, 10, 0, 12);
                cn.TabIndex = tabIndex++;

                sp_inputs.Children.Add(tb);
                sp_inputs.Children.Add(cn);
            }
        }

        private void Btn_rebuild_Click(object sender, RoutedEventArgs e)
        {
            if (filepath == "")
            {
                MessageBox.Show("Click on the green box above to open CSV file.", "CSV File not set", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (forest != null)
            {
                if (MessageBox.Show("This will create new forest, replacing the previous one.", "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                {
                    return;
                }
            }

            btn_rebuild.IsEnabled = false;

            TrainingSet = CSVtoDataTable(filepath);
            var nTree = int.Parse(tbx_numTrees.Text.Replace(" ", "").TrimStart('0'));
            var nSample = int.Parse(tbx_numSamples.Text.Replace(" ", "").TrimStart('0'));
            RandomForest.OnCultivateStart += Forest_OnCultivateStart;
            forest = new RandomForest(TrainingSet, nTree, nSample);
            forest.OnCultivateFinished += Forest_OnCultivateFinished;
            forest.OnCultivateProgress += Forest_OnCultivateProgress;
            forest.OnVotingProgress += Forest_OnVotingProgress;
        }

        private void Forest_OnVotingProgress(int voteDone, double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                txt_btm.Text = "Collecting votes..." + voteDone + "/" + tbx_numTrees.Text + "  ( " + Math.Round(percentage, 0).ToString() + "% )";
            });
        }

        private void Forest_OnCultivateProgress(int treeDone, double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                txt_btm.Text = "Generating decision tree..." + treeDone + "/" + tbx_numTrees.Text + "  ( " + Math.Round(percentage, 0) + "% )";
            });
        }
        
        private void Forest_OnCultivateFinished(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txt_main.Text = "One sec...";
                txt_btm.Text = "Generating UI...";
                GenerateUI();

                txt_main.Text = "Forest Ready";
                txt_btm.Text = "Waiting for test input...";

                GridView gv = (GridView)list_testSet.View;
                while (gv.Columns.Count > 1)
                {
                    gv.Columns.RemoveAt(0);
                }
                for (int i = 0; i < TestSet.Columns.Count; i++)
                {
                    var column = TestSet.Columns[i];
                    gv.Columns.Insert(i, new GridViewColumn() { Header = column.Caption.ToUpper(), DisplayMemberBinding = new Binding(column.ColumnName) });
                }
                list_testSet.View = gv;
                list_testSet.ItemsSource = TestSet.DefaultView;

                Console.WriteLine("Forest succesfully created from " + filepath);
            });
        }

        private void Forest_OnCultivateStart(object sender, EventArgs e)
        {
            btn_rebuild.IsEnabled = false;
            txt_main.Text = "Growing trees...";
            txt_btm.Text = "...";
        }

        private void Tbx_numTrees_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (btn_rebuild == null)
            {
                return;
            }
            btn_rebuild.IsEnabled = true;
        }

        private void Test_input_Click(object sender, RoutedEventArgs e)
        {
            if (TrainingSet == null)
            {
                return;
            }
            txt_main.Text = "...";
            txt_btm.Text = "Getting inputs...";

            List<string> inputs = new List<string>(TrainingSet.Columns.Count);
            foreach (var c in sp_inputs.Children)
            {
                var t = c.GetType();
                if (t == typeof(TextBlock))
                {
                    continue;
                }
                else if (t == typeof(TextBox))
                {
                    inputs.Add(((TextBox)c).Text);
                }
                else // Combobox
                {
                    inputs.Add(((ComboBox)c).Text);
                }
            }

            txt_main.Text = "Taking votes...";
            txt_btm.Text = "...";

            var strc = TrainingSet.Clone();
            strc.Rows.Add(inputs.ToArray());

            forest.Predict(strc.Rows[0]).ContinueWith((task) => 
            {
                if (task.Result == "")
                {
                    Dispatcher.Invoke(() =>
                    {
                        txt_main.Text = "Failed! Grow more trees!";
                        txt_btm.Text = "Waiting for input...";
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        txt_main.Text = task.Result;
                        txt_btm.Text = "Waiting for test input..." ;
                    });
                }
            });
        }
    }
}