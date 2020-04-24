using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace ProjectCeleste.GameFiles.Tools.Xmb
{
    public sealed class XmbAttribute
    {
        private XmbAttribute(int nameId, string value)
        {
            NameId = nameId;
            Value = value;
        }

        public int NameId { get; }

        public string Value { get; }

        public static XmbAttribute Deserialize(BinaryReader reader)
        {
            var nameId = reader.ReadInt32();
            var valueLength = reader.ReadUInt32();
            var value = Encoding.Unicode.GetString(reader.ReadBytes((int) valueLength * 2));
            return new XmbAttribute(nameId, value);
        }

        public static XmbAttribute Deserialize(XAttribute nodeAttribute, ref IList<string> attributeNames)
        {
            var name =
                (!string.IsNullOrWhiteSpace(nodeAttribute.Name.NamespaceName)
                    ? nodeAttribute.Name.NamespaceName + ":" : string.Empty)
                + nodeAttribute.Name.LocalName;

            int nameId;
            if (attributeNames.Contains(name))
            {
                nameId = attributeNames.IndexOf(name);
            }
            else
            {
                attributeNames.Add(name);
                nameId = attributeNames.Count - 1;
            }

            return new XmbAttribute(nameId, nodeAttribute.Value);
        }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(NameId);
                bw.Write((uint) Value.Length);
                bw.Write(Encoding.Unicode.GetBytes(Value));
            }

            return ms.ToArray();
        }
    }
}