using System;
using System.IO;
namespace Renamer
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                if (File.Exists(Path.Combine(args[0], "extractedNames.txt")))
                {
                    string[] lines = File.ReadAllLines(Path.Combine(args[0], "extractedNames.txt"));

                    if (lines.Length > 1)
                    {
                        if (lines[0].StartsWith("Extracted on"))
                        {
                            foreach (string line in lines)
                            {
                                if (line.Contains(": AkMediaAsset "))
                                {
                                    string[] data = line.Split(": AkMediaAsset ");

                                    Console.WriteLine("Found valid line with file " + data[0] + " with ID " + data[1]);

                                    if(File.Exists(Path.Combine(args[0], data[1] + "-chunk2.wem")))
                                    {
                                        Console.WriteLine("2nd chunk file found, renaming");
                                        File.Move(Path.Combine(args[0], data[1] + "-chunk2.wem"), Path.Combine(args[0], data[0] + " (" + data[1] + ").wem"));
                                    }
                                    else if(File.Exists(Path.Combine(args[0], data[1] + ".wem")))
                                    {
                                        Console.WriteLine("Single chunk file found, renaming");
                                        File.Move(Path.Combine(args[0], data[1] + ".wem"), Path.Combine(args[0], data[0] + " (" + data[1] + ").wem"));
                                    }
                                    else
                                    {
                                        Console.WriteLine("No appropriate file found, skipping");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Invalid line '" + line + "', skipping");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid extractedNames.txt");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid extractedNames.txt");
                    }
                }
                else
                {
                    Console.WriteLine("No extractedNames.txt found at " + Path.Combine(args[0], "extractedNames.txt"));
                }
            }
            else
            {
                Console.WriteLine("Renamer.exe <path/to/directory/with/wems/and/extractedNames>");
            }
        }
    }
}
