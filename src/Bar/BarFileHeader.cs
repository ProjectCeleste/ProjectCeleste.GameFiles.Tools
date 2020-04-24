using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjectCeleste.GameFiles.Tools.Utils;

namespace ProjectCeleste.GameFiles.Tools.Bar
{
    public class BarFileHeader
    {
        public BarFileHeader(string fileName, IEnumerable<FileInfo> fileInfos)
        {
            Espn = "ESPN";
            Unk0 = 2;
            Unk1 = 0x44332211;
            Unk2 = new byte[66 * 4];
            Checksum = 0;
            var enumerable = fileInfos as FileInfo[] ?? fileInfos.ToArray();
            NumberOfFiles = (uint)enumerable.Length;
            FilesTableOffset = 292 + (uint) enumerable.Sum(key => key.Length);
            FileNameHash = Encoding.Default.GetBytes(fileName.ToUpper()).GetSuperFastHash();
        }

        public BarFileHeader(BinaryReader binaryReader)
        {
            var espn = new string(binaryReader.ReadChars(4));
            if (espn != "ESPN")
                throw new Exception("File is not a valid BAR Archive");

            Espn = espn;

            Unk0 = binaryReader.ReadUInt32();

            Unk1 = binaryReader.ReadUInt32();

            Unk2 = binaryReader.ReadBytes(66 * 4);

            Checksum = binaryReader.ReadUInt32();

            NumberOfFiles = binaryReader.ReadUInt32();

            FilesTableOffset = binaryReader.ReadUInt32();

            FileNameHash = binaryReader.ReadUInt32();
        }

        public string Espn { get; }

        public uint Unk0 { get; }

        public uint Unk1 { get; }

        public IReadOnlyCollection<byte> Unk2 { get; }

        public uint Checksum { get; }

        public uint NumberOfFiles { get; }

        public uint FilesTableOffset { get; }

        public uint FileNameHash { get; }

        //
        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Espn.ToCharArray());
            bw.Write(Unk0);
            bw.Write(Unk1);
            bw.Write(Unk2.ToArray());
            bw.Write(Checksum);
            bw.Write(NumberOfFiles);
            bw.Write(FilesTableOffset);
            bw.Write(FileNameHash);
            return ms.ToArray();
        }
    }
}