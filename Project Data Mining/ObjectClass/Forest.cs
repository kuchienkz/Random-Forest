using Project_Data_Mining.ObjectClass;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Project_Data_Mining
{
    public class RandomForest
    {
        public delegate void CultivateProgress(int treeDone, double percentage);
        public delegate void VotingProgress(int voteDone, double percentage);
        public event CultivateProgress OnCultivateProgress;
        public event VotingProgress OnVotingProgress;
        public static event EventHandler OnCultivateStart;
        public event EventHandler OnCultivateFinished;

        public List<Tree> Trees;
        public List<ObjectClass.Attribute> Attributes;
        public List<CategoricalFactory.EqualWidthBin[]> AttributeDescriptors;
        public bool IsReady { get; set; }
        public string LastVoteResult { get; private set;}

        public int CountTree;
        public int SubsampleSize;
        public RandomForest(DataTable dataset, int jumlahTree, int ukuranSubsample)
        {
            Console.WriteLine("GENERATING FOREST...");
            IsReady = false;
            Trees = new List<Tree>();
            AttributeDescriptors = new List<CategoricalFactory.EqualWidthBin[]>(dataset.Columns.Count);

            // [EXPERIMENTAL] Convert numericals to categorical, for training set only
            for (int i = 0; i < dataset.Columns.Count; i++)
            {
                var vals = ObjectClass.Attribute.GetDistinctAttributeValuesOfColumn(dataset, i);

                if (vals.Count > 10
                    && vals.All(a => double.TryParse(a.Value, out double ou))) // probably numerical
                {
                    var bins = CategoricalFactory.GenerateEqualWidthBins(dataset, i, 4);
                    AttributeDescriptors.Add(bins);
                }
            }
            Tree.AttributeDescriptors = AttributeDescriptors;

            CountTree = jumlahTree;
            SubsampleSize = ukuranSubsample;
            
            CultivateTrees(dataset).ContinueWith(delegate 
            {
                IsReady = true;
                OnCultivateFinished?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("FOREST READY!");

                //Console.WriteLine("GraphViz: " + Trees[0].GraphVizInput);
            });
        }
        
        public Task<string> Predict(DataRow dataInput)
        {
            return Task<string>.Factory.StartNew(() =>
            {
                if (Trees.Count < CountTree)
                {
                    MessageBox.Show("Forest is not ready! Trees: " + Trees.Count + "/" + CountTree, "Please wait...", MessageBoxButton.OK, MessageBoxImage.Information);
                    return "";
                }
                List<string> votes = new List<string>();
                for (int i = 0; i < Trees.Count; i++)
                {
                    votes.Add(Trees[i].Predict(dataInput));
                    OnVotingProgress?.Invoke(i + 1, (i + 1) / (double)Trees.Count * 100.0);
                }

                for (int i = 0; i < votes.Count; i++)
                {
                    Console.WriteLine("Path of Tree #" + (i + 1) + ": " + votes[i]);
                    votes[i] = votes[i].Split(new string[] { "-->", "--" }, StringSplitOptions.None).Last().Trim(' ');
                }

                votes.RemoveAll(a => a.Contains("NOT_FOUND"));
                if (votes.Count > 0)
                {
                    LastVoteResult = votes.GroupBy(s => s).OrderByDescending(s => s.Count()).First().Key;
                }
                else
                {
                    LastVoteResult = "NOT_FOUND";
                }

                Console.WriteLine("MOST FREQUENT PREDICTION: " + LastVoteResult);

                return LastVoteResult;
            });
        }
        
        private Task CultivateTrees(DataTable dt)
        {
            IsReady = false;
            OnCultivateStart?.Invoke(this, EventArgs.Empty);

            return Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < CountTree; i++)
                {
                    Task.Factory.StartNew(() =>
                    {
                        var sample = BootstrapResample(dt).Result;
                        try
                        {
                            Trees.Add(new Tree(sample));
                            OnCultivateProgress?.Invoke(Trees.Count, (double)Trees.Count / (double)CountTree * 100);
                            Console.WriteLine("Cultivating trees........" + (Trees.Count) + " / " + CountTree);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            throw;
                        }
                    });
                }

                while (Trees.Count < CountTree)
                {
                    System.Threading.Thread.Sleep(100);
                }
            });
        }

        private Task<DataTable> BootstrapResample(DataTable originalDataset)
        {
            return Task<DataTable>.Factory.StartNew(() =>
            {
                var count = originalDataset.Rows.Count;
                DataTable newDt = originalDataset.Clone();
                Random r = new Random();
                for (int i = 0; i < SubsampleSize; i++)
                {
                    var idx = r.Next(count);
                    DataRow ro = originalDataset.Rows[idx];
                    newDt.Rows.Add(ro.ItemArray);
                }

                return newDt;
            });
        }
    }
}
