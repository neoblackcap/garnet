using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Transactions;
using Garnet.server;
using Tsavorite.core;
using Tsavorite.devices;

namespace Garnet
{
    class Graph : CustomObjectBase
    {
        private readonly GraphNode root;

        private readonly Dictionary<byte[], GraphNode> nodeNameDict;

        static readonly int OverHead = MemoryUtils.DictionaryOverhead + IntPtr.Size;

        public Graph(byte type)
            : base(type, 0, OverHead)
        {
            root = new(Guid.NewGuid().ToByteArray(), null);
        }

        public Graph(byte type, BinaryReader reader)
            : base(type, reader, OverHead)
        {
            nodeNameDict = new(ByteArrayComparer.Instance);
            root = new(Guid.NewGuid().ToByteArray(), null);

            DeserializeObject(reader);
        }

        public Graph(Graph obj)
            : base(obj)
        {
            nodeNameDict = obj.nodeNameDict;
            root = obj.root;
        }

        public override CustomObjectBase CloneObject() => new Graph(this);


        public override void SerializeObject(BinaryWriter writer)
        {
            var nodes = TopologicalSort();
            writer.Write(nodes.Count);
            foreach (var node in TopologicalSort())
            {
                node.Serialize(writer);
            }
        }

        public void DeserializeObject(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            var nodes = new List<GraphNode>(count);
            for (int i = 0; i < count; i++)
            {
                var node = new GraphNode(reader);
                node.Add(node);

                var key = reader.ReadBytes(reader.ReadInt32());
                var value = reader.ReadBytes(reader.ReadInt32());
                _nodeGraphDict.Add(key, value);

                UpdateSize(key, value);
            }

        }

        public override void Dispose()
        {
        }

        public List<GraphNode> TopologicalSort()
        {
            var compareFn = EqualityComparer<GraphNode>.Create((a, b) => ByteArrayComparer.Instance.Equals(a.Id, b.Id));
            var visited = new HashSet<GraphNode>(compareFn);
            var stack = new Stack<GraphNode>();

            stack.Push(root);
            var result = new List<GraphNode>();

            foreach (var node in stack)
            {
                if (!visited.Add(node))
                {
                    throw new Exception("Graph has cycle");
                }

                if (node.IsLeaf)
                {
                    result.Add(node);
                }
                else
                {
                    foreach (var ajaNode in node)
                        stack.Push(ajaNode);
                }

            }

            result.Reverse();
            return result;
        }


        public bool Add(byte[] parent, byte[] child, byte[] value)
        {
            var delimeter = Encoding.UTF8.GetBytes(":");
            var parentName = BytesJoin(root.Name, delimeter, parent);

            if (!nodeNameDict.TryGetValue(parentName, out var parentNode))
            {
                return false;
            }

            var node = new GraphNode(child, value);

            var childName = BytesJoin(root.Name, delimeter, parent);
            if (!nodeNameDict.TryAdd(childName, node))
            {
                return false;
            }

            parentNode.Add(node);

            return true;
        }

        private static byte[] BytesJoin(params IList<byte[]> contents)
        {
            var result = new byte[contents.Sum(x => x.Length)];

            long written = 0;
            foreach (var content in contents)
            {
                Array.Copy(content, 0, result, written, content.Length);
                written += content.Length;
            }

            return result;
        }

        /// <summary>
        /// Returns the items from this object using a cursor to indicate the start of the scan,
        /// a pattern to filter out the items to return, and a count to indicate the number of items to return.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="items"></param>
        /// <param name="cursor"></param>
        /// <param name="count"></param>
        /// <param name="pattern"></param>
        /// <param name="patternLength"></param>
        /// <returns></returns>
        public override unsafe void Scan(long start, out List<byte[]> items, out long cursor, int count = 10,
            byte* pattern = null, int patternLength = 0)
        {
            cursor = start;
            items = new();
            int index = 0;

            if (_nodeGraphDict.Count < start)
            {
                cursor = 0;
                return;
            }

            foreach (var item in _nodeGraphDict)
            {
                if (index < start)
                {
                    index++;
                    continue;
                }

                bool addToList = false;
                if (patternLength == 0)
                {
                    items.Add(item.Key);
                    addToList = true;
                }
                else
                {
                    fixed (byte* keyPtr = item.Key)
                    {
                        if (GlobUtils.Match(pattern, patternLength, keyPtr, item.Key.Length))
                        {
                            items.Add(item.Key);
                            addToList = true;
                        }
                    }
                }

                if (addToList)
                    items.Add(item.Value);

                cursor++;

                // Each item is a pair in the Dictionary but two items in the result List
                if (items.Count == (count * 2))
                    break;
            }

            // Indicates end of collection has been reached.
            if (cursor == _nodeGraphDict.Count)
                cursor = 0;
        }

        private void UpdateSize(byte[] key, byte[] value, bool add = true)
        {
            var keyLenSize = Utility.RoundUp(key.Length, IntPtr.Size);
            var valueLenSize = Utility.RoundUp(value.Length, IntPtr.Size);
            var kvSize = 2 * MemoryUtils.ByteArrayOverhead;
            var overhead = MemoryUtils.DictionaryEntryOverhead;
            var size = keyLenSize + valueLenSize + kvSize + overhead;
            this.Size += add ? size : -size;
            Debug.Assert(this.Size >= MemoryUtils.DictionaryOverhead);
        }

    }
}