using System;
using System.Threading.Tasks;
using System.IO;
using ProtoBuf;

namespace BridgeChat.Protocol
{
    public static class Util
    {
        public static byte[] ProtobufSerialize<T>(T t)
        {
            using (var memstream = new MemoryStream()) {
                Serializer.SerializeWithLengthPrefix(memstream, t, PrefixStyle.Fixed32);
                return memstream.ToArray();
            }
        }

        // layering sync calls on top of async isn't nice, I know :/
        public static Task<T> AsyncProtobufRead<T>(Stream stream)
        {
            return Task.Run(() => Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Fixed32));
        }
    }
}

