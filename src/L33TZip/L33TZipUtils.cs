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
        #region Check

        public static bool IsL33TZipFile(string fileName)
        {
            bool result;
            using (var fileStream = File.Open(fileName, FileMode.Open))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    try
                    {
                        var head = new string(reader.ReadChars(4));
                        result = head == "l33t" || head == "l66t";
                    }
                    catch (Exception)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        public static bool IsL33TZipFile(byte[] data)
        {
            bool result;
            using (var fileStream = new MemoryStream(data, false))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    try
                    {
                        var head = new string(reader.ReadChars(4));
                        result = head == "l33t" || head == "l66t";
                    }
                    catch (Exception)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        #endregion

        #region Create

        public static void CreateL33TZipFile(string inputFileName, string outputFileName)
        {
            if (!File.Exists(inputFileName))
                throw new FileNotFoundException($"File '{inputFileName}' not found!", inputFileName);

            try
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                var length = new FileInfo(inputFileName).Length;
                using (var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (var fileStreamFinal =
                        File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (var final = new BinaryWriter(fileStreamFinal))
                        {
                            //
                            final.BaseStream.Position = 0L;

                            //Write L33T Header & File Length
                            if (length > int.MaxValue)
                            {
                                char[] l33T = {'l', '6', '6', 't'};
                                final.Write(l33T);
                                final.Write(length);
                            }
                            else
                            {
                                char[] l33T = {'l', '3', '3', 't'};
                                final.Write(l33T);
                                final.Write(Convert.ToInt32(length));
                            }

                            //Write Deflate specification (2 Byte)
                            final.Write(new byte[] {0x78, 0x9C});

                            //
                            using (var a = new DeflateStream(fileStreamFinal, CompressionMode.Compress,
                                CompressionLevel.BestCompression))
                            {
                                var buffer = new byte[4096];
                                var totalread = 0L;
                                int read;
                                while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    //
                                    if (read > length)
                                    {
                                        totalread += length;
                                        a.Write(buffer, 0, Convert.ToInt32(length));
                                    }
                                    else if (totalread + read <= length)
                                    {
                                        totalread += read;
                                        a.Write(buffer, 0, read);
                                    }
                                    else if (totalread + read > length)
                                    {
                                        var leftToRead = length - totalread;
                                        totalread += leftToRead;
                                        final.Write(buffer, 0, Convert.ToInt32(leftToRead));
                                    }

                                    //
                                    if (totalread >= length)
                                        break;
                                }
                            }
                        }
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

        public static byte[] CreateL33TZip(string inputFileName)
        {
            if (!File.Exists(inputFileName))
                throw new FileNotFoundException($"File '{inputFileName}' not found!", inputFileName);

            var length = new FileInfo(inputFileName).Length;
            using (var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var fileStreamFinal = new MemoryStream())
                {
                    using (var final = new BinaryWriter(fileStreamFinal))
                    {
                        //
                        final.BaseStream.Position = 0L;

                        //Write L33T Header & File Length
                        if (length > int.MaxValue)
                        {
                            char[] l33T = {'l', '6', '6', 't'};
                            final.Write(l33T);
                            final.Write(length);
                        }
                        else
                        {
                            char[] l33T = {'l', '3', '3', 't'};
                            final.Write(l33T);
                            final.Write(Convert.ToInt32(length));
                        }

                        //Write Deflate specification (2 Byte)
                        final.Write(new byte[] {0x78, 0x9C});

                        //
                        using (var a = new DeflateStream(fileStreamFinal, CompressionMode.Compress,
                            CompressionLevel.BestCompression))
                        {
                            var buffer = new byte[4096];
                            var totalread = 0L;
                            int read;
                            while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                //
                                if (read > length)
                                {
                                    totalread += length;
                                    a.Write(buffer, 0, Convert.ToInt32(length));
                                }
                                else if (totalread + read <= length)
                                {
                                    totalread += read;
                                    a.Write(buffer, 0, read);
                                }
                                else if (totalread + read > length)
                                {
                                    var leftToRead = length - totalread;
                                    totalread += leftToRead;
                                    final.Write(buffer, 0, Convert.ToInt32(leftToRead));
                                }

                                //
                                if (totalread >= length)
                                    break;
                            }
                        }

                        return fileStreamFinal.ToArray();
                    }
                }
            }
        }

        public static async Task DoCreateL33TZipFile(string inputFileName, string outputFileName,
            CancellationToken ct = default(CancellationToken),
            IProgress<double> progress = null)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(inputFileName))
                    throw new FileNotFoundException($"File '{inputFileName}' not found!", inputFileName);

                try
                {
                    if (File.Exists(outputFileName))
                        File.Delete(outputFileName);

                    var length = new FileInfo(inputFileName).Length;
                    using (var fileStream = File.Open(inputFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        using (var fileStreamFinal =
                            File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            using (var final = new BinaryWriter(fileStreamFinal))
                            {
                                //
                                final.BaseStream.Position = 0L;

                                //Write L33T Header & File Length
                                if (length > int.MaxValue)
                                {
                                    char[] l33T = {'l', '6', '6', 't'};
                                    final.Write(l33T);
                                    final.Write(length);
                                }
                                else
                                {
                                    char[] l33T = {'l', '3', '3', 't'};
                                    final.Write(l33T);
                                    final.Write(Convert.ToInt32(length));
                                }

                                //Write Deflate specification (2 Byte)
                                final.Write(new byte[] {0x78, 0x9C});

                                //
                                using (var a = new DeflateStream(fileStreamFinal, CompressionMode.Compress,
                                    CompressionLevel.BestCompression))
                                {
                                    var buffer = new byte[4096];
                                    var totalread = 0L;
                                    int read;
                                    while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        ct.ThrowIfCancellationRequested();

                                        //
                                        if (read > length)
                                        {
                                            totalread += length;
                                            a.Write(buffer, 0, Convert.ToInt32(length));
                                        }
                                        else if (totalread + read <= length)
                                        {
                                            totalread += read;
                                            a.Write(buffer, 0, read);
                                        }
                                        else if (totalread + read > length)
                                        {
                                            var leftToRead = length - totalread;
                                            totalread += leftToRead;
                                            final.Write(buffer, 0, Convert.ToInt32(leftToRead));
                                        }

                                        progress?.Report((double) totalread / length);

                                        //
                                        if (totalread >= length)
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    if (File.Exists(outputFileName))
                        File.Delete(outputFileName);

                    throw;
                }
            }, ct);
        }

        #endregion

        #region  Extract

        public static void ExtractL33TZipFile(string fileName, string outputFileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"File '{fileName}' not found!", fileName);

            try
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new BinaryReader(fileStream))
                    {
                        //Header && Length
                        var head = new string(reader.ReadChars(4));
                        long length;
                        switch (head.ToLower())
                        {
                            case "l33t":
                                length = reader.ReadInt32();
                                //Skip deflate specification (2 Byte)
                                reader.BaseStream.Position = 10L;
                                break;
                            case "l66t":
                                length = reader.ReadInt64();
                                //Skip deflate specification (2 Byte)
                                reader.BaseStream.Position = 14L;
                                break;
                            default:
                                throw new FileLoadException($"'l33t' header not found, file: '{fileName}'");
                        }

                        //
                        using (var a = new DeflateStream(reader.BaseStream, CompressionMode.Decompress))
                        {
                            using (var fileStreamFinal =
                                File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                using (var final = new BinaryWriter(fileStreamFinal))
                                {
                                    var buffer = new byte[4096];
                                    int read;
                                    var totalread = 0L;
                                    while ((read = a.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        //
                                        if (read > length)
                                        {
                                            totalread += length;
                                            final.Write(buffer, 0, (int) length);
                                        }
                                        else if (totalread + read <= length)
                                        {
                                            totalread += read;
                                            final.Write(buffer, 0, read);
                                        }
                                        else if (totalread + read > length)
                                        {
                                            var leftToRead = length - totalread;
                                            totalread += leftToRead;
                                            final.Write(buffer, 0, (int) leftToRead);
                                        }

                                        //
                                        if (totalread >= length)
                                            break;
                                    }
                                }
                            }
                        }
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

        public static byte[] ExtractL33TZipFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"File '{fileName}' not found!", fileName);

            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    return ExtractL33TZipFile(reader);
                }
            }
        }

        public static byte[] ExtractL33TZipFile(byte[] data)
        {
            using (var fileStream = new MemoryStream(data, false))
            {
                using (var reader = new BinaryReader(fileStream))
                {
                    return ExtractL33TZipFile(reader);
                }
            }
        }

        public static byte[] ExtractL33TZipFile(BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            //Header && Length
            var head = new string(reader.ReadChars(4));
            long length;
            switch (head.ToLower())
            {
                case "l33t":
                    length = reader.ReadInt32();
                    //Skip deflate specification (2 Byte)
                    reader.BaseStream.Position = 10L;
                    break;
                case "l66t":
                    length = reader.ReadInt64();
                    //Skip deflate specification (2 Byte)
                    reader.BaseStream.Position = 14L;
                    break;
                default:
                    throw new FileLoadException("'l33t' header not found");
            }

            //
            using (var a = new DeflateStream(reader.BaseStream, CompressionMode.Decompress))
            {
                using (var fileStreamFinal = new MemoryStream())
                {
                    using (var final = new BinaryWriter(fileStreamFinal))
                    {
                        var buffer = new byte[4096];
                        int read;
                        var totalread = 0L;
                        while ((read = a.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            //
                            if (read > length)
                            {
                                totalread += length;
                                final.Write(buffer, 0, (int) length);
                            }
                            else if (totalread + read <= length)
                            {
                                totalread += read;
                                final.Write(buffer, 0, read);
                            }
                            else if (totalread + read > length)
                            {
                                var leftToRead = length - totalread;
                                totalread += leftToRead;
                                final.Write(buffer, 0, (int) leftToRead);
                            }

                            //
                            if (totalread >= length)
                                break;
                        }
                        return fileStreamFinal.ToArray();
                    }
                }
            }
        }

        public static async Task DoExtractL33TZipFile(string fileName, string outputFileName,
            CancellationToken ct = default(CancellationToken),
            IProgress<double> progress = null)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(fileName))
                    throw new FileNotFoundException($"File '{fileName}' not found!", fileName);

                try
                {
                    if (File.Exists(outputFileName))
                        File.Delete(outputFileName);

                    using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var reader = new BinaryReader(fileStream))
                        {
                            //Header && Length
                            var head = new string(reader.ReadChars(4));
                            long length;
                            switch (head.ToLower())
                            {
                                case "l33t":
                                    length = reader.ReadInt32();
                                    //Skip deflate specification (2 Byte)
                                    reader.BaseStream.Position = 10L;
                                    break;
                                case "l66t":
                                    length = reader.ReadInt64();
                                    //Skip deflate specification (2 Byte)
                                    reader.BaseStream.Position = 14L;
                                    break;
                                default:
                                    throw new FileLoadException($"'l33t' header not found, file: '{fileName}'");
                            }

                            ct.ThrowIfCancellationRequested();
                            //
                            using (var a = new DeflateStream(reader.BaseStream, CompressionMode.Decompress))
                            {
                                using (var fileStreamFinal =
                                    File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    using (var final = new BinaryWriter(fileStreamFinal))
                                    {
                                        var buffer = new byte[4096];
                                        int read;
                                        var totalread = 0L;
                                        while ((read = a.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            ct.ThrowIfCancellationRequested();

                                            //
                                            if (read > length)
                                            {
                                                totalread += length;
                                                final.Write(buffer, 0, (int) length);
                                            }
                                            else if (totalread + read <= length)
                                            {
                                                totalread += read;
                                                final.Write(buffer, 0, read);
                                            }
                                            else if (totalread + read > length)
                                            {
                                                var leftToRead = length - totalread;
                                                totalread += leftToRead;
                                                final.Write(buffer, 0, (int) leftToRead);
                                            }

                                            progress?.Report((double) totalread / length);

                                            //
                                            if (totalread >= length)
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    if (File.Exists(outputFileName))
                        File.Delete(outputFileName);

                    throw;
                }
            }, ct);
        }

        #endregion
    }
}