using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace psd_importer
{
    class TextEngineNode
    {
        public const String TYPE_VALUE = "type value";
		public const String TYPE_STRUCTURE = "type structure";
		
		public string type;
		public string key;
		public string value;
        public List<TextEngineNode> structure = new List<TextEngineNode>();

        public TextEngineNode getNodeByKey(String key)
		{
            return structure.First(node => node.key == key);
		}

        //returns a node that is nested somewhere inside this node, the path to the nested node
        //is described by a series of node names giben in 'keys'
        public TextEngineNode getNestedNodeByKey(String[] keys)
        {
            TextEngineNode tempNode = this;

            foreach (string key in keys)
            {
                if (tempNode == null)
                {
                    return null;
                }
                else
                {
                    tempNode = tempNode.getNodeByKey(key);
                }
            }

            return tempNode;
        }
    }
}
