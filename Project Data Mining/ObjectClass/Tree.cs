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
        public static List<CategoricalFactory.EqualWidthBin[]> AttributeDescriptors;


        public Tree(DataTable dt)
        {
            Dataset = dt;

            Root = Learn(dt, new DistinctValue("", 0));
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
                routeStr = root.Edge.Value.ToLower() + " --> " + root.Name.ToUpper();
                valueFound = true;
            }
            else
            {
                foreach (var childNode in root.ChildNodes)
                {
                    foreach (var entry in valuesForQuery)
                    {
                        if (childNode.Edge.Value.ToUpper().Equals(entry.Value.ToUpper()) && root.Name.ToUpper().Equals(entry.Key.ToUpper()))
                        {
                            valuesForQuery.Remove(entry.Key);

                            return routeStr + Predict(childNode, valuesForQuery, $"{childNode.Edge.Value.ToLower()} --> ");
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

        private TreeNode Learn(DataTable dt, DistinctValue edgeName)
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

        private static bool IsLeaf(TreeNode root, DataTable dt, DistinctValue value)
        {
            var isLeaf = true;
            var allEndValues = new List<string>();

            // get all leaf values for the attribute in question
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                var s = dt.Rows[i][root.TableIndex].ToString();
                if (value.HasDescriptor)
                {
                    if (value.Descriptor.Check(s))
                    {
                        var ss = dt.Rows[i][dt.Columns.Count - 1].ToString();
                        allEndValues.Add(ss);
                    }
                }
                else
                {
                    if (s.Equals(value.Value))
                    {
                        var ss = dt.Rows[i][dt.Columns.Count - 1].ToString();
                        allEndValues.Add(ss);
                    }
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
                root.ChildNodes.Add(new TreeNode(true, allEndValues[0], value));
            }

            return isLeaf;
        }

        private static DataTable CreateSmallerTable(DataTable dt, DistinctValue edgePointingToNextNode, int rootTableIndex)
        {
            var smallerDt = dt.Clone();

            // add rows which contain edgePointingToNextNode to new datatable
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                var row = new string[dt.Columns.Count];

                if (edgePointingToNextNode.HasDescriptor)
                {
                    if (edgePointingToNextNode.Descriptor.Check(dt.Rows[i][rootTableIndex].ToString()))
                    {
                        for (var j = 0; j < dt.Columns.Count; j++)
                        {
                            row[j] = dt.Rows[i][j].ToString();
                        }

                        smallerDt.Rows.Add(row);
                    }
                }
                else
                {
                    if (dt.Rows[i][rootTableIndex].ToString().Equals(edgePointingToNextNode.Value))
                    {
                        for (var j = 0; j < dt.Columns.Count; j++)
                        {
                            row[j] = dt.Rows[i][j].ToString();
                        }

                        smallerDt.Rows.Add(row);
                    }
                }
               
            }

            // remove column which was already used as node            
            smallerDt.Columns.Remove(smallerDt.Columns[rootTableIndex]);

            return smallerDt;
        }

        private static TreeNode GetRootNode(DataTable dt, DistinctValue edge)
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
            var baseEntropy = CalculateBaseEntropy(dt);

            for (var i = 0; i < attributes.Count; i++)
            {
                attributes[i].InformationGain = CalculateInformationGain(dt, i, baseEntropy);
                if (attributes[i].InformationGain > highestInformationGain)
                {
                    highestInformationGain = attributes[i].InformationGain;
                    highestInformationGainIndex = i;
                }
            }

            AttributeCollection = attributes;
            return new TreeNode(attributes[highestInformationGainIndex].Name, highestInformationGainIndex, attributes[highestInformationGainIndex], edge);
        }

        private static double CalculateInformationGain(DataTable dt, int colIndex, double baseEntropy)
        {
            var totalRows = dt.Rows.Count;
            var amountOfEachDistinctValue = GetAmountOfEachDistinctValue(dt, colIndex);
            var amountOfEachDistinctValue_resol = GetAmountOfEachDistinctValue(dt, dt.Columns.Count - 1);

            var stepsForCalculation = new List<double>();
            
            foreach (var val in amountOfEachDistinctValue)
            {
                var p_val_label = new List<KeyValuePair<string, int>>();
                foreach (var label in amountOfEachDistinctValue_resol)
                {
                    int c = 0;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (val.HasDescriptor)
                        {
                            if (val.Descriptor.Check((string)dt.Rows[i][colIndex]) && (string)dt.Rows[i][dt.Columns.Count - 1] == label.Value)
                            {
                                c++;
                            }
                        }
                        else
                        {
                            if ((string)dt.Rows[i][colIndex] == val.Value && (string)dt.Rows[i][dt.Columns.Count - 1] == label.Value)
                            {
                                c++;
                            }
                        }
                    }
                    p_val_label.Add(new KeyValuePair<string, int>(label.Value, c));
                }

                var p_val = val.Occurences / (double)dt.Rows.Count; // peluang val

                // prevent dividedByZeroException
                if (p_val == 0)
                {
                    stepsForCalculation.Add(0.0);
                }
                else
                {
                    //stepsForCalculation.Add(-p * Math.Log(p, 2) - secondDivision * Math.Log(secondDivision, 2));
                    var calc = p_val * CalculateEntropy(p_val_label.Select(a => a.Value / (double)val.Occurences).ToArray());
                    stepsForCalculation.Add(calc);
                }
            }

            var gain = baseEntropy - stepsForCalculation.Sum();

            return gain;
        }
        
        private static List<DistinctValue> GetAmountOfEachDistinctValue(DataTable dt, int indexOfColumnToCheck)
        {
            var foundValues = new List<DistinctValue>();
            var descriptor = Tree.AttributeDescriptors.Count > indexOfColumnToCheck ? AttributeDescriptors[indexOfColumnToCheck] : null;

            if (descriptor == null) // already categorical
            {
                var distinctValues = GetCategoricalDisticntValues(dt, indexOfColumnToCheck);
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
            }
            else // numerical, use descriptor
            {
                foreach (var dc in descriptor)
                {
                    var occurence = 0;

                    for (var i = 0; i < dt.Rows.Count; i++)
                    {

                        if (dc.Check(dt.Rows[i][indexOfColumnToCheck].ToString()))
                        {
                            occurence++;
                        }
                    }

                    var array = new DistinctValue(dc, occurence);
                    foundValues.Add(array);
                }
            }
            

            return foundValues;
        }

        private static IEnumerable<string> GetCategoricalDisticntValues(DataTable dt, int indexOfColumnToCheck)
        {
            var dVals = new SortedSet<string>();
            
            for (var j = 1; j < dt.Rows.Count; j++)
            {
                dVals.Add(dt.Rows[j][indexOfColumnToCheck].ToString());
            }

            return dVals;
        }

        private string GenerateGraphVizInput(TreeNode root, string edge)
        {
            var s = "digraph { \n";
            if (root.ChildNodes != null && root.ChildNodes.Count > 0)
            {
                foreach (var n in root.ChildNodes)
                {
                    GenerateGraphVizInput(n, root.Edge.Value.ToLower());
                    s += "\"" + root.Name.ToUpper() + "\" -> \"" + n.Name.ToUpper() + "\"[label=\"" + n.Edge.Value.ToLower() + "\"];\n";
                }
            }
            else
            {

            }

            return s + "}";
        }

        public static double CalculateBaseEntropy(DataTable dt)
        {
            var amountOfEachDistinctValue = GetAmountOfEachDistinctValue(dt, dt.Columns.Count - 1);

            var cRows = dt.Rows.Count;
            var baseEntropy = CalculateEntropy(amountOfEachDistinctValue
                .Select(val => val.Occurences / (double)cRows) // hitung peluang setiap fitur
                .ToArray());

            return baseEntropy;
        }

        public static double CalculateEntropy(params double[] peluang)
        {
            double r = 0.0;
            foreach (var z in peluang)
            {
                r += -(z * Math.Log(z, peluang.Length));
            }
            return double.IsNaN(r) ? 0 : r;
                   
        }
    }
}
