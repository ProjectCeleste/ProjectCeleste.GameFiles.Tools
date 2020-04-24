namespace ProjectCeleste.GameFiles.Tools.Xmb
{
    public static class XmbFileUtils
    { 
        public static string XmbToXml(byte[] xmb)
        {
            var xmbDocument = XmbDocument.ParseXmb(xmb);
            return xmbDocument.GetXml();
        }

        public static void XmbFileToXmlFile(string xmbFilePath, string xmlFilePath)
        {
            var xmbDocument = XmbDocument.ParseXmbFile(xmbFilePath);
            xmbDocument.SaveXml(xmlFilePath);
        }

        public static byte[] XmlToXmb(string xml)
        {
            var xmbDocument = XmbDocument.ParseXml(xml);
            return xmbDocument.GetXmb();
        }

        public static void XmlFileToXmbFile(string xmlFilePath, string xmbFilePath)
        {
            var xmbDocument = XmbDocument.ParseXmlFile(xmlFilePath);
            xmbDocument.SaveXmb(xmbFilePath);
        }
    }
}