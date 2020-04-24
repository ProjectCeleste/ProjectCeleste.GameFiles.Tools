#region Using directives

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Overby.Extensions.AsyncBinaryReaderWriter;

#endregion

namespace ProjectCeleste.GameFiles.Tools.Utils
{
    public static class L33TZipUtils
    {
        private const string L33THeader = "l33t";
        private const string L66THeader = "l66t";
        private const int BufferSize = 32*1024;

        #region CheckAsync

        public static async Task<bool> IsL33TZipAsync(string fileName, CancellationToken ct = default)
        {
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await IsL33TZipAsync(fileStream, ct);
        }

        public static async Task<bool> IsL33TZipAsync(byte[] data, CancellationToken ct = default)
        {
            using var memoryStream = new MemoryStream(data, false);
            return await IsL33TZipAsync(memoryStream, ct);
        }

        public static async Task<bool> IsL33TZipAsync(Stream stream, CancellationToken ct = default)
        {
            var oldPosition = stream.Position;
            try
            {
                using var reader = new AsyncBinaryReader(stream);
                var fileHeader = new string(await reader.ReadCharsAsync(4, ct));
                switch (fileHeader.ToLower())
                {
                    case L33THeader:
                    case L66THeader:
                        break;
                    default:
                        throw new InvalidOperationException($"Header '{fileHeader}' is not recognized as a valid type");
                }

                return true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
            {
                return false;
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                stream.Position = oldPosition;
            }
        }

        #endregion

        #region Check

        public static bool IsL33TZip(string fileName)
        {
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            return IsL33TZip(fileStream);
        }

        public static bool IsL33TZip(byte[] data)
        {
            using var memoryStream = new MemoryStream(data, false);
            return IsL33TZip(memoryStream);
        }

        public static bool IsL33TZip(Stream stream)
        {
                using var reader = new BinaryReader(stream);
                return IsL33TZip(reader);
        }

        public static bool IsL33TZip(BinaryReader reader)
        {
            var oldPosition = reader.BaseStream.Position;
            try
            {
                var fileHeader = new string(reader.ReadChars(4));
                switch (fileHeader.ToLower())
                {
                    case L33THeader:
                    case L66THeader:
                        break;
                    default:
                        throw new InvalidOperationException($"Header '{fileHeader}' is not recognized as a valid type");
                }

                return true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
            {
                return false;
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                reader.BaseStream.Position = oldPosition;
            }
        }
        #endregion

        #region Compressing

        public static async Task<byte[]> CompressFileAsL33TZipAsync(string inputFileName,
            IProgress<double> progress = null, CancellationToken ct = default)
        {
            using var sourceFileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None);
            using var outputStream = new MemoryStream();
            await CompressFileAsL33TZipAsync(sourceFileStream, outputStream, progress, ct);
            return outputStream.ToArray();
        }

        public static async Task CompressFileAsL33TZipAsync(string inputFileName, string outputFileName,
            IProgress<double> progress = null,
            CancellationToken ct = default)
        {
            try
            {
                using var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None);
                using var fileStreamFinal = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                using var outputFileStreamWriter = new BinaryWriter(fileStreamFinal);
                await CompressFileAsL33TZipAsync(fileStream, fileStreamFinal, progress, ct);
            }
            catch (Exception)
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }

        private static async Task CompressFileAsL33TZipAsync(
            Stream inputStream,
            Stream outputStream,
            IProgress<double> progress = null,
            CancellationToken ct = default)
        {
            //
            var fileLength = inputStream.Length;

            //Write L33T Header
            using (var writer = new AsyncBinaryWriter(inputStream))
            {
                //Write L33T Header & File Length
                if (fileLength > int.MaxValue)
                {
                    await writer.WriteAsync(L66THeader.ToCharArray(), ct);
                    await writer.WriteAsync(fileLength, ct);
                }
                else
                {
                    await writer.WriteAsync(L33THeader.ToCharArray(), ct);
                    await writer.WriteAsync(Convert.ToInt32(fileLength), ct);
                }

                //Write Deflate specification (2 Byte)
                await writer.WriteAsync(new byte[] {0x78, 0x9C}, ct);
            }

            //Write Content
            var buffer = new byte[BufferSize];
            var totalBytesRead = 0L;
            var lastProgress = 0d;
            int bytesRead;
            using var compressedStream = new DeflateStream(outputStream, CompressionLevel.Optimal);
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                ct.ThrowIfCancellationRequested();

                if (bytesRead > fileLength)
                {
                    totalBytesRead += fileLength;
                    await compressedStream.WriteAsync(buffer, 0, Convert.ToInt32(fileLength), ct);
                }
                else if (totalBytesRead + bytesRead <= fileLength)
                {
                    totalBytesRead += bytesRead;
                    await compressedStream.WriteAsync(buffer, 0, bytesRead, ct);
                }
                else if (totalBytesRead + bytesRead > fileLength)
                {
                    var leftToRead = fileLength - totalBytesRead;
                    totalBytesRead += leftToRead;
                    await compressedStream.WriteAsync(buffer, 0, Convert.ToInt32(leftToRead), ct);
                }

                var newProgress = (double) totalBytesRead / fileLength * 100;

                if (newProgress - lastProgress > 1)
                {
                    progress?.Report(newProgress);
                    lastProgress = newProgress;
                }

                if (totalBytesRead >= fileLength)
                    break;
            }
        }

        #endregion

        #region DecompressAsync

        public static async Task<byte[]> DecompressL33TZipAsync(string zipFileName,
            IProgress<double> progress = null,
            CancellationToken ct = default)
        {
            using var outStream = new MemoryStream();
            using (var fileStream = File.Open(zipFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await DecompressL33TZipAsync(fileStream, outStream, progress, ct);
            }

            return outStream.ToArray();
        }

        public static async Task DecompressL33TZipAsync(string fileName, string outputFileName,
            IProgress<double> progress = null,
            CancellationToken ct = default)
        {
            try
            {
                using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var outputStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                await DecompressL33TZipAsync(fileStream, outputStream, progress, ct);
            }
            catch (Exception)
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }

        public static async Task<byte[]> DecompressL33TZipAsync(byte[] zipData,
            IProgress<double> progress = null,
            CancellationToken ct = default)
        {
            using var outStream = new MemoryStream();
            using (var fileStream = new MemoryStream(zipData, false))
            {
                await DecompressL33TZipAsync(fileStream, outStream, progress, ct);
            }

            return outStream.ToArray();
        }

        public static async Task DecompressL33TZipAsync(
            Stream inputStream,
            Stream outputStream,
            IProgress<double> progress = null,
            CancellationToken ct = default)
        {
            //Get extracted content length
            long fileLength;
            using (var reader = new AsyncBinaryReader(inputStream))
            {
                var fileHeader = new string(await reader.ReadCharsAsync(4, ct));
                switch (fileHeader.ToLower())
                {
                    case L33THeader:
                        fileLength = await reader.ReadInt32Async(ct);
                        //Skip deflate specification (2 Byte)
                        reader.BaseStream.Position += 2;
                        break;
                    case L66THeader:
                        fileLength = await reader.ReadInt64Async(ct);
                        //Skip deflate specification (2 Byte)
                        reader.BaseStream.Position += 2;
                        break;
                    default:
                        throw new InvalidOperationException($"Header '{fileHeader}' is not recognized as a valid type");
                }
            }

            //Extract content
            var buffer = new byte[BufferSize];
            int bytesRead;
            var totalBytesRead = 0L;
            var lastProgress = 0d;
            using var compressedStream = new DeflateStream(inputStream, CompressionMode.Decompress);
            while ((bytesRead = await compressedStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                ct.ThrowIfCancellationRequested();

                if (bytesRead > fileLength)
                {
                    totalBytesRead += fileLength;
                    await outputStream.WriteAsync(buffer, 0, (int) fileLength, ct);
                }
                else if (totalBytesRead + bytesRead <= fileLength)
                {
                    totalBytesRead += bytesRead;
                    await outputStream.WriteAsync(buffer, 0, bytesRead, ct);
                }
                else if (totalBytesRead + bytesRead > fileLength)
                {
                    var leftToRead = fileLength - totalBytesRead;
                    totalBytesRead += leftToRead;
                    await outputStream.WriteAsync(buffer, 0, (int) leftToRead, ct);
                }

                var newProgress = (double) totalBytesRead / fileLength * 100;

                if (newProgress - lastProgress > 1)
                {
                    progress?.Report(newProgress);
                    lastProgress = newProgress;
                }

                if (totalBytesRead >= fileLength)
                    break;
            }
        }

        #endregion

        #region Decompress

        public static byte[] DecompressL33TZip(string zipFileName)
        {
            using var outStream = new MemoryStream();
            using (var fileStream = File.Open(zipFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DecompressL33TZip(fileStream, outStream);
            }

            return outStream.ToArray();
        }

        public static void DecompressL33TZip(string fileName, string outputFileName)
        {
            try
            {
                using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var outputStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                DecompressL33TZip(fileStream, outputStream);
            }
            catch (Exception)
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }

        public static byte[] DecompressL33TZip(byte[] zipData)
        {
            using var outStream = new MemoryStream();
            using (var fileStream = new MemoryStream(zipData, false))
            {
                DecompressL33TZip(fileStream, outStream);
            }

            return outStream.ToArray();
        }

        public static void DecompressL33TZip(Stream inputStream, Stream outputStream)
        {
            //Get extracted content length
            long fileLength;
            using (var reader = new BinaryReader(inputStream))
            {
                var fileHeader = new string(reader.ReadChars(4));
                switch (fileHeader.ToLower())
                {
                    case L33THeader:
                        fileLength = reader.ReadInt32();
                        //Skip deflate specification (2 Byte)
                        reader.BaseStream.Position += 2;
                        break;
                    case L66THeader:
                        fileLength = reader.ReadInt64();
                        //Skip deflate specification (2 Byte)
                        reader.BaseStream.Position += 2;
                        break;
                    default:
                        throw new InvalidOperationException($"Header '{fileHeader}' is not recognized as a valid type");
                }
            }

            //Extract content
            var buffer = new byte[BufferSize];
            int bytesRead;
            var totalBytesRead = 0L;
            using var compressedStream = new DeflateStream(inputStream, CompressionMode.Decompress);
            while ((bytesRead = compressedStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (bytesRead > fileLength)
                {
                    totalBytesRead += fileLength;
                    outputStream.Write(buffer, 0, (int) fileLength);
                }
                else if (totalBytesRead + bytesRead <= fileLength)
                {
                    totalBytesRead += bytesRead;
                    outputStream.Write(buffer, 0, bytesRead);
                }
                else if (totalBytesRead + bytesRead > fileLength)
                {
                    var leftToRead = fileLength - totalBytesRead;
                    totalBytesRead += leftToRead;
                    outputStream.Write(buffer, 0, (int) leftToRead);
                }

                if (totalBytesRead >= fileLength)
                    break;
            }
        }

        #endregion
    }
}