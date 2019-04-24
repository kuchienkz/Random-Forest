using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Data_Mining.ObjectClass
{
    public enum MyEnum
    {

    }
    public class Attribute
    {
        public Attribute(string name, List<DistinctValue> distincttAttributevalues)
        {
            Name = name;
            DistinctAttributeValues = distincttAttributevalues;
        }

        public string Name { get; }

        public List<DistinctValue> DistinctAttributeValues { get; }

        public double InformationGain { get; set; }

        public static List<DistinctValue> GetDistinctAttributeValuesOfColumn(DataTable dt, int columnIndex)
        {
            var distinctValues = new SortedSet<DistinctValue>();
            var descriptor = Tree.AttributeDescriptors.Count > columnIndex ? Tree.AttributeDescriptors[columnIndex] : null;

            if (descriptor == null)
            {

                for (var i = 0; i < dt.Rows.Count; i++)
                {
                    var s = new DistinctValue(dt.Rows[i][columnIndex].ToString(), 0);
                    distinctValues.Add(s);
                }
            }
            else
            {
                
                for (int i = 0; i < descriptor.Length; i++)
                {
                    var cc = 0;
                    for (int q = 0; q < dt.Rows.Count; q++)
                    {
                        if (descriptor[i].Check(dt.Rows[q][columnIndex].ToString()))
                        {
                            cc++;
                        }
                    }
                    var s = new DistinctValue(descriptor[i], cc);
                    distinctValues.Add(s);
                }
                
            }

            return distinctValues.ToList();
        }
    }
}
