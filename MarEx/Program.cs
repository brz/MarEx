using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MarEx
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 5)
            {
                Console.WriteLine("Insufficient arguments supplied");
                return;
            }
            else
            {
                string input = null;
                string output = null;
                bool decompress;
                
                switch(args[0])
                {
                    case "d":
                        decompress = true;
                        break;
                    case "c":
                        decompress = false;
                        break;
                    default:
                        Console.WriteLine("No compress or decompress argument supplied");
                        return;
                }
               
                for (var i = 1; i < args.Length; i += 2)
                {
                    if(args[i] == "-i")
                    {
                        input = args[i + 1];
                    }
                    else if(args[i] == "-o")
                    {
                        output = args[i + 1];
                    }
                }

                if (decompress)
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
                else
                {
                    Console.WriteLine("Compression not yet implemented");
                    return;
                }
            }
        }

        private static void Decompress(string inputFile, string outputFolder)
        {
            var bytes = File.ReadAllBytes(inputFile);

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
    }
}
