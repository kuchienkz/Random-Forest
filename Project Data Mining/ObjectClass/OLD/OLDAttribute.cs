//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Project_Data_Mining.ObjectClass
//{
//    public class Attribute
//    {
//        public string Name
//        {
//            get; set;
//        }

//        List<string> values;
//        public List<string> Values
//        {
//            get
//            {
//                return this.values;
//            }
//            set
//            {
//                for (int i = 0; i < value.Count; i++)
//                {
//                    value[i] = value[i].ToLower();
//                }
//                this.values = new List<string>(value);
//            }
//        }

//        public Attribute()
//        {

//        }

//        public Attribute(string name, List<string> values)
//        {
//            this.Name = name;
//            this.Values = values;
//        }

//        public bool IsThereSuchAValue(string value)
//        {
//            if (Values.FindIndex(0, Values.Count, (string g) =>
//            {
//                if (g.Equals(value.ToLower()))
//                {
//                    return true;
//                }
//                else
//                {
//                    return false;
//                }
//            }) > -1)
//            {
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }

//        public override string ToString()
//        {
//            return this.Name;
//        }
//    }
//}
