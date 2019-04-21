//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Project_Data_Mining.ObjectClass
//{
//    public class OLDTreeNode
//    {
//        private Object data;
//        private Branch children = null;

//        public OLDTreeNode()
//        {

//        }

//        public OLDTreeNode(int nrOfChildren)
//        {
//            this.children = new Branch(nrOfChildren);
//        }

//        public OLDTreeNode(Object data) : this(data, null)
//        {

//        }
//        public OLDTreeNode(Object data, Branch children)
//        {
//            Value = data;
//            this.children = children;
//        }

//        public Object Value
//        {
//            get
//            {
//                if (this.data == null)
//                {
//                    return "NULL";
//                }
//                else if (this.data.GetType() == typeof(String))
//                {
//                    return (string)this.data;
//                }
//                else
//                {
//                    return (Attribute)this.data;
//                }
//            }
//            set { this.data = value; }
//        }

//        public OLDTreeNode this[int index]
//        {
//            get
//            {
//                if (this.children == null || children.Items.Count + 1 < index)
//                {
//                    return null;
//                }
//                else
//                {
//                    return this.children.Items[index];
//                }
//            }
//            set
//            {
//                if (this.children == null)
//                {
//                    this.children = new Branch(index);
//                    this.children.Items[index] = value;
//                }
//                else
//                {
//                    this.children.Items[index] = value;
//                }
//            }
//        }

//        public int Children
//        {
//            set
//            {
//                this.children = new Branch(value);
//            }
//        }

//        public List<OLDTreeNode> GetAllChildrenOfANode
//        {
//            get
//            {
//                if (this.children != null)
//                {
//                    return this.children.Items;
//                }
//                else
//                {
//                    return null;
//                }
//            }
//        }

//        public void RemoveSpecifiedChild(OLDTreeNode nodeToRemove)
//        {
//            this.children.RemoveChildItem(nodeToRemove);
//        }
//    }
//}
