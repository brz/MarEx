using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MarEx
{
    /// <summary>
    /// Tool to decompress, compress and list TOC for MAR files (MSN Archive).
    /// MAR files are a proprietary format, used in MSN Explorer.
    /// Based on the information found here: https://www.neowin.net/forum/topic/35549-skinning-msn-explorer
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Insufficient arguments supplied");
                return;
            }

            MarExAction? action;
            switch (args[0])
            {
                case "d": //Decompress
                    action = MarExAction.Decompress;
                    break;
                case "c": //Compress
                    action = MarExAction.Compress;
                    break;
                case "i": //Info
                    action = MarExAction.Info;
                    break;
                default:
                    Console.WriteLine("First argument must indicate one of the following actions: d (decompress), c (compress) or i (info)");
                    return;
            }

            if(action == MarExAction.Decompress || action == MarExAction.Compress)
            {
                if(args.Length != 5 || !args.Contains("-i") || !args.Contains("-o"))
                {
                    Console.WriteLine("Insufficient arguments supplied");
                    return;
                }

                string input = null;
                string output = null;

                for (var i = 1; i < args.Length; i += 2)
                {
                    if (args[i] == "-i")
                    {
                        input = args[i + 1];
                    }
                    else if (args[i] == "-o")
                    {
                        output = args[i + 1];
                    }
                }

                if(action == MarExAction.Decompress)
                {
                    if (!File.Exists(input))
                    {
                        Console.WriteLine("Input file not found");
                        return;
                    }

                    Console.WriteLine($"Decompression of file {input} started");
                    Decompress(input, output);
                    Console.WriteLine($"File decompressed to output folder {output}");
                    return;
                }
                else if(action == MarExAction.Compress)
                {
                    if (!Directory.Exists(input))
                    {
                        Console.WriteLine("Input folder not found");
                        return;
                    }

                    Console.WriteLine($"Compression of folder {input} started");
                    Compress(input, output);
                    Console.WriteLine($"Folder compressed to output file {output}");
                    return;
                }
            }
            else if(action == MarExAction.Info)
            {
                if(args.Length != 2)
                {
                    Console.WriteLine("Insufficient arguments supplied");
                    return;
                }

                if (!File.Exists(args[1]))
                {
                    Console.WriteLine("Input file not found");
                    return;
                }

                var bytes = File.ReadAllBytes(args[1]);
                var contentInfos = GetContentInfos(bytes);

                var tablePrinter = new TablePrinter("FileName", "Size", "CRC32", "StartPos");
                foreach(var contentInfo in contentInfos)
                {
                    tablePrinter.AddRow(contentInfo.FileName, contentInfo.Size, contentInfo.Crc32, contentInfo.StartPosition);
                }
                tablePrinter.Print();
                Console.WriteLine($"Total number of files: {contentInfos.Count}");
            }
        }

        private static List<ContentInfo> GetContentInfos(byte[] bytes)
        {
            //Skip header (first 8 bytes)
            var currentPos = 8;

            //Determine number of files
            var numberOfFiles = BitConverter.ToUInt32(bytes.Skip(currentPos).Take(4).ToArray(), 0);
            currentPos += 4;

            //Create ContentInfo List
            var contentInfos = new List<ContentInfo>();

            for (var i = 0; i < numberOfFiles; i++)
            {
                var internalCurrentPos = 0;

                //Each file description is 68 bytes in size total
                var fileDescriptionBytes = bytes.Skip(currentPos).Take(68).ToArray();

                //Filename is 56 bytes block, padded with 0's
                var fileName = Encoding.Default.GetString(fileDescriptionBytes, internalCurrentPos, 56).TrimEnd('\0');
                internalCurrentPos += 56;

                //Filesize is 4 bytes
                var fileSize = BitConverter.ToUInt32(fileDescriptionBytes.Skip(internalCurrentPos).Take(4).ToArray(), 0);
                internalCurrentPos += 4;

                //CRC32 is 4 bytes
                var crc32 = BitConverter.ToUInt32(fileDescriptionBytes.Skip(internalCurrentPos).Take(4).ToArray(), 0);
                internalCurrentPos += 4;

                //Start position is 4 bytes
                var startPosition = BitConverter.ToUInt32(fileDescriptionBytes.Skip(internalCurrentPos).Take(4).ToArray(), 0);

                //ContentInfo object
                var contentInfo = new ContentInfo
                {
                    FileName = fileName,
                    Size = fileSize,
                    Crc32 = crc32,
                    StartPosition = startPosition
                };
                contentInfos.Add(contentInfo);

                currentPos += 68;
            }

            return contentInfos;
        }

        private static void Decompress(string inputFile, string outputFolder)
        {
            var bytes = File.ReadAllBytes(inputFile);

            var contentInfos = GetContentInfos(bytes);

            foreach (var item in contentInfos)
            {
                var contentBytes = bytes.Skip(Convert.ToInt32(item.StartPosition)).Take(Convert.ToInt32(item.Size)).ToArray();
                var outputFilePath = Path.Combine(outputFolder, item.FileName);
                var nestedOutputFolder = Path.GetDirectoryName(outputFilePath);
                if (!Directory.Exists(nestedOutputFolder))
                {
                    Directory.CreateDirectory(nestedOutputFolder);
                }
                File.WriteAllBytes(outputFilePath, contentBytes);
            }
        }

        private static void Compress(string inputFolder, string outputFile)
        {
            var fileNames = Directory.GetFiles(inputFolder, "*.*", SearchOption.AllDirectories);

            var orderCounter = 0;
            var fileNamesOrderedToc = fileNames.OrderBy(f => f.Replace("_", "a"), StringComparer.OrdinalIgnoreCase).Select(f => new { PathAndFileName = f, Order = orderCounter++}).ToList(); //Order for TOC
            orderCounter = 0;
            var fileNamesOrderedFiles = fileNames.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).Select(f => new { PathAndFileName = f, Order = orderCounter++ }).ToList(); //Order for files

            var repackInfos = new List<RepackInfo>();
            for (var i = 0; i < fileNames.Length; i++)
            {
                var pathAndFileName = fileNamesOrderedFiles.OrderBy(f => f.Order).Select(f => f.PathAndFileName).Skip(i).First();
                var outputFileName = pathAndFileName.Substring(inputFolder.EndsWith(Path.DirectorySeparatorChar.ToString()) ? inputFolder.Length : inputFolder.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                var fileAsByteArray = File.ReadAllBytes(pathAndFileName);
                var size = Convert.ToUInt32(new FileInfo(pathAndFileName).Length);
                var crc32 = Helpers.CalculateCrc32(fileAsByteArray);

                int startPosition;
                if (i == 0)
                {
                    startPosition = 8 + 4 + 68 * fileNames.Length;
                }
                else
                {
                    startPosition = (int)repackInfos.Last().StartPosition + (int)repackInfos.Last().Size;
                }

                repackInfos.Add(new RepackInfo { PathAndFileName = pathAndFileName, FileName = outputFileName, Crc32 = crc32, Size = size, StartPosition = (uint)startPosition, File = fileAsByteArray });
            }

            var totalOutputSize = 8 + 4 + 68 * repackInfos.Count + repackInfos.Sum(f => f.Size);

            //Build output file
            var outputByteArray = new byte[totalOutputSize];

            //Set header (first 8 bytes)
            outputByteArray[0] = 0x4D;
            outputByteArray[1] = 0x41;
            outputByteArray[2] = 0x52;
            outputByteArray[3] = 0x43;
            outputByteArray[4] = 0x03;
            outputByteArray[5] = 0x00;
            outputByteArray[6] = 0x00;
            outputByteArray[7] = 0x00;

            //Set number of files (4 bytes)
            var numberOfFilesAsByteArray = Helpers.GetZeroPaddedByteArray(BitConverter.GetBytes(repackInfos.Count), 4);
            Array.Copy(numberOfFilesAsByteArray, 0, outputByteArray, 8, 4);

            //Append file descriptions and file contents
            for (var i = 0; i < repackInfos.Count; i++)
            {
                var rdr = fileNamesOrderedToc.OrderBy(f => f.Order).Select(f => f.PathAndFileName).Skip(i).First();
                var repackInfo = repackInfos.Where(ri => ri.PathAndFileName == rdr).First();
                //var repackInfo = repackInfos[i];

                //File description for each file is 68 bytes
                var fileDescriptionBytes = new byte[68];

                Array.Copy(Helpers.GetZeroPaddedByteArray(Encoding.Default.GetBytes(repackInfo.FileName), 56), 0, fileDescriptionBytes, 0, 56); //Filename (56 bytes)
                Array.Copy(BitConverter.GetBytes(repackInfo.Size), 0, fileDescriptionBytes, 56, 4); //Filesize (4 bytes)
                Array.Copy(BitConverter.GetBytes(repackInfo.Crc32), 0, fileDescriptionBytes, 60, 4); //CRC32 (4 bytes)
                Array.Copy(BitConverter.GetBytes(repackInfo.StartPosition), 0, fileDescriptionBytes, 64, 4); //Start position (4 bytes)

                //Write the file description of the current file to the output byte array
                Array.Copy(fileDescriptionBytes, 0, outputByteArray, 8 + 4 + i * 68, 68);

                //Write the contents of the current file to the output byte array
                Array.Copy(repackInfo.File, 0, outputByteArray, repackInfo.StartPosition, repackInfo.Size);
            }

            //Write byte array to output file
            var outputFolder = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            File.WriteAllBytes(outputFile, outputByteArray);
        }
    }

    public enum MarExAction
    {
        Decompress = 1,
        Compress = 2,
        Info = 3
    }
}
