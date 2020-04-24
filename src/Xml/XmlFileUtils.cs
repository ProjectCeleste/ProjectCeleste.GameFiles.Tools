#region Using directives

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

#endregion

namespace ProjectCeleste.GameFiles.Tools.Xml
{
    public class CustomEncodingStringWriter : StringWriter
    {
        public CustomEncodingStringWriter(Encoding encoding)
        {
            Encoding = encoding;
        }

        public override Encoding Encoding { get; }
    }

    public static class XmlFileUtils
    {
        #region Misc

        public static string PrettyXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentNullException(nameof(xml));
            var xDoc = XDocument.Parse(xml,
                LoadOptions.SetBaseUri | LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            string output;
            using (var stringWriter = new CustomEncodingStringWriter(Encoding.UTF8))
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    IndentChars = "\t",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace,
                    ConformanceLevel = ConformanceLevel.Document
                }))
                {
                    xDoc.Save(xmlWriter);
                }
                output = stringWriter.ToString();
            }
            return output;
        }

        public static string ToXmlString(string text, bool isAttribute = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var sb = new StringBuilder();
            for (var position = 0; position < text.Length; position++)
            {
                var chr = text[position];
                switch (chr)
                {
                    case '<':
                    {
                        sb.Append("&lt;");
                        break;
                    }
                    case '>':
                    {
                        sb.Append("&gt;");
                        break;
                    }
                    case '&':
                    {
                        sb.Append("&amp;");
                        break;
                    }
                    case '\"':
                    {
                        sb.Append(isAttribute ? "&quot;" : "\"");
                        break;
                    }
                    case '\'':
                    {
                        sb.Append(isAttribute ? "&apos;" : "\'");
                        break;
                    }
                    case '\n':
                    {
                        sb.Append(isAttribute ? "&#xA;" : "\n");
                        break;
                    }
                    case '\r':
                    {
                        sb.Append(isAttribute ? "&#xD;" : "\r");
                        break;
                    }
                    case '\t':
                    {
                        sb.Append(isAttribute ? "&#x9;" : "\t");
                        break;
                    }
                    default:
                    {
                        if (chr < 32)
                            throw new InvalidDataException(
                                $"Invalid character '{chr} (Chr {Convert.ToInt16(chr)})' at position '{position}'");
                        sb.Append(chr);
                        break;
                    }
                }
            }

            return sb.ToString();
        }

        public static string FromXmlString(string text, bool isAttribute = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
                .Replace("&quot;", "\"").Replace("&apos;", "'");
            
            if(isAttribute)
                text = text.Replace("&#xA;", "\n").Replace("&#xD;", "\r").Replace("&#x9;", "\t");

            return text;
        }

        #endregion

        #region Serialize

        public static void SerializeToXmlFile(this object serializableObject, string xmlFilePath,
            bool autoBackup = false, int backupMaxCount = 10)
        {
            if (serializableObject == null)
                throw new ArgumentNullException(nameof(serializableObject));

            if (string.IsNullOrWhiteSpace(xmlFilePath))
                throw new ArgumentNullException(nameof(xmlFilePath));

            var xml = SerializeToXml(serializableObject);
            var dir = Path.GetDirectoryName(xmlFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (autoBackup && File.Exists(xmlFilePath))
            {
                var backupFile = $"{xmlFilePath}.{DateTime.UtcNow.ToFileTimeUtc():X8}.bak";

                if (File.Exists(backupFile))
                    File.Delete(backupFile);

                File.Move(xmlFilePath, backupFile);

                // Cleanup Backup
                Directory.GetFiles(dir, $"{xmlFilePath}.*.bak", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTime)
                    .Skip(backupMaxCount)
                    .ToList()
                    .ForEach(File.Delete);
            }

            File.WriteAllText(xmlFilePath, xml, Encoding.UTF8);
        }

        public static string SerializeToXml(this object serializableObject)
        {
            if (serializableObject == null)
                throw new ArgumentNullException(nameof(serializableObject));

            string output;
            var serializer = new XmlSerializer(serializableObject.GetType());
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = true,
                NewLineHandling = NewLineHandling.None
            };
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);
            using (var stringWriter = new CustomEncodingStringWriter(Encoding.UTF8))
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    serializer.Serialize(xmlWriter, serializableObject, ns);
                }

                output = stringWriter.ToString();
            }

            return output;
        }

        #endregion

        #region Deserialize

        public static T DeserializeFromXmlFile<T>(string xmlFilePath) where T : class
        {
            if (string.IsNullOrWhiteSpace(xmlFilePath))
                throw new ArgumentNullException(nameof(xmlFilePath));

            return !File.Exists(xmlFilePath)
                ? throw new FileNotFoundException("File Not Found", xmlFilePath)
                : DeserializeFromXml<T>(File.ReadAllText(xmlFilePath, Encoding.UTF8));
        }

        public static T DeserializeFromXml<T>(string xml) where T : class
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentNullException(nameof(xml));

            T output;
            var xmls = new XmlSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                output = (T) xmls.Deserialize(ms);
            }

            return output;
        }

        #endregion
    }
}