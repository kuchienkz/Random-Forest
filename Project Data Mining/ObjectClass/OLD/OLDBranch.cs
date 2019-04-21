//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Project_Data_Mining.ObjectClass
//{
//    public class Branch
//    {
//        public List<TreeNode> Items;

//        public Branch()
//        {
//            this.Items = new List<TreeNode>();
//        }

//        public Branch(int initialSize) : this()
//        {
//            for (int i = 0; i < initialSize; i++)
//            {
//                this.Items.Add(new TreeNode());
//            }
//        }

//        public TreeNode FindByValue(string value)
//        {
//            foreach (var item in Items)
//            {
//                if (item.Value.Equals(value))
//                {
//                    return item;
//                }
//            }
//            return null;
//        }

//        public void RemoveChildItem(TreeNode nodeToRemove)
//        {
//            Items.Remove(nodeToRemove);
//        }
//    }
//}
