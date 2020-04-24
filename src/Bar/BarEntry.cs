using System;
using System.IO;
using System.Text;

namespace ProjectCeleste.GameFiles.Tools.Bar
{
    public class BarEntry
    {
        public BarEntry(BinaryReader binaryReader)
        {
            Offset = binaryReader.ReadInt32();
            FileSize = binaryReader.ReadInt32();
            FileSize2 = binaryReader.ReadInt32();
            LastWriteTime = new BarEntryLastWriteTime(binaryReader);
            var length = binaryReader.ReadUInt32();
            FileName = Encoding.Unicode.GetString(binaryReader.ReadBytes((int) length * 2));
        }

        public BarEntry(string filename, int offset, int fileSize, BarEntryLastWriteTime modifiedDates)
        {
            FileName = filename;
            Offset = offset;
            FileSize = fileSize;
            FileSize2 = fileSize;
            LastWriteTime = modifiedDates;
        }

        public BarEntry(string rootPath, FileInfo fileInfo, int offset, bool ignoreLastWriteTime = true)
        {
            rootPath = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? rootPath
                : rootPath + Path.DirectorySeparatorChar;

            FileName = fileInfo.FullName.Replace(rootPath, string.Empty);
            Offset = offset;
            FileSize = (int) fileInfo.Length;
            FileSize2 = FileSize;
            LastWriteTime = ignoreLastWriteTime
                ? new BarEntryLastWriteTime(new DateTime(2011, 1, 1))
                : new BarEntryLastWriteTime(fileInfo.LastWriteTimeUtc);
        }

        public string FileName { get; }

        public int Offset { get; }

        public int FileSize { get; }

        public int FileSize2 { get; }

        public BarEntryLastWriteTime LastWriteTime { get; }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Offset);
            bw.Write(FileSize);
            bw.Write(FileSize2);
            bw.Write(LastWriteTime.ToByteArray());
            bw.Write(FileName.Length);
            bw.Write(Encoding.Unicode.GetBytes(FileName));
            return ms.ToArray();
        }
    }
}