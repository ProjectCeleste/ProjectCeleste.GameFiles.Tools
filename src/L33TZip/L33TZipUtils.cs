#region Using directives

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;

#endregion

namespace ProjectCeleste.GameFiles.Tools.L33TZip
{
    public static class L33TZipUtils
    {
        private const string L33tHeader = "l33t";
        private const string L66tHeader = "l66t";

        #region Check

        public static bool IsL33TZipFile(string fileName)
        {
            using (var fileStream = File.Open(fileName, FileMode.Open))
            {
                return StreamIsL33TZip(fileStream);
            }
        }

        public static bool IsL33TZipFile(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data, false))
            {
                return StreamIsL33TZip(memoryStream);
            }
        }

        private static bool StreamIsL33TZip(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                try
                {
                    var fileHeader = new string(reader.ReadChars(4));
                    return fileHeader == L33tHeader || fileHeader == L66tHeader;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        #endregion

        #region Create

        public static void CreateL33TZipFile(string inputFileName, string outputFileName)
        {
            try
            {
                using (var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                using (var outputFileStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var outputFileStreamWriter = new BinaryWriter(outputFileStream))
                using (var compressedStream = new DeflateStream(outputFileStream, CompressionMode.Compress, CompressionLevel.BestCompression))
                {
                    WriteFileHeaders(outputFileStreamWriter, fileStream.Length);
                    WriteCompressedStream(compressedStream, fileStream, outputFileStreamWriter);
                }
            }
            catch (Exception)
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }

        public static byte[] CreateL33TZip(string inputFileName)
        {
            using (var sourceFileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None))
            using (var outputStream = new MemoryStream())
            using (var outputStreamWriter = new BinaryWriter(outputStream))
            using (var compressedStream = new DeflateStream(outputStream, CompressionMode.Compress, CompressionLevel.BestCompression))
            {
                WriteFileHeaders(outputStreamWriter, sourceFileStream.Length);
                WriteCompressedStream(compressedStream, sourceFileStream, outputStreamWriter);
                return outputStream.ToArray();
            }
        }

        private static void WriteFileHeaders(BinaryWriter writer, long fileLength)
        {
            writer.BaseStream.Position = 0L;

            //Write L33T Header & File Length
            if (fileLength > int.MaxValue)
            {
                writer.Write(L66tHeader.ToCharArray());
                writer.Write(fileLength);
            }
            else
            {
                writer.Write(L33tHeader.ToCharArray());
                writer.Write(Convert.ToInt32(fileLength));
            }

            //Write Deflate specification (2 Byte)
            writer.Write(new byte[] { 0x78, 0x9C });
        }

        private static void WriteCompressedStream(
            DeflateStream compressedStream,
            FileStream sourceFileStream,
            BinaryWriter final,
            CancellationToken ct = default,
            IProgress<double> progress = null)
        {
            var buffer = new byte[4096];
            var totalBytesRead = 0L;
            int bytesRead;
            var fileLength = sourceFileStream.Length;

            while ((bytesRead = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();

                if (bytesRead > fileLength)
                {
                    totalBytesRead += fileLength;
                    compressedStream.Write(buffer, 0, Convert.ToInt32(fileLength));
                }
                else if (totalBytesRead + bytesRead <= fileLength)
                {
                    totalBytesRead += bytesRead;
                    compressedStream.Write(buffer, 0, bytesRead);
                }
                else if (totalBytesRead + bytesRead > fileLength)
                {
                    var leftToRead = fileLength - totalBytesRead;
                    totalBytesRead += leftToRead;
                    final.Write(buffer, 0, Convert.ToInt32(leftToRead));
                }

                progress?.Report((double)totalBytesRead / fileLength * 100);
                
                if (totalBytesRead >= fileLength)
                    break;
            }
        }

        public static async Task DoCreateL33TZipFile(string inputFileName, string outputFileName,
            CancellationToken ct = default,
            IProgress<double> progress = null)
        {
            try
            {
                using (var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                using (var fileStreamFinal = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var final = new BinaryWriter(fileStreamFinal))
                {
                    WriteFileHeaders(final, fileStream.Length);

                    using (var a = new DeflateStream(fileStreamFinal, CompressionMode.Compress,
                        CompressionLevel.BestCompression))
                    {
                        WriteCompressedStream(a, fileStream, final, ct, progress);
                    }
                }
            }
            catch (Exception)
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }

        #endregion

        #region Extract
        public static void ExtractL33TZipFile(string inputFileName, string outputFileName)
        {
            try
            {
                using (var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var fileStreamReader = new BinaryReader(fileStream))
                using (var compressedStream = new DeflateStream(fileStreamReader.BaseStream, CompressionMode.Decompress))
                using (var sourceStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var sourceStreamWriter = new BinaryWriter(sourceStream))
                {
                    long fileLength = ReadFileLengthFromCompressedFile(fileStreamReader);
                    ReadCompressedStream(compressedStream, sourceStreamWriter, fileLength);
                }
            }
            catch
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }

        public static byte[] ExtractL33TZipFile(string zipFileName)
        {
            using (var fileStream = File.Open(zipFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var fileStreamReader = new BinaryReader(fileStream))
                return ExtractL33TZipFile(fileStreamReader);
        }

        public static byte[] ExtractL33TZipFile(byte[] zipData)
        {
            using (var fileStream = new MemoryStream(zipData, false))
            using (var fileStreamReader = new BinaryReader(fileStream))
                return ExtractL33TZipFile(fileStreamReader);
        }

        public static byte[] ExtractL33TZipFile(BinaryReader zipReader)
        {
            zipReader.BaseStream.Seek(0, SeekOrigin.Begin);

            long fileLength = ReadFileLengthFromCompressedFile(zipReader);

            using (var compressedStream = new DeflateStream(zipReader.BaseStream, CompressionMode.Decompress))
            using (var outputMemoryStream = new MemoryStream())
            using (var outputWriter = new BinaryWriter(outputMemoryStream))
            {
                ReadCompressedStream(compressedStream, outputWriter, fileLength);

                return outputMemoryStream.ToArray();
            }
        }

        public static async Task DoExtractL33TZipFile(string fileName, string outputFileName, // Not suffixed async
            CancellationToken ct = default,
            IProgress<double> progress = null)
        {
            try
            {
                using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var fileStreamReader = new BinaryReader(fileStream))
                using (var compressedStream = new DeflateStream(fileStreamReader.BaseStream, CompressionMode.Decompress))
                using (var outputStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var outputStreamWriter = new BinaryWriter(outputStream))
                {
                    long length = ReadFileLengthFromCompressedFile(fileStreamReader);
                    ct.ThrowIfCancellationRequested();
                    ReadCompressedStream(compressedStream, outputStreamWriter, length, ct, progress);
                }
            }
            catch (Exception)
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }

        private static void ReadCompressedStream(
            DeflateStream compressedStream,
            BinaryWriter targetStream,
            long fileLength,
            CancellationToken ct = default,
            IProgress<double> progress = null)
        {
            var buffer = new byte[4096];
            int bytesRead;
            var totalBytesRead = 0L;
            while ((bytesRead = compressedStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();

                if (bytesRead > fileLength)
                {
                    totalBytesRead += fileLength;
                    targetStream.Write(buffer, 0, (int)fileLength);
                }
                else if (totalBytesRead + bytesRead <= fileLength)
                {
                    totalBytesRead += bytesRead;
                    targetStream.Write(buffer, 0, bytesRead);
                }
                else if (totalBytesRead + bytesRead > fileLength)
                {
                    var leftToRead = fileLength - totalBytesRead;
                    totalBytesRead += leftToRead;
                    targetStream.Write(buffer, 0, (int)leftToRead);
                }

                progress?.Report((double)totalBytesRead / fileLength * 100);

                if (totalBytesRead >= fileLength)
                    break;
            }
        }

        private static long ReadFileLengthFromCompressedFile(BinaryReader reader)
        {
            var fileHeader = new string(reader.ReadChars(4));
            long length;

            switch (fileHeader.ToLower())
            {
                case L33tHeader:
                    length = reader.ReadInt32();
                    //Skip deflate specification (2 Byte)
                    reader.BaseStream.Position = 10L;
                    break;
                case L66tHeader:
                    length = reader.ReadInt64();
                    //Skip deflate specification (2 Byte)
                    reader.BaseStream.Position = 14L;
                    break;
                default:
                    throw new FileLoadException($"Header '{fileHeader}' is not recognized as a valid type");
            }

            return length;
        }

        #endregion
    }
}