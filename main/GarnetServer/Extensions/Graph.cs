using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Transactions;
using Garnet.server;
using Tsavorite.core;

namespace Garnet
{
    class Graph : CustomObjectBase
    {
        private readonly Dictionary<byte[], byte[]> _nodeGraphDict;
        private readonly GraphNode _root;
        private readonly Dictionary<byte[], GraphNode> _nodeNameDict;

        public Graph(byte type)
            : base(type, 0, MemoryUtils.DictionaryOverhead)
        {
            _nodeGraphDict = new(ByteArrayComparer.Instance);
            _root = new(Guid.NewGuid().ToByteArray(), null);
        }

        public Graph(byte type, BinaryReader reader)
            : base(type, reader, MemoryUtils.DictionaryOverhead)
        {
            _nodeGraphDict = new(ByteArrayComparer.Instance);

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadBytes(reader.ReadInt32());
                var value = reader.ReadBytes(reader.ReadInt32());
                _nodeGraphDict.Add(key, value);

                UpdateSize(key, value);
            }
        }

        // quicksort




        public Graph(Graph obj)
            : base(obj)
        {
            _nodeGraphDict = obj._nodeGraphDict;
        }

        public override CustomObjectBase CloneObject() => new Graph(this);


        public override void SerializeObject(BinaryWriter writer)
        {
            writer.Write(_nodeGraphDict.Count);
            foreach (var kvp in _nodeGraphDict)
            {
                writer.Write(kvp.Key.Length);
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                writer.Write(kvp.Value);
            }
        }

        public override void Dispose()
        {
        }


        public bool Add(byte[] parent, byte[] child, byte[] value)
        {
            var delimeter = Encoding.UTF8.GetBytes(":");
            long size = _root.Name.Length + delimeter.Length + child.Length;

            var name = new byte[size];

            Array.Copy(_root.Name, name, _root.Name.Length);
            Array.Copy(delimeter, 0, name, parent.Length, delimeter.Length);
            Array.Copy(parent, 0, name, _root.Name.Length + delimeter.Length, child.Length);

            if (!_nodeNameDict.TryGetValue(parent, out var parentNode))
            {
                return false;
            }

            var node = new GraphNode(child, value);

            if (!_nodeNameDict.TryAdd(child, node)) {
                return false;
            }

            parentNode.Add(node);

            return true;
        }

        private byte[] BytesJoin(params byte[][] contents)
        {
            var size = contents.Sum(x => x.Length);
            var result = new byte[size];
            long current = 0;

            for (int i = 0; i < contents.Length; i++)
            {
                Array.Copy(contents[i], 0, result, 0, contents[i].Length);
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

        public bool Set(byte[] key, byte[] value)
        {
            if (_nodeGraphDict.TryGetValue(key, out var oldValue))
            {
                UpdateSize(key, oldValue, false);
            }

            _nodeGraphDict[key] = value;
            UpdateSize(key, value);
            return true;
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

        public bool TryGetValue(byte[] key, [MaybeNullWhen(false)] out byte[] value)
        {
            return _nodeGraphDict.TryGetValue(key, out value);
        }
    }
}