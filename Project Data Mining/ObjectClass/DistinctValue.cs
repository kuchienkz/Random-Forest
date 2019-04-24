using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Data_Mining.ObjectClass
{
    public class DistinctValue : IComparable
    {
        private string value;
        public int Occurences;
        public CategoricalFactory.EqualWidthBin Descriptor;

        public bool HasDescriptor = false;

        public DistinctValue(string value, int occurences)
        {
            this.value = value;
            Occurences = occurences;
        }

        public string Value
        {
            get
            {
                return HasDescriptor ? Descriptor.GetLabel() : value;
            }
        }

        public DistinctValue(CategoricalFactory.EqualWidthBin descriptor, int occurences)
        {
            HasDescriptor = true;
            Descriptor = descriptor;
            Occurences = occurences;
        }

        public int CompareTo(object obj)
        {
            return Value.CompareTo(((DistinctValue)obj).Value);
        }
    }
}
