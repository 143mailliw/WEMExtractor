using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using CUE4Parse.FileProvider;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using WEMSharp;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("william341's AmnesiaExtractor");
Console.WriteLine("This program is licenced under the GPL (version 3.0). Please see https://www.gnu.org/licenses/gpl-3.0.en.html for more information.");
Console.WriteLine("Special thanks:");
Console.WriteLine("        CUE4Parse team: UE4 asset parsing library");
Console.WriteLine("        Crauzer: WEMSharp library, which I modified to work on .NET 6 (https://github.com/143mailliw/WEMSharp)");
Console.WriteLine("        Yirkha, ItsBranK, Jiri Hruska: various versions of Revorb");
Console.WriteLine();

if(args.Length > 0) {
  bool skipFileIntegrityChecks = false;
  if (OperatingSystem.IsMacOS()) {
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("macOS detected, skipping file integrity checks.");
    Console.WriteLine("This tool is only officially supported & tested on Linux and Windows, and only with the Windows version of the game.");
    Console.WriteLine("If you have problems, try installing the game in CrossOver/Wine, and then use the Windows version in this tool.");
    Console.WriteLine("(if you feel the need to ask to fix it, please remember that in order to port this tool to macOS I would have to spend 800+ dollars)");
    Console.WriteLine();
    skipFileIntegrityChecks = true;
    Console.ResetColor();
  } else {
    Console.WriteLine("Searching for KidAMnesia.exe...");
  }

  if(Directory.Exists(args[0]) && (File.Exists(Path.Combine(args[0], "KidAMnesia.exe")) || skipFileIntegrityChecks)) {
    if(!skipFileIntegrityChecks) {
      Console.WriteLine("Found KidAMnesia.exe.");
    } else {
      Console.WriteLine("Unsupported platform: skipping executable verification checks.");
    }
    string[] paks = new string[7];

    if(!skipFileIntegrityChecks) {
      for(int i = 0; i < 7; i++) {
        paks[i] = Path.Combine(args[0], "Paperbag", "Content", "Paks", "pakchunk0_s" + (i + 1) + "-WindowsNoEditor.pak");

        if(!File.Exists(paks[i])) {
          Console.WriteLine("Pak file " + paks[i] + " missing. You should validate your game files. Exiting...");
          Console.WriteLine("Press any key to exit...");
          Console.ReadKey();
          Environment.Exit(1);
        }
      }
    } else {
      Console.WriteLine("Unsupported platform: skipping pak verification checks.");
    }
    
    Console.WriteLine();
    Console.WriteLine("We need the game's AES decryption key in order to search it's files.");
    Console.WriteLine("You'll have to find this yourself. This is easy on Windows, but very difficult on Linux and nearly impossible on macOS.");
    Console.WriteLine("Please do not ask me for the key on reddit. I cannot give it to you and will not respond if asked for it.");
    Console.WriteLine("(These aren't distributed with the program to avoid the wrath of the IFPI & RIAA)");
    Console.Write("Please enter AES key (eg. 0xABC123...): ");
    string key = Console.ReadLine() ?? "";

    if (key.StartsWith("0x") && key.Length == 66) {
      Console.WriteLine("Key looks valid, attempting to open...");
      Console.WriteLine("If we crash after this, check your key.");
      Console.WriteLine();

      DefaultFileProvider provider = new DefaultFileProvider(Path.Combine(args[0], "Paperbag", "Content", "Paks"), SearchOption.TopDirectoryOnly);
      provider.Initialize();
      provider.SubmitKey(new FGuid(), new FAesKey(key));
      provider.LoadMappings();
      provider.LoadLocalization(CUE4Parse.UE4.Versions.ELanguage.English);
      
      string basePath = Path.Combine(AppContext.BaseDirectory, "KidAMnesiaExported");
      string wemPath = Path.Combine(AppContext.BaseDirectory, "KidAMnesiaExported", "wems");
      string unknownPath = Path.Combine(AppContext.BaseDirectory, "KidAMnesiaExported", "unknown");
      string oggPath = Path.Combine(AppContext.BaseDirectory, "KidAMnesiaExported", "ogg");

      Console.Write("Would you like to rename the files to their known names? Some files do not have known names and will just be named their ID numbers. (Y/n) ");
      ConsoleKeyInfo renameKey = Console.ReadKey();
      Console.WriteLine();

      Console.Write("Would you like to convert all convertable files into OGG files? Some files cannot be converted (you will be notified). All WEM files will still be present. (Y/n) "); 
      ConsoleKeyInfo oggKey = Console.ReadKey();
      Console.WriteLine();

      if(Directory.Exists(basePath) && !(args.Length > 1 && args[1] == "ogg") ) {
        Console.Write(basePath + " already exists. Press Y to delete or any other key to exit. (y/N) ");
        if(Console.ReadKey().Key == ConsoleKey.Y) {
          Directory.Delete(basePath, true);
          Console.WriteLine();
        } else {
          Environment.Exit(2);
        }
      } else {
        bool oggBool = oggKey.Key == ConsoleKey.Y || oggKey.Key == ConsoleKey.Enter;
        Console.Write("This operation will use approximately " + (oggBool ? "5GB" : "2.5GB") + " of disk space. Are you sure you want to continue? (Y/n) ");
        ConsoleKeyInfo proccedKey = Console.ReadKey();
        if(proccedKey.Key == ConsoleKey.Y || proccedKey.Key == ConsoleKey.Enter) {
          Console.WriteLine();
        } else {
          Environment.Exit(3);
        }
      }

      if(args.Length > 1) {
        if(args[1] == "debug") {
          AmnesiaExtractor.DisplayOptions.DisplayDebug = true;
        }
      }

      Console.WriteLine();

      Directory.CreateDirectory(basePath);
      Directory.CreateDirectory(wemPath);
      Directory.CreateDirectory(unknownPath);
      if(oggKey.Key == ConsoleKey.Y || oggKey.Key == ConsoleKey.Enter) {
        Directory.CreateDirectory(oggPath);
      }

      // I don't know a better way to do this and CUE4Parse has basically no documentation
      int scanned = 0;
      if(!(args.Length > 1 && args[1] == "ogg")) {
        foreach(var file in provider.Files) {
          scanned++;
          Console.Write("\rScanning files for audio to export: " + scanned + "/" + provider.Files.Count());
          if(file.Key.StartsWith("Paperbag/Content/WwiseAudio/Media") && file.Key.EndsWith(".uasset")) {
            var exports = provider.LoadObjectExports(file.Key);
            foreach(var export in exports) {
              if(export.Name.Contains("AkMediaAssetData")) {
                if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
                  Console.WriteLine("Audio asset " + export.Outer + " found");
                  Console.WriteLine("Attempting to save WEM data...");
                }

                if(provider.Files.ContainsKey(file.Key.Replace(".uasset", ".ubulk"))) {
                  byte[] loadedObject = provider.SaveAsset(file.Key.Replace(".uasset", ".ubulk"));
                  byte[] loadedExp = provider.SaveAsset(file.Key.Replace(".uasset", ".uexp"));

                  string assetId = file.Key.Split("/").Last().Split(".").First();
                  byte[] finalData = AmnesiaExtractor.Extractor.Extract(loadedObject, loadedExp);
                  if(finalData.Length == 0) {
                    File.WriteAllBytes(Path.Combine(unknownPath, assetId + ".unknown"), loadedObject);
                  } else {
                    File.WriteAllBytes(Path.Combine(wemPath, assetId + ".wem"), finalData);
                  }
                } else {
                  if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
                    Console.WriteLine("No data, skipping..");
                  }
                }
              }
            }
          }
        }
        Console.WriteLine();

        // rename files 
        if(renameKey.Key == ConsoleKey.Enter || renameKey.Key == ConsoleKey.Y) {
          scanned = 0;
          foreach(var file in provider.Files) {
            scanned++;
            Console.Write("\rScanning files for names: " + scanned + "/" + provider.Files.Count());
            if(file.Key.StartsWith("Paperbag/Content/WwiseAudio/Events/Default_Work_Unit") && file.Key.EndsWith(".uasset")) {
              var exports = provider.LoadObjectExports(file.Key);
              foreach(var export in exports) {
                if(export.Name.Contains("AkAudioEventData")) {
                  string jsonified = JsonConvert.SerializeObject(export);
                  AmnesiaExtractor.AkMediaAsset? asset = JsonConvert.DeserializeObject<AmnesiaExtractor.AkMediaAsset>(jsonified);
                  if(asset != null) {
                    List<AmnesiaExtractor.INameFinderData> nameData = AmnesiaExtractor.NameFinder.FindName(asset);

                    foreach(AmnesiaExtractor.INameFinderData data in nameData) {
                      if(data.Name != null && data.Id != null) {
                        if(AmnesiaExtractor.DisplayOptions.DisplayDebug)
                          Console.WriteLine("Searching for " + data.Id + ".wem...");
                        if(File.Exists(Path.Combine(wemPath, data.Id + ".wem"))) {   
                          if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
                            Console.WriteLine("Found file, renaming...");
                          }
                          File.Move(Path.Combine(wemPath, data.Id + ".wem"), Path.Combine(wemPath, data.Name + " (" + data.Id + ").wem" ));
                        }
                      }
                    }
                  }
                }
              }
            }
          }
          Console.WriteLine();
        }
      }

      // convert files
      if(oggKey.Key == ConsoleKey.Enter || oggKey.Key == ConsoleKey.Y) {
        scanned = 0;
        int failed = 0;

        string revorbBinary = OperatingSystem.IsWindows() ? "Revorb.exe" : "revorb";
        string revorbPath = Path.Combine(AppContext.BaseDirectory, revorbBinary);
        bool useRevorb = false;

        if(File.Exists(revorbPath)) {
          if(OperatingSystem.IsLinux()) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("The Linux Revorb binary is compiled on Arch Linux.");
            Console.WriteLine("If your distro ships an out of date version of glibc, you may encounter a crash here.");
            Console.ResetColor();
          }
          useRevorb = true;
        } else {
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine("Revorb binary not found, ogg file integrity may be compromised.");
          if(OperatingSystem.IsMacOS()) {
            Console.WriteLine("This program does not come with Revorb on macOS. You will have to build it yourself.");
          }
          Console.ResetColor();
        }


        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("WARNING: Ogg support is (and always will be) incomplete. You should expect about ~200 files to fail. The majority of them are sound effects, about ~75 aren't.");
        Console.ResetColor();

        string[] wems = Directory.GetFiles(wemPath);
        foreach(var wem in wems) {
          scanned++;
          Console.Write("\rConverting to Ogg: " + scanned + "/" + wems.Length + " (" + failed + "/" + scanned + " failed)"); 
          try {
            WEMFile file = new WEMFile(wem, WEMForcePacketFormat.NoForcePacketFormat);
            string name = wem.Split(Path.DirectorySeparatorChar).Last().Replace(".wem", ".ogg");
            file.GenerateOGG(Path.Combine(oggPath, name), Path.Combine(AppContext.BaseDirectory, "codebook.bin"), false, false);
            if(useRevorb) {
              ProcessStartInfo startInfo = new ProcessStartInfo();
              
              startInfo.CreateNoWindow = true;
              startInfo.UseShellExecute = false;
              startInfo.RedirectStandardOutput = true;
              startInfo.RedirectStandardError = true;
              startInfo.RedirectStandardInput = true;
              startInfo.WindowStyle = ProcessWindowStyle.Hidden;
              startInfo.FileName = revorbPath;
              startInfo.Arguments = "\"" + Path.Combine(oggPath, name) + "\"";

              Process? process = Process.Start(startInfo);
              if(process != null) {
                process.WaitForExit();
              }
            }
          } catch {
            failed++;
          }
        }
        Console.WriteLine();
      }

      Console.WriteLine("Files saved to " + basePath + ".");
    } else {
      Console.WriteLine("Invalid key format. Key must start with 0x and be at least 64 characters long.");
    }
  }
} else {
  Console.WriteLine("KidAMnesia.exe not found. Please use the folder the game is located in (eg. C:\\Program Files\\Epic Games\\KidAMnesiaExhibition).");
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
