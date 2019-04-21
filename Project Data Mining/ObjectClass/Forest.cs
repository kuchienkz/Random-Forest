using Project_Data_Mining.ObjectClass;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Project_Data_Mining
{
    public class RandomForest
    {
        public List<Tree> Trees;
        public bool IsReady { get; set; }
        public string LastVoteResult { get; private set;}

        public int CountTree;
        public int SubsampleSize;
        public RandomForest(DataTable dataset, int jumlahTree, int ukuranSubsample)
        {
            Console.WriteLine("GENERATING FOREST...");
            IsReady = false;
            Trees = new List<Tree>();

            CountTree = jumlahTree;
            SubsampleSize = ukuranSubsample;

            CultivateTrees(dataset).ContinueWith(delegate 
            {
                IsReady = true;
                Console.WriteLine("FOREST READY!");
                //Console.WriteLine("GraphViz: " + Trees[0].GraphVizInput);
            });
        }
        
        public string Predict(DataRow dataInput)
        {
            if (Trees.Count < CountTree)
            {
                MessageBox.Show("Forest is not ready! Trees: " + Trees.Count + "/" + CountTree, "Please wait...", MessageBoxButton.OK, MessageBoxImage.Information);
                return "";
            }
            List<string> votes = new List<string>();
            foreach (var t in Trees)
            {
                votes.Add(t.Predict(dataInput));
            }

            for (int i = 0; i < votes.Count; i++)
            {
                Console.WriteLine("Path of Tree #" + (i + 1) + ": " + votes[i]);
                votes[i] = votes[i].Split(new string[]{ "-->", "--" }, StringSplitOptions.None).Last().Trim(' ');
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
        }
        
        private Task CultivateTrees(DataTable dt)
        {
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
