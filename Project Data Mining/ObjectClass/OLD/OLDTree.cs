//using System.Collections.Generic;
//using System.Data;
//using System.IO;
//using System.Linq;

//namespace Project_Data_Mining.ObjectClass
//{
//    public class DecisionTree
//    {
//        List<Attribute> Attributes;
//        List<string> Labels = new List<string>();
        
//        public string LabelColumn { get; set; }
//        public TreeNode Root { get; set; }

//        public DecisionTree(string csvFile)
//        {
//            Attributes = new List<Attribute>();
            
//            var lines = File.ReadAllLines(csvFile).ToList();
//            var headers = lines[0].Split(',');
//            LabelColumn = headers.Last();
//            lines.RemoveAt(0);

//            for (int i = 0; i < headers.Length - 1; i++)
//            {
//                var vals = new List<string>();
//                for (int c = 0; c < lines.Count; c++)
//                {
//                    vals.Add(lines[c].Split(',')[i]);
//                }

//                Attributes.Add(new Attribute(headers[i].Replace(" ", ""), vals));
//            }
//            Labels.AddRange(new SortedSet<string>(lines.Select(a => a.Split(',')[headers.Length - 1])));


//            DataTable dt = CSVtoDataTable(csvFile);
//            Root = GenerateID3Tree(dt, Attributes);
//        }


        
//        private TreeNode GenerateID3Tree(DataTable dt, List<Attribute> attributes)
//        {
//            TreeNode node = new TreeNode();

//            var uniclass = InstancesBelongToTheSameClass(dt);
//            if (!uniclass.Equals(""))
//            {
//                node.Value = uniclass;
//                return node;
//            }
//            if (attributes.Count == 0)
//            {
//                node.Value = MostCommonLabel(dt);
//                return node;
//            }
//            Attribute selectedAttribute = BestAttribute(attributes, dt);
//            node.Value = selectedAttribute;
//            node.Children = selectedAttribute.Values.Count;

//            var count = selectedAttribute.Values.Count;
//            for (int i = 0; i < count; i++)
//            {
//                DataTable dtSubset = InstancesWithSpecificValue(selectedAttribute, dt, selectedAttribute.Values[i]);
//                if (dtSubset.Rows.Count != 0)
//                {
//                    attributes.Remove(selectedAttribute);
//                    node[i] = GenerateID3Tree(dtSubset, attributes);
//                }
//                else
//                {
//                    node.RemoveSpecifiedChild(node[i]);
//                    count--;
//                }
//            }

//            return node;
//        }
        
//        private string MostCommonLabel(DataTable dt_)
//        {
//            int[] cc = new int[Labels.Count];

//            foreach (DataRow row in dt_.Rows)
//            {
//                var idx = Labels.IndexOf(Labels.First(a => row[LabelColumn].ToString().Equals(a)));
//                if (idx >= 0)
//                {
//                    cc[idx]++;
//                }
//            }

//            var labIdx = 0;
//            var highest = cc[0];
//            for (int i = 0; i < cc.Length; i++)
//            {
//                if (cc[i] > highest)
//                {
//                    highest = cc[i];
//                    labIdx = i;
//                }
//            }

//            return Labels[labIdx];
//        }

//        /// <summary>
//        /// Method returns all instances that have attribute value specified by input argument
//        /// </summary>
//        /// <param name="attValue">string</param>
//        /// <returns>DataTable</returns>
//        private DataTable InstancesWithSpecificValue(Attribute at, DataTable dt_, string attValue)
//        {
//            DataTable dtSubset = new DataTable();
//            dtSubset = dt_.Clone();

//            foreach (DataRow row in dt_.Rows)
//            {
//                if (row[at.Name].ToString() == attValue)
//                {
//                    dtSubset.ImportRow(row);
//                }
//            }

//            return dtSubset;
//        }

//        public string InstancesBelongToTheSameClass(DataTable dt_)
//        {
//            foreach (var lab in Labels)
//            {
//                string conceptColumnTrue = string.Format("{0} = '{1}'", LabelColumn, lab);
//                if (dt_.Select(conceptColumnTrue).Length == dt_.Rows.Count)
//                {
//                    return lab;
//                }
//            }

//            return "";
//        }
        
//        public Attribute BestAttribute(List<Attribute> attributes, DataTable dt_)
//        {
//            Dictionary<string, double> attGainDictionary = new Dictionary<string, double>();
//            double gainPerAtt = 0.0;
//            foreach (Attribute att in attributes)
//            {
//                gainPerAtt = CalculateAttributeGain(att, dt_);
//                attGainDictionary.Add(att.Name, gainPerAtt);
//            }
            
//            double mostGain = 0.0;
//            foreach (var item in attGainDictionary)
//            {
//                if (item.Value > mostGain)
//                {
//                    mostGain = item.Value;
//                }
//            }

//            return attributes.FirstOrDefault(g => g.Name.Equals(
//                attGainDictionary.FirstOrDefault(a => a.Value == mostGain).Key));
//        }
        
//        public double CalculateAttributeGain(Attribute att, DataTable dt_)
//        {
//            double informationGainPerAttribute = CalculateEntropy(dt_);
//            string[] columnsToQuery = new string[] { att.Name, LabelColumn };
//            DataView dv = new DataView(dt_);
//            DataTable dtAtt = dv.ToTable(false, columnsToQuery);
//            List<int> flags = new List<int>();

//            for (int i = 0; i < att.Values.Count * 2; i++)
//            {
//                flags.Add(0);
//            }

//            foreach (DataRow row in dtAtt.Rows)
//            {
//                for (int i = 0; i < att.Values.Count; i++)
//                {
//                    foreach (var lab in Labels)
//                    {
//                        if (row[att.Name].ToString() == att.Values[i] && row[LabelColumn].ToString() == lab)
//                        {
//                            flags[2 * i + 1]++;
//                            break;
//                        }
//                    }
//                }
//            }

//            double tempSumOfGain = 0.0;
//            double allFlagsSummed = 0.0;
//            foreach (int item in flags)
//            {
//                allFlagsSummed += item;
//            }

//            for (int i = 0; i < flags.Count; i = i + 2)
//            {
//                double attTotalPositiveNegativePerValue = flags[i] + flags[i + 1];
//                double firstAddend = xlog2x(flags[i], attTotalPositiveNegativePerValue);
//                double secondAddend = xlog2x(flags[i + 1], attTotalPositiveNegativePerValue);
//                tempSumOfGain += (attTotalPositiveNegativePerValue / allFlagsSummed) * (firstAddend + secondAddend);
//            }

//            return informationGainPerAttribute - tempSumOfGain;
//        }

//        /// <summary>
//        /// Method calculates entropy as per formula specified in ID3 algorithm.
//        /// </summary>
//        /// <returns></returns>
//        public double CalculateEntropy(DataTable dt_)
//        {
//            int[] cc = new int[Labels.Count];

//            DataView dv = new DataView(dt_);
//            DataTable onlyConceptColumn = dv.ToTable(LabelColumn);

//            foreach (DataRow row in onlyConceptColumn.Rows)
//            {
//                var idx = Labels.IndexOf(Labels.First(a => row[LabelColumn].ToString().Equals(a)));
//                if (idx >= 0)
//                {
//                    cc[idx]++;
//                }
//            }

//            int nrOfTotalExamples = cc.Sum();
//            double tempReturn = 0;
//            for (int i = 0; i < cc.Length; i++)
//            {
//                tempReturn += xlog2x(cc[i], nrOfTotalExamples);
//            }
            
//            return tempReturn;
//        }

//        /// <summary>
//        /// Method calculates xlog2x where x is ratio of x/y form and x is first input parameter while y is second.
//        /// </summary>
//        /// <param name="numerator"></param>
//        /// <param name="denumerator"></param>
//        /// <returns></returns>
//        private double xlog2x(double numerator, double denumerator)
//        {
//            double tempSum;
//            if (numerator == 0 || denumerator == 0)
//            {
//                return 0.0;
//            }
//            else
//                tempSum = -numerator / denumerator;
//            tempSum *= System.Math.Log(numerator / denumerator, 2);

//            return tempSum;
//        }

//        public static DataTable CSVtoDataTable(string strFilePath)
//        {
//            DataTable dt = new DataTable();
//            using (StreamReader sr = new StreamReader(strFilePath))
//            {
//                string[] headers = sr.ReadLine().Split(',');
//                foreach (string header in headers)
//                {
//                    dt.Columns.Add(header);
//                }
//                while (!sr.EndOfStream)
//                {
//                    string[] rows = sr.ReadLine().Split(',');
//                    DataRow dr = dt.NewRow();
//                    for (int i = 0; i < headers.Length; i++)
//                    {
//                        dr[i] = rows[i];
//                    }
//                    dt.Rows.Add(dr);
//                }

//            }


//            return dt;
//        }
//    }
//}
