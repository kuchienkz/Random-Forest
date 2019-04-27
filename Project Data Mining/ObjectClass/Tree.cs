using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Data_Mining.ObjectClass
{
    public class Tree
    {
        public readonly DataTable Dataset;
        public TreeNode Root { get; set; }
        public static List<Feature> AttributeCollection;
        
        public Tree(DataTable dt)
        {
            Dataset = dt;
            Root = Learn(dt, "");
        }

        //Instance call
        public string Predict(DataRow dataInput)
        {
            var valuesForQuery = new Dictionary<string, string>();
            for (int i = 0; i < Dataset.Columns.Count - 1; i++) // exclude resolution column
            {
                var input = dataInput[i].ToString();
                var colName = Dataset.Columns[i].ColumnName;
                if (CategoricalFactory.IsDescriptorExists(colName))
                {
                    input = CategoricalFactory.DescriptNumericalValue(input, colName);
                }
                valuesForQuery.Add(Dataset.Columns[i].ToString(), input);
            }
            return Predict(Root, valuesForQuery, "");
        }

        //Recursive call
        public static string Predict(TreeNode root, IDictionary<string, string> valuesForQuery, string routeStr)
        {
            var valueFound = false;

            routeStr += root.Name.ToUpper() + " -- ";

            if (root.IsLeaf)
            {
                routeStr = root.Edge.ToLower() + " --> " + root.Name.ToUpper();
                valueFound = true;
            }
            else
            {
                foreach (var childNode in root.ChildNodes)
                {
                    foreach (var entry in valuesForQuery)
                    {
                        if (childNode.Edge.ToUpper().Equals(entry.Value.ToUpper()) && root.Name.ToUpper().Equals(entry.Key.ToUpper()))
                        {
                            valuesForQuery.Remove(entry.Key);

                            return routeStr + Predict(childNode, valuesForQuery, $"{childNode.Edge.ToLower()} --> ");
                        }
                    }
                }
            }
            
            if (!valueFound)
            {
                routeStr += "NOT_FOUND";
            }
            return routeStr;
        }

        private TreeNode Learn(DataTable dt, string edgeName)
        {
            var root = GetRootNode(dt, edgeName);

            foreach (var val in root.NodeAttribute.DistinctAttributeValues)
            {
                if (IsLeaf(root, dt, val))
                {
                    continue;
                }

                var reducedTable = CreateSmallerTable(dt, val, root.TableIndex);

                root.ChildNodes.Add(Learn(reducedTable, val));
            }

            return root;
        }

        private static bool IsLeaf(TreeNode root, DataTable dt, string attribute)
        {
            var isLeaf = true;
            var allEndValues = new List<string>();

            // get all leaf values for the attribute in question
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                var s = dt.Rows[i][root.TableIndex].ToString();
                if (s.Equals(attribute))
                {
                    var ss = dt.Rows[i][dt.Columns.Count - 1].ToString();
                    allEndValues.Add(ss);
                }
            }

            // check whether all elements of the list have the same value
            if (allEndValues.Count > 0 && allEndValues.Any(x => x != allEndValues[0]))
            {
                isLeaf = false;
            }

            // create leaf with value to display and edge to the leaf
            if (isLeaf)
            {
                root.ChildNodes.Add(new TreeNode(true, allEndValues[0], attribute));
            }

            return isLeaf;
        }

        private static DataTable CreateSmallerTable(DataTable dt, string edgePointingToNextNode, int rootTableIndex)
        {
            var smallerDt = dt.Clone();

            // add rows which contain edgePointingToNextNode to new datatable
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[i][rootTableIndex].ToString().Equals(edgePointingToNextNode))
                {
                    var row = new string[dt.Columns.Count];

                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        row[j] = dt.Rows[i][j].ToString();
                    }

                    smallerDt.Rows.Add(row);
                }
            }

            // remove column which was already used as node            
            smallerDt.Columns.Remove(smallerDt.Columns[rootTableIndex]);

            return smallerDt;
        }

        private static TreeNode GetRootNode(DataTable dt, string edge)
        {
            var attributes = new List<Feature>();
            var highestInformationGainIndex = -1;
            var highestInformationGain = 0.0;
            
            try
            {
                // Get all names, amount of attributes and attributes for every column             
                for (var i = 0; i < dt.Columns.Count - 1; i++)
                {
                    var distinctvalues = Feature.GetDistinctValuesOfColumn(dt, i);
                    attributes.Add(new Feature(dt.Columns[i].ToString(), distinctvalues));
                }

                // Calculate Entropy (S)
                var tableEntropy = CalculateBaseEntropy(dt);

                for (var i = 0; i < attributes.Count; i++)
                {
                    var gain = CalculateInformationGain(dt, i, tableEntropy);
                    if (gain > highestInformationGain)
                    {
                        attributes[i].InformationGain = gain;
                        highestInformationGain = attributes[i].InformationGain;
                        highestInformationGainIndex = i;
                    }
                }

                if (highestInformationGainIndex == -1)
                {
                    highestInformationGainIndex = 0;
                }
                return new TreeNode(attributes[highestInformationGainIndex].Name, highestInformationGainIndex, attributes[highestInformationGainIndex], edge);
            }
            catch (Exception z)
            {
                Console.WriteLine(z.Message);
                throw;
            }
        }

        private static double CalculateInformationGain(DataTable dt, int colIndex, double baseEntropy)
        {
            var totalRows = dt.Rows.Count;
            var amountOfEachDistinctValue = GetAmountOfEachDistinctValue(dt, colIndex);
            var amountOfEachDistinctValue_resol = GetAmountOfEachDistinctValue(dt, dt.Columns.Count - 1);

            var stepsForCalculation = new List<double>();
            
            foreach (var val in amountOfEachDistinctValue)
            {
                var p_val = val.Occurences / (double)dt.Rows.Count; // peluang val
                var p_val_label = new List<KeyValuePair<string, int>>();
                foreach (var label in amountOfEachDistinctValue_resol)
                {
                    int c = 0;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if ((string)dt.Rows[i][colIndex] == val.Value && (string)dt.Rows[i][dt.Columns.Count - 1] == label.Value)
                        {
                            c++;
                        }
                    }
                    p_val_label.Add(new KeyValuePair<string, int>(label.Value, c));
                }

                // prevent dividedByZeroException
                if (p_val == 0)
                {
                    stepsForCalculation.Add(0.0);
                }
                else
                {
                    //stepsForCalculation.Add(-p * Math.Log(p, 2) - secondDivision * Math.Log(secondDivision, 2));
                    var calc = -p_val * CalculateEntropy(p_val_label.Select(a => a.Value / (double)val.Occurences).ToArray());
                    stepsForCalculation.Add(calc);
                }
            }

            var gain = baseEntropy - stepsForCalculation.Sum();

            return gain;
        }

        private static double CalculateBaseEntropy(DataTable dt)
        {
            var amountOfEachDistinctValue = GetAmountOfEachDistinctValue(dt, dt.Columns.Count - 1);

            var cRows = dt.Rows.Count;
            var baseEntropy = CalculateEntropy(amountOfEachDistinctValue
                .Select(val => val.Occurences / (double)cRows)
                .ToArray());

            return baseEntropy;
        }

        private static List<DistinctValue> GetAmountOfEachDistinctValue(DataTable dt, int indexOfColumnToCheck)
        {
            var foundValues = new List<DistinctValue>();
            var distinctValues = Feature.GetDistinctValuesOfColumn(dt, indexOfColumnToCheck);

            foreach (var val in distinctValues)
            {
                var occurence = 0;
                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i][indexOfColumnToCheck].ToString().Equals(val))
                    {
                        occurence++;
                    }
                }

                var array = new DistinctValue(val, occurence);
                foundValues.Add(array);
            }

            return foundValues;
        }

        public string GenerateGraphVizInput()
        {
            return GenerateGraphVizInput(Root, "", "digraph { \n");
        }

        private string GenerateGraphVizInput(TreeNode root, string edge, string g)
        {
            if (root.ChildNodes != null && root.ChildNodes.Count > 0)
            {
                foreach (var n in root.ChildNodes)
                {
                    GenerateGraphVizInput(n, root.Edge.ToLower(), g);
                    g += "\"" + root.Name.ToUpper() + "\" -> \"" + n.Name.ToUpper() + "\"[label=\"" + n.Edge.ToLower() + "\"];\n";
                }
            }

            return g + "}";
        }

        private static double CalculateEntropy(params double[] p)
        {
            double r = 0.0;
            foreach (var z in p)
            {
                r += -(z * Math.Log(z, p.Length));
            }
            return double.IsNaN(r) ? 0 : r;
                   
        }

        private class DistinctValue
        {
            public string Value;
            public int Occurences;

            public DistinctValue(string value, int occurences)
            {
                Value = value;
                Occurences = occurences;
            }
        }
    }
}
