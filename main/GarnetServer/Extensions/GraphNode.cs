using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using Garnet.server;
using System.Collections;

namespace Garnet
{
    class GraphNode: IEnumerable<GraphNode>
    {
        public byte[] Name { get; set; }
        public byte[] Value { get; set; }

        public byte[] Id { get; set; }

        public bool IsLeaf
        {
            get
            {
                return ajacencyNodes.Count == 0;
            }
        }

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

        public void Serialize(BinaryWriter writer)
        {
            int len = Name.Length + Value.Length + Id.Length;

        }

        public IEnumerator<GraphNode> GetEnumerator()
        {
            for (var i = 0; i < ajacencyNodes.Count; i++)
            {
                yield return ajacencyNodes[i];
                
            }

        }

        IEnumerator IEnumerable.GetEnumerator() 
        {
            return GetEnumerator();
        }
    }
}