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

        public string GraphVizInput;
        public readonly DataTable Dataset;
        public TreeNode Root { get; set; }
        public static List<Attribute> AttributeCollection;
        
        public Tree(DataTable dt)
        {
            Dataset = dt;
            Root = Learn(dt, "");
            GraphVizInput = GenerateGraphVizInput(Root, "");
        }

        //Instance call
        public string Predict(DataRow dataInput)
        {
            var valuesForQuery = new Dictionary<string, string>();
            for (int i = 0; i < Dataset.Columns.Count; i++)
            {
                valuesForQuery.Add(Dataset.Columns[i].ToString(), dataInput[i].ToString());
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

            foreach (var item in root.NodeAttribute.DistinctAttributeValues)
            {
                if (IsLeaf(root, dt, item))
                {
                    continue;
                }

                var reducedTable = CreateSmallerTable(dt, item, root.TableIndex);
                root.ChildNodes.Add(Learn(reducedTable, item));
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
            var smallerDt = new DataTable();

            // add column titles
            for (var i = 0; i < dt.Columns.Count; i++)
            {
                smallerDt.Columns.Add(dt.Columns[i].ToString());
            }

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
            var attributes = new List<Attribute>();
            var highestInformationGainIndex = -1;
            var highestInformationGain = double.MinValue;

            // Get all names, amount of attributes and attributes for every column             
            for (var i = 0; i < dt.Columns.Count - 1; i++)
            {
                var distinctvalues = Attribute.GetDistinctAttributeValuesOfColumn(dt, i);
                
                attributes.Add(new Attribute(dt.Columns[i].ToString(), distinctvalues));
            }

            // Calculate Entropy (S)
            var tableEntropy = CalculateTableEntropy(dt);

            for (var i = 0; i < attributes.Count; i++)
            {
                attributes[i].InformationGain = GetGainForAllAttributes(dt, i, tableEntropy);
                if (attributes[i].InformationGain > highestInformationGain)
                {
                    highestInformationGain = attributes[i].InformationGain;
                    highestInformationGainIndex = i;
                }
            }

            AttributeCollection = attributes;
            return new TreeNode(attributes[highestInformationGainIndex].Name, highestInformationGainIndex, attributes[highestInformationGainIndex], edge);
        }

        private static double GetGainForAllAttributes(DataTable dt, int colIndex, double entropyOfDataset)
        {
            var totalRows = dt.Rows.Count;
            var amountForDifferentValue = GetAmountOfEdgesAndTotalPositivResults(dt, colIndex);
            var stepsForCalculation = new List<double>();

            foreach (var item in amountForDifferentValue)
            {
                // helper for calculation
                var firstDivision = item[0, 1] / (double)item[0, 0];
                var secondDivision = (item[0, 0] - item[0, 1]) / (double)item[0, 0];

                // prevent dividedByZeroException
                if (firstDivision == 0 || secondDivision == 0)
                {
                    stepsForCalculation.Add(0.0);
                }
                else
                {
                    stepsForCalculation.Add(-firstDivision * Math.Log(firstDivision, 2) - secondDivision * Math.Log(secondDivision, 2));
                }
            }

            var gain = stepsForCalculation.Select((t, i) => amountForDifferentValue[i][0, 0] / (double)totalRows * t).Sum();

            gain = entropyOfDataset - gain;

            return gain;
        }

        private static double CalculateTableEntropy(DataTable dt)
        {
            var totalRows = dt.Rows.Count;
            var amountForDifferentValue = GetAmountOfEdgesAndTotalPositivResults(dt, dt.Columns.Count - 1);

            var stepsForCalculation = amountForDifferentValue
                .Select(item => item[0, 0] / (double)totalRows)
                .Select(division => -division * Math.Log(division, 2))
                .ToList();

            return stepsForCalculation.Sum();
        }

        private static List<int[,]> GetAmountOfEdgesAndTotalPositivResults(DataTable dt, int indexOfColumnToCheck)
        {
            var foundValues = new List<int[,]>();
            var knownValues = CountKnownValues(dt, indexOfColumnToCheck);

            foreach (var item in knownValues)
            {
                var amount = 0;
                var positiveAmount = 0;

                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i][indexOfColumnToCheck].ToString().Equals(item))
                    {
                        amount++;

                        // Counts the positive cases and adds the sum later to the array for the calculation
                        if (dt.Rows[i][dt.Columns.Count - 1].ToString().Equals(dt.Rows[0][dt.Columns.Count - 1]))
                        {
                            positiveAmount++;
                        }
                    }
                }

                int[,] array = { { amount, positiveAmount } };
                foundValues.Add(array);
            }

            return foundValues;
        }

        private static IEnumerable<string> CountKnownValues(DataTable dt, int indexOfColumnToCheck)
        {
            var knownValues = new List<string>();

            // add the value of the first row to the list
            if (dt.Rows.Count > 0)
            {
                knownValues.Add(dt.Rows[0][indexOfColumnToCheck].ToString());
            }

            for (var j = 1; j < dt.Rows.Count; j++)
            {
                var newValue = knownValues.All(item => !dt.Rows[j][indexOfColumnToCheck].ToString().Equals(item));

                if (newValue)
                {
                    knownValues.Add(dt.Rows[j][indexOfColumnToCheck].ToString());
                }
            }

            return knownValues;
        }

        private string GenerateGraphVizInput(TreeNode root, string edge)
        {
            var s = "digraph { \n";
            if (root.ChildNodes != null && root.ChildNodes.Count > 0)
            {
                foreach (var n in root.ChildNodes)
                {
                    GenerateGraphVizInput(n, root.Edge.ToLower());
                    s += "\"" + root.Name.ToUpper() + "\" -> \"" + n.Name.ToUpper() + "\"[label=\"" + n.Edge.ToLower() + "\"];\n";
                }
            }
            else
            {

            }

            return s + "}";
        }
    }
}
