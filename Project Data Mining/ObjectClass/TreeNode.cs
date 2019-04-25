using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Data_Mining.ObjectClass
{
    public class TreeNode
    {
        public TreeNode(string name, int tableIndex, Feature nodeAttribute, string edge)
        {
            Name = name;
            TableIndex = tableIndex;
            NodeAttribute = nodeAttribute;
            ChildNodes = new List<TreeNode>();
            Edge = edge;
        }

        public TreeNode(bool isleaf, string name, string edge)
        {
            IsLeaf = isleaf;
            Name = name;
            Edge = edge;
        }

        public string Name { get; }

        public string Edge { get; }

        public Feature NodeAttribute { get; }

        public List<TreeNode> ChildNodes { get; }

        public int TableIndex { get; }

        public bool IsLeaf { get; }
    }
}
