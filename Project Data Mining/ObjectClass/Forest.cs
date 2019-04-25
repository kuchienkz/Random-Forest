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
        public List<Feature> Attributes;
        public bool IsReady { get; set; }
        public string LastVoteResult { get; private set;}

        public int CountTree;
        public int BootstrapSampleSize;
        public RandomForest(DataTable dataset, int jumlahTree, int ukuranSubsample)
        {
            Console.WriteLine("GENERATING FOREST...");
            IsReady = false;
            Trees = new List<Tree>();

            CountTree = jumlahTree;
            BootstrapSampleSize = ukuranSubsample;
            
            CultivateTrees(dataset).ContinueWith((t) => 
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
        
        private Task CultivateTrees(DataTable dt)
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
                        Trees.Add(new Tree(sample));
                        OnCultivateProgress?.Invoke(Trees.Count, Trees.Count / (double)CountTree * 100d);
                        Console.WriteLine("Cultivating trees........" + (Trees.Count) + " / " + CountTree);
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
                Random r = new Random();
                for (int i = 0; i < BootstrapSampleSize; i++)
                {
                    var idx = r.Next(count);
                    DataRow ro = originalDataset.Rows[idx];
                    newDt.Rows.Add(ro.ItemArray);
                }

                return newDt;
            });
        }

        //public string GetLargestDOT_Tree()
        //{
        //    var longest = "";
        //    foreach (var t in Trees)
        //    {
        //        var a = t.GenerateGraphVizInput();
        //        if (a.Length > longest.Length)
        //        {
        //            longest = a;
        //        }
        //    }

        //    return longest;
        //}
    }
}
