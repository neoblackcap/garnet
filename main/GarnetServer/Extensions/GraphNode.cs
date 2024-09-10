using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using Garnet.server;
using System.Collections;
using System.Linq;
using Garnet.networking;

namespace Garnet
{
    class GraphNode : IEnumerable<GraphNode>
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

        public GraphNode(BinaryReader reader)
        {
            Deserialize(reader);
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
            int headerLen = Name.Length + Value.Length + Id.Length + (3 * sizeof(int));
            var ajacencyNodeIds = ajacencyNodes.Select((node) => node.Id);
            int nodeLen = ajacencyNodes.Select(node => node.Id.Length).Sum();
            byte[] nodes = ajacencyNodes.Select(node => node.Id).Aggregate((left, right) =>
            {
                byte[] result = new byte[left.Length + right.Length];
                Buffer.BlockCopy(left, 0, result, 0, left.Length);
                Buffer.BlockCopy(right, 0, result, left.Length, right.Length);
                return result;
            });

            int totalLen = headerLen + nodeLen;
            writer.Write(totalLen);
            writer.Write(Name.Length);
            writer.Write(Name);
            writer.Write(Value.Length);
            writer.Write(Value);
            writer.Write(Id.Length);
            writer.Write(Id);
            writer.Write(nodes);
        }

        public void Deserialize(BinaryReader reader)
        {
            int totalLen = reader.ReadInt32();
            Name = reader.ReadBytes(reader.ReadInt32());
            Value = reader.ReadBytes(reader.ReadInt32());
            Id = reader.ReadBytes(reader.ReadInt32());

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

        // override object.Equals
        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (GraphNode)obj;
            var comparer = ByteArrayComparer.Instance;
            return comparer.Equals(Id, other.Id);
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}