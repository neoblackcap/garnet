using System.IO;
using Garnet.server;

namespace Garnet
{
    class GraphFactory : CustomObjectFactory
    {
        public override CustomObjectBase Create(byte type)
            => new Graph(type);

        public override CustomObjectBase Deserialize(byte type, BinaryReader reader)
            => new Graph(type, reader);
    }
}