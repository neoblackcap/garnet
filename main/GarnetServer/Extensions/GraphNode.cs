using System.Collections.Generic;

namespace Garnet
{
    class GraphNode 
    {
        public byte[] Value { get; set; }
        readonly List<GraphNode> ajacencyNodes;

        public GraphNode(byte[] value)
        {
            this.Value = value;
            ajacencyNodes = new();
        }

        public void Add(GraphNode node)
        {
            ajacencyNodes.Add(node);
        }

        public void Remove(GraphNode node)
        {
            ajacencyNodes.Remove(node);
        }

    }
}