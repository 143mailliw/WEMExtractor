using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace DefinetlyNotBadAudioExtractionTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DefinetlyNotBadAudioExtractionTool");
            Console.WriteLine("This *should* work.");
            Console.WriteLine("I can only confirm that it works with Kid A Mnesia though, so that's definetly a *should* for anything else");
            Console.WriteLine("I'm probably not going to work on this any more for things that are not bugs that break extracting from Kid A Mnesia, since that's what it was meant to do");
            Console.WriteLine("Usage: DefinetlyNotBadAudioExtractionTool.exe <path>");
            Console.WriteLine("- william341");
            int filesScanned = 0;
            int filesC1 = 0;
            int filesC2 = 0;
            int filesCorrectC1 = 0;
            int filesCorrectC2 = 0;
            int filesBadHeaderSingle = 0;
            int filesBadHeaderC1 = 0;
            int filesBadHeaderC2 = 0;
            int filesBadExp = 0;
            int filesMissing = 0;
            if (args.Length > 0)
            {
                if (Directory.Exists(args[0]))
                {
                    if(Directory.Exists(Path.Combine(args[0], "Exported")))
                    {
                        Console.WriteLine("Exported directory already exists.");
                        Console.WriteLine("Press y to delete it, or any other key to cancel.");
                        if(Console.ReadKey().Key == ConsoleKey.Y)
                        {
                            Directory.Delete(Path.Combine(args[0], "Exported"), true);
                        }
                        else
                        {
                            Environment.Exit(1);
                        }
                    }
                    Console.WriteLine("Creating " + args[0] + "\\Exported");
                    Directory.CreateDirectory(Path.Combine(args[0], "Exported"));

                    string[] files = Directory.GetFiles(args[0], "*.uexp");
                    foreach (string file in files)
                    {
                        Console.WriteLine("Scanning " + file);
                        filesScanned++;
                        FileInfo fileInfo = new FileInfo(file);

                        // this is probably not a fantastic idea
                        string basePath = file.Substring(0, file.Length - 5);
                        string ubulkPath = basePath + ".ubulk";
                        if (File.Exists(ubulkPath))
                        {
                            string[] fullPath = basePath.Split(Path.DirectorySeparatorChar);
                            string assetId = fullPath[fullPath.Length - 1];
                            Console.WriteLine("Valid asset " + assetId + " found.");
                            // this is also probably not a fantastic idea
                            if (fileInfo.Length < 168)
                            {
                                Console.WriteLine("Single chunk asset found (fingers crossed), copying");
                                filesC1++;
                                FileStream stream = File.OpenRead(ubulkPath);
                                // read the first 4 bytes to ensure that it is an audio file
                                byte[] header = new byte[4];
                                stream.Read(header, 0, 4);

                                if (Encoding.ASCII.GetString(header) == "RIFF")
                                {
                                    File.Copy(ubulkPath, Path.Combine(args[0], "Exported", assetId + ".wem"));
                                    filesCorrectC1++;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid file");
                                    filesBadHeaderSingle++;
                                }
                            }
                            else if (fileInfo.Length == 168)
                            {
                                Console.WriteLine("2 chunk asset found, splitting");
                                filesC2++;
                                Console.WriteLine("Loading .uexp file");
                                FileStream stream = File.OpenRead(ubulkPath);

                                // read the first 4 bytes to ensure that it is an audio file
                                byte[] header = new byte[4];
                                stream.Read(header, 0, 4);

                                if (Encoding.ASCII.GetString(header) == "RIFF")
                                {
                                    Console.WriteLine("Valid file");

                                    // hopefully read the offset
                                    // i hope to god this is always correct
                                    FileStream uexpStream = File.OpenRead(file);
                                    uexpStream.Seek(124, SeekOrigin.Begin);
                                    byte[] offset = new byte[4];
                                    uexpStream.Read(offset, 0, 4);
                                    Array.Reverse(offset);
                                    // it's too late to figure out how to do this right
                                    int offsetInt = int.Parse(Convert.ToHexString(offset), System.Globalization.NumberStyles.HexNumber);
                                    Console.WriteLine("Offset: 0x" + Convert.ToHexString(offset));

                                    // read the first chunk
                                    byte[] chunk1 = new byte[offsetInt];
                                    stream.Seek(0, SeekOrigin.Begin);
                                    stream.Read(chunk1, 0, offsetInt);
                                    File.WriteAllBytes(Path.Combine(args[0], "Exported", assetId + "-chunk1.wem"), chunk1);
                                    Console.WriteLine("Chunk 1 written");

                                    // read the second chunk
                                    byte[] chunk2 = new byte[stream.Length - offsetInt];
                                    stream.Seek(offsetInt, SeekOrigin.Begin);
                                    stream.Read(chunk2, 0, (int)(stream.Length - offsetInt));
                                    if(Encoding.ASCII.GetString(chunk2[0..4]) == "RIFF")
                                    {
                                        File.WriteAllBytes(Path.Combine(args[0], "Exported", assetId + "-chunk2.wem"), chunk2);
                                        Console.WriteLine("Chunk 2 written");
                                        filesCorrectC2++;
                                    } else
                                    {
                                        Console.WriteLine("Invalid chunk 2, bad header");
                                        filesBadHeaderC2++;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Invalid file");
                                    filesBadHeaderC1++;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Multi-chunk asset found. This is not handled. Skipping");
                                Console.WriteLine("File Length" + fileInfo.Length.ToString());
                                filesBadExp++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Missing associated .ubulk file, skipping (invalid asset)");
                            filesMissing++;
                        }
                    }

                    Console.WriteLine("Files scanned: " + filesScanned.ToString());
                    Console.WriteLine("Files with invalid/unknown .uexp: " + filesBadExp.ToString());
                    Console.WriteLine("Files with no .ubulk file: " + filesMissing.ToString());
                    Console.WriteLine("Files started processing with 1 chunk: " + filesC1.ToString());
                    Console.WriteLine("Files started processing with 2 chunks: " + filesC2.ToString());
                    Console.WriteLine("Files correctly processed with 1 chunk: " + filesCorrectC1.ToString());
                    Console.WriteLine("Files correctly processed with 2 chunks: " + filesCorrectC2.ToString());
                    Console.WriteLine("---- SOME NON-RIFF AUDIO FILES ARE NORMAL ON SINGLE CHUNK FILES ----");
                    Console.WriteLine("Files with bad header (not an RIFF audio file) single chunk: " + filesBadHeaderSingle.ToString());
                    Console.WriteLine("Files with bad header (not an RIFF audio file) on chunk 1: " + filesBadHeaderC1.ToString());
                    Console.WriteLine("Files with bad header (not an RIFF audio file or mis-aligned) on chunk 2: " + filesBadHeaderC2.ToString());
                }
                else
                {
                    Console.WriteLine("Invalid folder.");
                }
            }
            else
            {
                Console.WriteLine("Drop a folder with .ubulk and .uexp files on to the executable.");
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
