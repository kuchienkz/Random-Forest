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
        public Attribute(string name, List<string> distincttAttributevalues)
        {
            Name = name;
            DistinctAttributeValues = distincttAttributevalues;
        }

        public CategoricalComparer CategoricalComparer;

        public string Name { get; }

        public List<string> DistinctAttributeValues { get; }

        public double InformationGain { get; set; }

        public static List<string> GetDistinctAttributeValuesOfColumn(DataTable dt, int columnIndex)
        {
            var distinctValues = new SortedSet<string>();

            for (var i = 0; i < dt.Rows.Count; i++)
            {
                var s = dt.Rows[i][columnIndex].ToString();
                distinctValues.Add(s);
            }

            return distinctValues.ToList();
        }

        public bool Compare()
        {

        }
    }
}
