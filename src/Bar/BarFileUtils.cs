#region Using directives

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Celeste.GameFiles.Tools.Extensions;
using ProjectCeleste.GameFiles.Tools.L33TZip;
using ProjectCeleste.GameFiles.Tools.Xmb;

#endregion

namespace ProjectCeleste.GameFiles.Tools.Bar
{
    public static class BarFileUtils
    {
        private static readonly ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;

        #region Extracting
        public static async Task ExtractBarFilesAsync(string inputFile, string outputPath, bool convertFile = true)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException($"File '{inputFile}' not found!", inputFile);

            if (!outputPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                outputPath += Path.DirectorySeparatorChar;

            using var barFileStream = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            var barFilesInfo = ReadBarFileInfo(barFileStream);

            foreach (var barFileInfo in barFilesInfo.BarFileEntrys)
                await ExtractBarFileContents(barFileInfo, barFilesInfo.RootPath, barFileStream, outputPath, convertFile);
        }

        public static async Task ExtractBarFile(string inputFile, string file, string outputPath, bool convertFile = true)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentNullException(nameof(file), "Value cannot be null or empty.");

            if (!File.Exists(inputFile))
                throw new FileNotFoundException($"File '{inputFile}' not found!", inputFile);

            if (!outputPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                outputPath += Path.DirectorySeparatorChar;

            using var fileStream = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            var barFilesInfo = ReadBarFileInfo(fileStream);

            var barFileInfo = barFilesInfo.BarFileEntrys.First(
                key => string.Equals(key.FileName, file, StringComparison.OrdinalIgnoreCase));

            fileStream.Seek(barFileInfo.Offset, SeekOrigin.Begin); //Seek to file

            var path = Path.Combine(outputPath, barFilesInfo.RootPath,
                Path.GetDirectoryName(barFileInfo.FileName) ?? string.Empty);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filePath = Path.Combine(outputPath, barFilesInfo.RootPath, barFileInfo.FileName);

            var tempFileName = await CreateTempFileForBarFile(fileStream, barFileInfo);

            await ConvertFile(convertFile, tempFileName, barFileInfo, filePath);
        }
        
        private static BarFile ReadBarFileInfo(FileStream fileStream)
        {
            using var binReader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, true);

            //Read Header
            binReader.BaseStream.Seek(0, SeekOrigin.Begin); //Seek to header
            var barFileHeader = new BarFileHeader(binReader);

            //Read Files Info
            binReader.BaseStream.Seek(barFileHeader.FilesTableOffset, SeekOrigin.Begin); //Seek to file table

            return new BarFile(binReader);
        }

        private static async Task<string> CreateTempFileForBarFile(FileStream fileStream, BarEntry barFileInfo)
        {
            var tempFileName = Path.GetTempFileName();
            using (var fileStreamFinal = File.Open(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = bufferPool.Rent(81920);
                try
                {
                    int read;
                    var totalread = 0L;
                    while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        if (read > barFileInfo.FileSize)
                        {
                            totalread = barFileInfo.FileSize;
                            await fileStreamFinal.WriteAsync(buffer, 0, barFileInfo.FileSize);
                        }
                        else if (totalread + read <= barFileInfo.FileSize)
                        {
                            totalread += read;
                            await fileStreamFinal.WriteAsync(buffer, 0, read);
                        }
                        else if (totalread + read > barFileInfo.FileSize)
                        {
                            var leftToRead = barFileInfo.FileSize - totalread;
                            totalread = barFileInfo.FileSize;
                            await fileStreamFinal.WriteAsync(buffer, 0, Convert.ToInt32(leftToRead));
                        }

                        if (totalread >= barFileInfo.FileSize)
                            break;
                    }
                }
                finally
                {
                    bufferPool.Return(buffer);
                }
            }

            return tempFileName;
        }

        private static async Task ExtractBarFileContents(BarEntry barFileInfo, string rootPath, FileStream fileStream, string outputPath, bool convertFile)
        {
            fileStream.Seek(barFileInfo.Offset, SeekOrigin.Begin); //Seek to file

            var path = Path.Combine(outputPath, rootPath,
                Path.GetDirectoryName(barFileInfo.FileName) ?? string.Empty);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filePath = Path.Combine(outputPath, rootPath, barFileInfo.FileName);

            //Extract to tmp file
            var tempFileName = await CreateTempFileForBarFile(fileStream, barFileInfo);

            await ConvertFile(convertFile, tempFileName, barFileInfo, filePath);
        }

        private static async Task ConvertFile(bool convertFile, string tempFileName, BarEntry barFileInfo, string filePath)
        {
            //Convert file
            if (convertFile)
            {
                if (L33TZipUtils.IsL33TZipFile(tempFileName) &&
                    !barFileInfo.FileName.EndsWith(".age4scn", StringComparison.OrdinalIgnoreCase))
                {
                    var rnd = new Random(Guid.NewGuid().GetHashCode());
                    var tempFileName2 =
                        Path.Combine(Path.GetTempPath(),
                            $"{Path.GetFileName(barFileInfo.FileName)}-{rnd.Next()}.tmp");
                    await L33TZipUtils.ExtractL33TZipFileAsync(tempFileName, tempFileName2);

                    if (File.Exists(tempFileName))
                        File.Delete(tempFileName);

                    tempFileName = tempFileName2;
                }

                if (barFileInfo.FileName.EndsWith(".xmb", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var rnd = new Random(Guid.NewGuid().GetHashCode());
                        var tempFileName2 =
                            Path.Combine(Path.GetTempPath(),
                                $"{Path.GetFileName(barFileInfo.FileName)}-{rnd.Next()}.tmp");
                        XmbFileUtils.XmbToXml(tempFileName, tempFileName2);

                        if (File.Exists(tempFileName))
                            File.Delete(tempFileName);

                        tempFileName = tempFileName2;

                        filePath = filePath.Substring(0, filePath.Length - 4);
                    }
                    catch (Exception)
                    {
                        //
                    }
                }
                else if (barFileInfo.FileName.EndsWith(".age4scn",
                                StringComparison.OrdinalIgnoreCase) &&
                            !L33TZipUtils.IsL33TZipFile(tempFileName))
                {
                    var rnd = new Random(Guid.NewGuid().GetHashCode());
                    var tempFileName2 =
                        Path.Combine(Path.GetTempPath(),
                            $"{Path.GetFileName(barFileInfo.FileName)}-{rnd.Next()}.tmp");
                    await L33TZipUtils.CompressFileAsL33TZipAsync(tempFileName, tempFileName2);

                    if (File.Exists(tempFileName))
                        File.Delete(tempFileName);

                    tempFileName = tempFileName2;
                }
            }

            //Move new file
            if (File.Exists(filePath))
                File.Delete(filePath);

            File.Move(tempFileName, filePath);

            File.SetCreationTimeUtc(filePath,
                new DateTime(barFileInfo.LastWriteTime.Year, barFileInfo.LastWriteTime.Month,
                    barFileInfo.LastWriteTime.Day, barFileInfo.LastWriteTime.Hour,
                    barFileInfo.LastWriteTime.Minute, barFileInfo.LastWriteTime.Second));

            File.SetLastWriteTimeUtc(filePath,
                new DateTime(barFileInfo.LastWriteTime.Year, barFileInfo.LastWriteTime.Month,
                    barFileInfo.LastWriteTime.Day, barFileInfo.LastWriteTime.Hour,
                    barFileInfo.LastWriteTime.Minute, barFileInfo.LastWriteTime.Second));
        }

        private static IEnumerable<string> GetAllFilesUnderDirectory(string directoryPath)
        {
            var filesInDirectory = Directory.EnumerateFiles(directoryPath);

            foreach (var subdirectoryPath in Directory.EnumerateDirectories(directoryPath))
                filesInDirectory = filesInDirectory.Concat(GetAllFilesUnderDirectory(subdirectoryPath));

            return filesInDirectory;
        }
        #endregion

        #region Reading

        public static async Task CreateBarFileAsync(IEnumerable<FileInfo> fileInfos, string inputPath,
            string outputFileName, string rootdir, bool ignoreLastWriteTime = true)
        {
            if (inputPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                inputPath = inputPath.Substring(0, inputPath.Length - 1);

            var outputFolder = Path.GetDirectoryName(outputFileName);
            if (outputFolder != null && !Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var uncompressedBarFiles = await Task.WhenAll(fileInfos.Select(t => EnsureFileIsExtracted(t)));

            using var barFileStream = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);

            //Write Bar Header
            var header = new BarFileHeader(Path.GetFileName(outputFileName), uncompressedBarFiles);
            var headerBytes = header.ToByteArray();
            await barFileStream.WriteAsync(headerBytes, 0, headerBytes.Length);

            //Write Files
            var barEntrys = new List<BarEntry>();
            foreach (var barEntry in uncompressedBarFiles)
            {
                barEntrys.Add(new BarEntry(inputPath, barEntry, (int)barFileStream.Position, ignoreLastWriteTime));

                using var uncompressedEntryFileStream = File.Open(barEntry.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                await uncompressedEntryFileStream.BufferedCopyToAsync(barFileStream);
            }

            //Write Bar Entrys
            var end = new BarFile(rootdir, barEntrys);
            using var bw = new BinaryWriter(barFileStream);

            end.WriteToBinaryWriter(bw);
        }

        public static async Task CreateBarFileAsync(string inputPath, string outputFileName, string rootdir,
            bool ignoreLastWriteTime = true)
        {
            var fileInfos = GetAllFilesUnderDirectory(inputPath)
                .Select(fileName => new FileInfo(fileName));

            await CreateBarFileAsync(fileInfos, inputPath, outputFileName, rootdir, ignoreLastWriteTime);
        }

        public static async Task ConvertToNullBarFile(string inputFile, string outputPath, string fileName,
            bool ignoreLastWriteTime = true)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException($"File '{inputFile}' not found!", inputFile);

            string rootName;
            using (var fileStream = File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using var binReader = new BinaryReader(fileStream);

                //Read Header
                binReader.BaseStream.Seek(0, SeekOrigin.Begin); //Seek to header
                var barFileHeader = new BarFileHeader(binReader);

                //Read Files Info
                binReader.BaseStream.Seek(barFileHeader.FilesTableOffset, SeekOrigin.Begin); //Seek to file table
                var barFilesInfo = new BarFile(binReader);

                rootName = barFilesInfo.RootPath;
            }

            var path = Path.GetRandomFileName();
            var inputPath = Path.Combine(Path.GetTempPath(), path, rootName);
            if (!Directory.Exists(inputPath))
                Directory.CreateDirectory(inputPath);

            inputPath = inputPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? inputPath.Substring(0, inputPath.Length - 1)
                : inputPath;

            var nullFile = Path.Combine(inputPath, "null");
            File.WriteAllText(nullFile, string.Empty);

            var outputFile = Path.Combine(outputPath, rootName, fileName);
            await CreateBarFileAsync(inputPath, outputFile, rootName, ignoreLastWriteTime);
        }

        private static async Task<FileInfo> EnsureFileIsExtracted(FileInfo file)
        {
            if (file.FullName.EndsWith(".age4scn", StringComparison.OrdinalIgnoreCase) &&
                 L33TZipUtils.IsL33TZipFile(file.FullName))
            {
                var data = await L33TZipUtils.ExtractL33TZipFileAsync(file.FullName);
                File.Delete(file.FullName);
                File.WriteAllBytes(file.FullName, data);
                return new FileInfo(file.FullName);
            }

            return file;
        }

        #endregion
    }
}