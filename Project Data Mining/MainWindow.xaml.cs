using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        Random random;
        string filepath = "";
        OpenFileDialog fd;
        bool IsCheckingAccuracy = false;

        public MainWindow()
        {
            random = new Random();

            fd = new OpenFileDialog();
            fd.Filter = "CSV file (*.csv)|*.csv";
            fd.InitialDirectory = Environment.CurrentDirectory;

            InitializeComponent();

            var path = Path.Combine(Environment.CurrentDirectory, "bg.jpg");
            var uri = new Uri(path);
            this.Background = new ImageBrush(new BitmapImage(uri));
        }
        
        // Random Forest class events
        private void Forest_OnVotingProgress(int voteDone, double percentage)
        {
            if (!IsCheckingAccuracy)
            {
                Dispatcher.Invoke(() =>
                {
                    txt_btm.Text = "Collecting votes..." + voteDone + "/" + tbx_numTrees.Text;
                    txt_main.Text = "Taking votes " + Math.Round(percentage, 0).ToString() + "%";
                });
            }
        }

        private void Forest_OnCultivateProgress(int treeDone, double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                txt_btm.Text = "Generating decision tree..." + treeDone + "/" + tbx_numTrees.Text;
                txt_main.Text = "Growing trees " + Math.Round(percentage, 0) + "%";
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

                test_input.IsEnabled = true;
                Console.WriteLine("Forest succesfully created from " + filepath);
            });
        }

        private void Forest_OnCultivateStart(object sender, EventArgs e)
        {
            list_testSet.ItemsSource = null;
            btn_rebuild.IsEnabled = false;
            test_input.IsEnabled = false;
            txt_main.Text = "Growing trees...";
            txt_btm.Text = "...";
        }
        
        // Main class events
        private void GenerateUI()
        {
            // Generate Input Controls 
            sp_inputs.Children.Clear();
            int tabIndex = 30;

            for (int c = 0; c < TestSet.Columns.Count - 1; c++)
            {
                var a = TestSet.Columns[c];
                var tb = new TextBlock();
                tb.Text = a.Caption.ToUpper() + ":";
                tb.FontSize = 15;

                Control cn;
                var distinctValues = TestSet.AsEnumerable().Select(q => q.Field<string>(a.ColumnName)).OrderBy(q => q).Distinct().ToList();
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
                    ((ComboBox)cn).IsEditable = true;
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



        // User-control interaction events
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

        private void Lvi_action_Click(object sender, RoutedEventArgs e)
        {
            var dataRow = (DataRowView)((Button)sender).DataContext;
            list_testSet.SelectedIndex = list_testSet.Items.IndexOf(dataRow);

            var vals = dataRow.Row.ItemArray.Select(a => a.ToString()).ToList();
            vals.RemoveAt(vals.Count - 1);
            int i = 0;
            foreach (var cn in sp_inputs.Children)
            {
                if (cn.GetType() == typeof(TextBlock))
                {
                    continue;
                }

                if (cn.GetType() == typeof(TextBox))
                {
                    ((TextBox)cn).Text = vals[i];
                }
                else
                {
                    ((ComboBox)cn).Text = vals[i];
                }
                i++;
            }
        }

        private void Tbx_dataset_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (fd.ShowDialog() == true)
            {
                try
                {
                    var testDt = CSVPreprocessor.CSVtoDataTable(fd.FileName, ref TestSet, random);
                    tbx_numSamples.Text = Math.Round(testDt.Rows.Count * 0.7, 0).ToString();
                    tbx_dataset.Text = Path.GetFileName(fd.FileName);
                    filepath = fd.FileName;

                    btn_rebuild.IsEnabled = true;
                    txt_btm.Text = "File loaded, click 'Build Forest' to create Model";
                }
                catch (Exception er)
                {
                    MessageBox.Show("Failed to load CSV file. Try another file.\n\nError msg: " + er.Message);
                }
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

            TrainingSet = CSVPreprocessor.CSVtoDataTable(filepath, ref TestSet, random);
            var nTree = int.Parse(tbx_numTrees.Text.Replace(" ", "").TrimStart('0'));
            var nSample = int.Parse(tbx_numSamples.Text.Replace(" ", "").TrimStart('0'));
            RandomForest.OnCultivateStart += Forest_OnCultivateStart;
            forest = new RandomForest(TrainingSet, ref TestSet, nTree, nSample, 0);
            forest.OnCultivateFinished += Forest_OnCultivateFinished;
            forest.OnCultivateProgress += Forest_OnCultivateProgress;
            forest.OnVotingProgress += Forest_OnVotingProgress;
        }

        private void Test2_Click(object sender, RoutedEventArgs e)
        {
            var idx = random.Next(TrainingSet.Rows.Count);
            Console.WriteLine("Testing data #" + (idx + 1));
            Console.WriteLine("\nRESULT:" + forest.Predict(TrainingSet.Rows[idx]) + "\n\n");
        }

        private void Tbx_numTrees_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]");
        }

        private void Btn_accuracy_Click(object sender, RoutedEventArgs e)
        {
            if (TestSet == null || TestSet.Rows.Count == 0)
            {
                return;
            }

            IsCheckingAccuracy = true;

            btn_accuracy.IsEnabled = false;
            txt_accuracy.Text = "Checking...";
            txt_accuracy.FontSize = 20;

            var rowCount = TestSet.Rows.Count;
            var correctPredictionIndices = new List<int>(rowCount);

            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < rowCount; i++)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txt_main.Text = "Predicting Test dataset " + Math.Floor(((i + 1) / (double)rowCount) * 100d) + "%";
                        txt_btm.Text = "Predicting data input... " + (i+1) + "/" + rowCount;
                    });

                    var res = forest.Predict(TestSet.Rows[i]);
                    res.Wait();

                    var a = TestSet.Rows[i][TestSet.Columns.Count - 1].ToString();
                    if (a.Equals(res.Result, StringComparison.OrdinalIgnoreCase))
                    {
                        correctPredictionIndices.Add(i);
                    }
                }
            }).ContinueWith((t) =>
            {
                //foreach (var idx in correctPredictionIndices)
                //{
                //    Dispatcher.Invoke(() =>
                //    {
                        
                //    });
                //}
                Dispatcher.Invoke(() =>
                {
                    txt_main.Text = "Forest Ready";
                    txt_btm.Text = "Waiting for test input...";

                    txt_accuracy.Text = Math.Floor((correctPredictionIndices.Count / (double)rowCount) * 100d) + "%";
                    txt_accuracy.FontSize = 40;
                    btn_accuracy.IsEnabled = true;

                    IsCheckingAccuracy = false;
                });
            });
        }

        private void Btn_addTrees_Click(object sender, RoutedEventArgs e)
        {
            tbx_numTrees.Text = (int.Parse(txt_addTrees.Text) + forest.CountTree).ToString();
            forest.AddTrees(int.Parse(txt_addTrees.Text));
        }
    }
}