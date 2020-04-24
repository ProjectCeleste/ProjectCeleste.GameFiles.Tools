using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ProjectCeleste.GameFiles.Tools.Bar
{
    public class BarFileBody
    {
        public BarFileBody(string rootPath, IEnumerable<BarEntry> barEntries)
        {
            RootPath = rootPath;
            var entries = barEntries as List<BarEntry> ?? barEntries.ToList();
            NumberOfRootFiles = (uint)entries.Count;
            Entries = entries;
        }

        public BarFileBody(BinaryReader binaryReader)
        {
            var rootNameLength = binaryReader.ReadUInt32();
            RootPath = Encoding.Unicode.GetString(binaryReader.ReadBytes((int) rootNameLength * 2));
            NumberOfRootFiles = binaryReader.ReadUInt32();
            var barFileEntries = new List<BarEntry>();
            for (uint i = 0; i < NumberOfRootFiles; i++)
                barFileEntries.Add(new BarEntry(binaryReader));
            Entries = new ReadOnlyCollection<BarEntry>(barFileEntries);
        }

        public string RootPath { get; }

        public uint NumberOfRootFiles { get; }

        public IReadOnlyCollection<BarEntry> Entries { get; }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(RootPath.Length);
            bw.Write(Encoding.Unicode.GetBytes(RootPath));
            bw.Write(NumberOfRootFiles);
            foreach (var barFileEntry in Entries)
                bw.Write(barFileEntry.ToByteArray());
            return ms.ToArray();
        }
    }
}