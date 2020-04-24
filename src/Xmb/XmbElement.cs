using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProjectCeleste.GameFiles.Tools.Xml;

namespace ProjectCeleste.GameFiles.Tools.Xmb
{
    public sealed class XmbElement
    {
        private XmbElement(int nameId, string text, int lineNumber,
            IEnumerable<XmbElement> childrenElements, IEnumerable<XmbAttribute> attributes)
        {
            NameId = nameId;
            Value = text;
            LineNumber = lineNumber;
            ChildrenElements = childrenElements as List<XmbElement> ?? childrenElements.ToList();
            Attributes = attributes as List<XmbAttribute> ?? attributes.ToList();
        }

        public int NameId { get; }

        public string Value { get; }

        public int LineNumber { get; }

        public IReadOnlyList<XmbAttribute> Attributes { get; }

        public IReadOnlyList<XmbElement> ChildrenElements { get; }

        public static XmbElement Deserialize(BinaryReader reader)
        {
            //HEAD
            var head = new string(reader.ReadChars(2));
            if (head != "XN")
                throw new Exception("Invalid Node (head does not equal XN)");

            reader.ReadInt32(); //DataLength

            //BODY
            var txtLength = reader.ReadUInt32();
            var text = Encoding.Unicode.GetString(reader.ReadBytes((int) txtLength * 2));
            var nameId = reader.ReadInt32();
            var lineNumber = reader.ReadInt32(); //LineNum

            var attrs = new List<XmbAttribute>();
            var num1 = reader.ReadInt32();
            for (var index1 = 0; num1 > index1; ++index1)
            {
                var attribute = XmbAttribute.Deserialize(reader);
                attrs.Add(attribute);
            }

            var childrenElements = new List<XmbElement>();
            var num2 = reader.ReadInt32();
            for (var index = 0; num2 > index; ++index)
            {
                var childrenElement = Deserialize(reader);
                childrenElements.Add(childrenElement);
            }

            return new XmbElement(nameId, text, lineNumber, childrenElements, attrs);
        }

        public static XmbElement Deserialize(XElement xmlElement, ref IList<string> elementNames,
            ref IList<string> attributeNames)
        {
            int nameId;
            if (elementNames.Contains(xmlElement.Name.LocalName))
            {
                nameId = elementNames.IndexOf(xmlElement.Name.LocalName);
            }
            else
            {
                elementNames.Add(xmlElement.Name.LocalName);
                nameId = elementNames.Count - 1;
            }

            var value = string.Empty;
            var textNode = xmlElement.Nodes().FirstOrDefault(key => key.NodeType == XmlNodeType.Text);
            if (textNode != null)
            {
                value = XmlFileUtils.FromXmlString(textNode.ToString());
            }

            IList<XmbAttribute> attributes = new List<XmbAttribute>();
            if (xmlElement.HasAttributes)
            {
                foreach (var attribute in xmlElement.Attributes())
                {
                    attributes.Add(XmbAttribute.Deserialize(attribute, ref attributeNames));
                }
            }

            IList<XmbElement> elements = new List<XmbElement>();
            if (xmlElement.HasElements)
            {
                foreach (var element in xmlElement.Elements())
                {
                    elements.Add(Deserialize(element, ref elementNames, ref attributeNames));
                }
            }

            var lineNumber = ((IXmlLineInfo) xmlElement).LineNumber;

            return new XmbElement(nameId, value, lineNumber, (List<XmbElement>) elements,
                (List<XmbAttribute>) attributes);
        }

        public byte[] ToByteArray()
        {
            //BODY
            byte[] body;
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((uint) Value.Length);
                    bw.Write(Encoding.Unicode.GetBytes(Value));
                    bw.Write(NameId);
                    bw.Write(LineNumber);
                    bw.Write(Attributes.Count);
                    foreach (var attribute in Attributes)
                    {
                        bw.Write(attribute.ToByteArray());
                    }

                    bw.Write(ChildrenElements.Count);
                    foreach (var element in ChildrenElements)
                    {
                        bw.Write(element.ToByteArray());
                    }
                }

                body = ms.ToArray();
            }

            //Header
            byte[] header;
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write("XN".ToCharArray());
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

        public string ToXml(IReadOnlyList<string> elementNames,
            IReadOnlyList<string> attrNames, string indent = "")
        {

            var str1 = "\r\n" + indent + "<" + elementNames[NameId];
            foreach (var t in Attributes)
            {
                var name = attrNames[t.NameId];
                var value = XmlFileUtils.ToXmlString(t.Value, true);
                str1 = str1 + " " + name + "=\"" + value + "\"";
            }

            string str2;
            if (!string.IsNullOrWhiteSpace(Value))
            {
                var text = XmlFileUtils.ToXmlString(Value.Trim());

                if (ChildrenElements.Count > 0)
                {
                    var str3 = str1 + ">\r\n" + indent + "\t" + text ;
                    str3 = ChildrenElements.Aggregate(str3,
                        (current, t) => current + t.ToXml(elementNames, attrNames, indent + "\t"));
                    str2 = str3 + "\r\n" + indent + "</" + elementNames[NameId] + ">";
                }
                else
                {
                    str2 = str1 + ">" + text + "</" + elementNames[NameId] + ">";
                }
            }
            else if (ChildrenElements.Count > 0)
            {
                var str3 = str1 + ">";
                str3 = ChildrenElements.Aggregate(str3,
                    (current, t) => current + t.ToXml(elementNames, attrNames, indent + "\t"));
                str2 = str3 + "\r\n" + indent + "</" + elementNames[NameId] + ">";
            }
            else
            {
                str2 = str1 + "/>";
            }

            return str2;
        }
    }
}