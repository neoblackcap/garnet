using System.Collections.Generic;
using System;

namespace Garnet
{
    class GraphNode 
    {
        public byte[] Name { get; set; }
        public byte[] Value { get; set; }

        public byte[] Id {get;set;}
        readonly List<GraphNode> ajacencyNodes;

        public GraphNode(byte[] name, byte[] value)
        {
            Name = name;
            Value = value;
            Id = Guid.NewGuid().ToByteArray();
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