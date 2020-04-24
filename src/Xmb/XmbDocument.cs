using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProjectCeleste.GameFiles.Tools.Xml;

namespace ProjectCeleste.GameFiles.Tools.Xmb
{
    public sealed class XmbDocument
    {
        private readonly XDocument _xDocument;

        private XmbDocument(XDocument xDocument)
        {
            _xDocument = xDocument;
        }

        public static XmbDocument ParseXmbFile(string inputFilePath)
        {
            using var fileStream = File.Open(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return ParseXmb(fileStream);
        }

        public static XmbDocument ParseXmb(byte[] data)
        {
            using var stream = new MemoryStream(data);
            return ParseXmb(stream);
        }

        public static XmbDocument ParseXmb(Stream stream)
        {
            using var reader = new BinaryReader(stream);
            return ParseXmb(reader);
        }

        public static XmbDocument ParseXmb(BinaryReader reader)
        {
            var xmb = XmbFile.Deserialize(reader);
            return ParseXmb(xmb);
        }

        public static XmbDocument ParseXmb(XmbFile xmb)
        {
            var xml = xmb.RootNode.ToXml(xmb.ElementNames, xmb.AttributeNames);
            xml = XmlFileUtils.PrettyXml(xml);
            return new XmbDocument(XDocument.Parse(xml,
                LoadOptions.SetBaseUri | LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace));
        }

        public static XmbDocument ParseXmlFile(string inputFilePath)
        {
            var xml = File.ReadAllText(inputFilePath, Encoding.UTF8);
            return ParseXml(xml);
        }

        public static XmbDocument ParseXml(string xml)
        {
            return new XmbDocument(XDocument.Parse(xml,
                LoadOptions.SetBaseUri | LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace));
        }

        public void SaveXmb(string outputFilePath)
        {
            File.WriteAllBytes(outputFilePath, GetXmb());
        }

        public byte[] GetXmb()
        {
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(XmbFile.Deserialize(_xDocument).ToByteArray());
            }

            return ms.ToArray();
        }

        public void SaveXml(string outputFilePath)
        {
            File.WriteAllText(outputFilePath, GetXml(), Encoding.UTF8);
        }

        public string GetXml()
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                ConformanceLevel = ConformanceLevel.Document
            };

            string result;
            using (StringWriter sw = new CustomEncodingStringWriter(Encoding.UTF8))
            {
                using (var xw = XmlWriter.Create(sw, settings))
                {
                    _xDocument.Save(xw);
                }

                result = sw.ToString();
            }

            return result;
        }
    }
}