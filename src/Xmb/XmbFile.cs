using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ProjectCeleste.GameFiles.Tools.Xmb
{
    public sealed class XmbFile
    {
        private XmbFile(IEnumerable<string> elementNames, IEnumerable<string> attributeNames,
            XmbElement root)
        {
            ElementNames = elementNames as List<string> ?? elementNames.ToList();
            AttributeNames = attributeNames as List<string> ?? attributeNames.ToList();
            RootNode = root;
        }

        public IReadOnlyList<string> ElementNames { get; }

        public IReadOnlyList<string> AttributeNames { get; }

        public XmbElement RootNode { get; }

        public static XmbFile Deserialize(BinaryReader reader)
        {
            //Header
            var identifier1 = new string(reader.ReadChars(2));
            if (identifier1 != "X1")
                throw new InvalidDataException("'X1' not found - Invalid XMB file!");

            reader.ReadInt32(); //DataLength;

            //Body
            var identifierRoot = new string(reader.ReadChars(2));
            if (identifierRoot != "XR")
                throw new InvalidDataException("'XR' not found - Invalid XMB file!");

            var unk0 = reader.ReadInt32();
            if (unk0 != 4)
                throw new InvalidDataException("'4' not found  - Invalid XMB file!");

            var gameId= reader.ReadInt32(); //7=AOM, 8=AOE3/AOEO
            if (gameId != 8)
                throw new InvalidDataException("'8' not found  - Invalid XMB file!");

            var elementCount = reader.ReadInt32();
            var elementNames = new List<string>();
            for (var index = 0; index < elementCount; ++index)
            {
                var strLength = reader.ReadUInt32();
                var str = Encoding.Unicode.GetString(reader.ReadBytes((int) strLength * 2));

                elementNames.Add(str);
            }

            var attributeCount = reader.ReadInt32();
            var attributeNames = new List<string>();
            for (var index = 0; index < attributeCount; ++index)
            {
                var strLength = reader.ReadUInt32();
                var str = Encoding.Unicode.GetString(reader.ReadBytes((int) strLength * 2));

                attributeNames.Add(str);
            }

            var xmbElement = XmbElement.Deserialize(reader);

            return new XmbFile(elementNames, attributeNames, xmbElement);
        }

        public static XmbFile Deserialize(XDocument xmbDocument)
        {
            IList<string> elementNames = new List<string>();
            IList<string> attributeNames = new List<string>();
            var rootNode = XmbElement.Deserialize(xmbDocument.Root, ref elementNames, ref attributeNames);
            return new XmbFile(elementNames, attributeNames, rootNode);
        }

        public byte[] ToByteArray()
        {
            //BODY
            byte[] body;
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write("XR".ToCharArray());
                    bw.Write(4);
                    bw.Write(8);
                    bw.Write(ElementNames.Count);
                    foreach (var elementName in ElementNames)
                    {
                        bw.Write((uint) elementName.Length);
                        bw.Write(Encoding.Unicode.GetBytes(elementName));
                    }

                    bw.Write(AttributeNames.Count);
                    foreach (var attributeName in AttributeNames)
                    {
                        bw.Write((uint) attributeName.Length);
                        bw.Write(Encoding.Unicode.GetBytes(attributeName));
                    }

                    bw.Write(RootNode.ToByteArray());
                }

                body = ms.ToArray();
            }

            //Header
            byte[] header;
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write("X1".ToCharArray());
                    bw.Write(body.Length);
                }

                header = ms.ToArray();
            }

            //Final
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(header);
                    bw.Write(body);
                }

                return ms.ToArray();
            }
        }

        public static bool IsXmlFile(string fileName)
        {
            try
            {
                new XmlDocument().Load(fileName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsXmbFile(string fileName)
        {
            using var fileStream = File.OpenRead(fileName);
            return IsXmbFile(fileStream);
        }

        public static bool IsXmbFile(byte[] data)
        {
            using var memoryStream = new MemoryStream(data, false);
            return IsXmbFile(memoryStream);
        }

        public static bool IsXmbFile(Stream stream)
        {
            using var reader = new BinaryReader(stream);
            return IsXmbFile(reader);
        }

        public static bool IsXmbFile(BinaryReader reader)
        {
            var oldPosition = reader.BaseStream.Position;
            try
            {
                //Header
                var identifier1 = new string(reader.ReadChars(2));
                if (identifier1 != "X1")
                    throw new InvalidDataException("'X1' not found - Invalid XMB file!");

                reader.ReadInt32(); //DataLength;

                //Body
                var identifierRoot = new string(reader.ReadChars(2));
                if (identifierRoot != "XR")
                    throw new InvalidDataException("'XR' not found - Invalid XMB file!");

                var unk0 = reader.ReadInt32();
                if (unk0 != 4)
                    throw new InvalidDataException("'4' not found  - Invalid XMB file!");

                var gameId = reader.ReadInt32(); //7=AOM, 8=AOE3/AOEO
                if (gameId != 8)
                    throw new InvalidDataException("'8' not found  - Invalid XMB file!");

                return true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
            {
                return false;
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                reader.BaseStream.Position = oldPosition;
            }
        }
    }
}