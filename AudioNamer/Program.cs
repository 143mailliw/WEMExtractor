using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace NameFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                List<String> items = new List<String>();

                items.Add("Extracted on " + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + ".");

                string[] files = Directory.GetFiles(args[0], "*.json", SearchOption.AllDirectories);

                foreach(string file in files)
                {
                    Console.WriteLine("Scanning " + file);
                    string jsonData = File.ReadAllText(file);

                    if (jsonData.Contains("AkAudioEventData"))
                    {
                        if(jsonData.Contains("MediaList"))
                        {
                            AkMediaAsset[] assets = JsonConvert.DeserializeObject<AkMediaAsset[]>(jsonData);
                            foreach(AkMediaAsset asset in assets)
                            {
                                if(asset.Type == "AkAudioEventData")
                                {
                                    if(asset.Properties != null && asset.Properties.MediaList != null)
                                    {
                                        foreach(AkMediaListItem item in asset.Properties.MediaList)
                                        {
                                            Console.WriteLine("Found " + asset.Outer + " in " + item.ObjectName);
                                            items.Add(asset.Outer + ": " + item.ObjectName);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No media in event, skipping...");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No audio event data, skipping...");
                    }

                    File.WriteAllLines(Path.Combine(args[0], "extractedNames.json"), items);
                }
            }
            else
            {
                Console.WriteLine("NameFinder.exe <path/to/json/folder>");
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
