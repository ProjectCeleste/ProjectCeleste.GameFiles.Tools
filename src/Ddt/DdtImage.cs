using System;

namespace ProjectCeleste.GameFiles.Tools.Ddt
{
    public class DdtImage
    {
        public DdtImage(int width, int height, int offset, int length, byte[] rawData)
        {
            if (rawData == null)
                throw new ArgumentNullException(nameof(rawData));
            if (rawData.Length != length)
                throw new ArgumentOutOfRangeException(nameof(length), length, @"rawData.Length != length");
            Width = width;
            Height = height;
            Offset = offset;
            RawData = rawData;
        }

        public int Width { get; }
        public int Height { get; }
        public int Offset { get; }
        public int Length => RawData?.Length ?? 0;
        public byte[] RawData { get; }
    }
}