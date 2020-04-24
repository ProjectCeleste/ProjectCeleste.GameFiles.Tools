using System.IO;

namespace ProjectCeleste.GameFiles.Tools.Bar
{
    public class BarFile
    {
        public BarFile(BarFileHeader barFileHeader, BarFileBody barFileBody)
        {
            BarFileHeader = barFileHeader;
            BarFileBody = barFileBody;
        }

        public BarFile(BinaryReader binaryReader)
        {
            BarFileHeader = new BarFileHeader(binaryReader);
            binaryReader.BaseStream.Position = BarFileHeader.FilesTableOffset;
            BarFileBody = new BarFileBody(binaryReader);
        }

        public BarFileHeader BarFileHeader { get; }

        public BarFileBody BarFileBody { get; }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(BarFileHeader.ToByteArray());
                bw.Write(BarFileBody.ToByteArray());
            }
            return ms.ToArray();
        }
    }
}