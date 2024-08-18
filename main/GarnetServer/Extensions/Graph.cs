using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Garnet.server;
using Tsavorite.core;

namespace Garnet
{

    class Graph : CustomObjectBase
    {
        private readonly Dictionary<byte[], byte[]> _childrenDict;
        private readonly GraphNode _root;

        public Graph(byte type)
            : base(type, 0, MemoryUtils.DictionaryOverhead)
        {
            _childrenDict = new(ByteArrayComparer.Instance);
        }

        public Graph(byte type, BinaryReader reader)
            : base(type, reader, MemoryUtils.DictionaryOverhead)
        {
            _childrenDict = new(ByteArrayComparer.Instance);

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadBytes(reader.ReadInt32());
                var value = reader.ReadBytes(reader.ReadInt32());
                _childrenDict.Add(key, value);

                UpdateSize(key, value);
            }
        }

        public Graph(Graph obj)
            : base(obj)
        {
            _childrenDict = obj._childrenDict;
        }

        public override CustomObjectBase CloneObject() => new Graph(this);


        public override void SerializeObject(BinaryWriter writer)
        {
            writer.Write(_childrenDict.Count);
            foreach (var kvp in _childrenDict)
            {
                writer.Write(kvp.Key.Length);
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                writer.Write(kvp.Value);
            }
        }

        public override void Dispose() { }



        public bool Add(byte[] parent, byte[] child) 
        {

            return true;

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
        public override unsafe void Scan(long start, out List<byte[]> items, out long cursor, int count = 10, byte* pattern = null, int patternLength = 0)
        {
            cursor = start;
            items = new();
            int index = 0;

            if (_childrenDict.Count < start)
            {
                cursor = 0;
                return;
            }

            foreach (var item in _childrenDict)
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
            if (cursor == _childrenDict.Count)
                cursor = 0;
        }

        public bool Set(byte[] key, byte[] value)
        {
            if (_childrenDict.TryGetValue(key, out var oldValue))
            {
                UpdateSize(key, oldValue, false);
            }

            _childrenDict[key] = value;
            UpdateSize(key, value);
            return true;
        }

        private void UpdateSize(byte[] key, byte[] value, bool add = true)
        {
            var size = Utility.RoundUp(key.Length, IntPtr.Size) + Utility.RoundUp(value.Length, IntPtr.Size)
                + (2 * MemoryUtils.ByteArrayOverhead) + MemoryUtils.DictionaryEntryOverhead;
            this.Size += add ? size : -size;
            Debug.Assert(this.Size >= MemoryUtils.DictionaryOverhead);
        }

        public bool TryGetValue(byte[] key, [MaybeNullWhen(false)] out byte[] value)
        {
            return _childrenDict.TryGetValue(key, out value);
        }
    }
}