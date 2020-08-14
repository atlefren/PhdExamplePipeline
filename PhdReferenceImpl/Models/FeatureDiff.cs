using System.IO;
using System.Text;

namespace PhdReferenceImpl.Models
{
    public class FeatureDiff
    {
        

        public string AttributeDiff { get; set; }
        public byte[] GeometryDiff { get; set; }

        public static FeatureDiff Deserialize(byte[] bytes)
        {
            using var reader = new BinaryReader(new MemoryStream(bytes));
            
            var geom = ReadBytes(reader);
            var attribsBytes = ReadBytes(reader);
            var attribs = Encoding.UTF8.GetString(attribsBytes, 0, attribsBytes.Length);

            return new FeatureDiff()
            {
                AttributeDiff = attribs,
                GeometryDiff = geom
            };
        }

        public byte[] Serialize()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            WriteBytes(writer, GeometryDiff);
            WriteBytes(writer, Encoding.UTF8.GetBytes(AttributeDiff));
            writer.Flush();
            stream.Flush();
            return stream.ToArray();
        }

        private static void WriteBytes(BinaryWriter writer, byte[] bytes)
        {
            writer.Write(bytes.Length);
            writer.Write(bytes, 0, bytes.Length);
        }

        private static byte[] ReadBytes(BinaryReader reader)
        {
            var numBytes = (int) reader.ReadUInt32();
            return reader.ReadBytes(numBytes);
        }

    }
}
