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
        private static Random r = new Random();

        public delegate void CultivateProgress(int treeDone, double percentage);
        public delegate void VotingProgress(int voteDone, double percentage);
        public event CultivateProgress OnCultivateProgress;
        public event VotingProgress OnVotingProgress;
        public static event EventHandler OnCultivateStart;
        public event EventHandler OnCultivateFinished;

        public DataTable dt;
        public List<Tree> Trees;
        public List<Feature> Attributes;
        public bool IsReady { get; set; }
        public string LastVoteResult { get; private set;}
        private DataTable TestSet;

        public double MinimumAccuracy;
        public int CountTree;
        public int BootstrapSampleSize;

        public RandomForest(DataTable trainingSet, ref DataTable testSet, int jumlahTree, int ukuranSubsample, double minAccuracy)
        {
            Console.WriteLine("GENERATING FOREST...");
            IsReady = false;
            Trees = new List<Tree>();
            dt = trainingSet;
            TestSet = testSet;
            MinimumAccuracy = minAccuracy > 1 ? minAccuracy / 100d : minAccuracy;

            CountTree = jumlahTree;
            BootstrapSampleSize = ukuranSubsample;
            
            CultivateTrees().ContinueWith((t) => 
            {
                IsReady = true;
                OnCultivateFinished?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("FOREST READY!");
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
        
        public void AddTrees(int addition)
        {
            CountTree += addition;
            CultivateTrees().ContinueWith((t) =>
            {
                IsReady = true;
                OnCultivateFinished?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("FOREST READY!");
            }); ;
        }

        private Task CultivateTrees()
        {
            IsReady = false;
            OnCultivateStart?.Invoke(this, EventArgs.Empty);

            return Task.Factory.StartNew(() =>
            {
                while (Trees.Count < CountTree)
                {
                    var sample = BootstrapResample(dt).Result;
                    try
                    {
                        var nt = new Tree(sample);
                        var acc = GetTreeAccuracy(nt);
                        if (acc >= MinimumAccuracy)
                        {
                            Trees.Add(nt);
                            OnCultivateProgress?.Invoke(Trees.Count, Trees.Count / (double)CountTree * 100d);
                            Console.WriteLine("Cultivating trees........" + (Trees.Count) + " / " + CountTree);
                        }
                        else
                        {
                            Console.WriteLine("Accuracy is " + acc + ", excluded.");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            });
        }

        private Task<DataTable> BootstrapResample(DataTable originalDataset)
        {
            return Task<DataTable>.Factory.StartNew(() =>
            {
                var count = originalDataset.Rows.Count;
                DataTable newDt = originalDataset.Clone();
                for (int i = 0; i < BootstrapSampleSize; i++)
                {
                    var idx = r.Next(count);
                    DataRow ro = originalDataset.Rows[idx];
                    newDt.Rows.Add(ro.ItemArray);
                }

                return newDt;
            });
        }

        private double GetTreeAccuracy(Tree tree)
        {
            var correctPrediction = 0d;
            foreach (DataRow t in TestSet.Rows)
            {
                var r = tree.Predict(t).Split(new string[] { "-->", "--" }, StringSplitOptions.None).Last().Trim(' '); ;
                var r2 = t[TestSet.Columns.Count - 1].ToString();
                if (r == r2)
                {
                    correctPrediction += 1;
                }
            }

            return correctPrediction / TestSet.Rows.Count;
        }

       
    }
}
