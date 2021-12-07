using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WEMSharp;

namespace AmnesiaExtractor {
  public class Converter {
    static string revorbBinary = OperatingSystem.IsWindows() ? "Revorb.exe" : "revorb";
    static string vgmstreamBinary = OperatingSystem.IsWindows() ? "vgmstream-cli.exe" : "vgmstream-cli";
    string revorbPath = Path.Combine(AppContext.BaseDirectory, revorbBinary);
    string vgmstreamPath = Path.Combine(AppContext.BaseDirectory, "vgmstream", vgmstreamBinary);
    bool useRevorb = false;
    bool useVGMStream = false;

    public string oggPath = "";

    public void VerifyFiles() { 
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
        Console.WriteLine("Revorb binary not found, ogg file integrity may be compromised. We're looking for '" + revorbBinary + "'.");
        if(OperatingSystem.IsMacOS()) {
          Console.WriteLine("This program does not come with Revorb on macOS. You will have to build it yourself.");
        }
        Console.ResetColor();
      }

      if(File.Exists(vgmstreamPath)) {
        if(OperatingSystem.IsLinux()) {
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine("The required libraries for vgmstream must be manually installed on Linux. Please see 'https://vgmstream.org/doc/USAGE'.");
          Console.ResetColor();
        }
        useVGMStream = true;
      } else {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("The vgmstream binary is missing. PCM audio will not be converted (it will be marked as failed). We're looking for 'vgmstream/" + vgmstreamBinary + "'.");
        if(OperatingSystem.IsMacOS()) {
          Console.WriteLine("This program does not come with vgmstream on macOS. You will have to build it yourself.");
        }
        Console.ResetColor();
      }

      bool[] array = {useRevorb, useVGMStream};
    }

    public int GetCodec(String wem) {
      FileStream stream = File.OpenRead(wem);
      stream.Seek(20, SeekOrigin.Begin);
      byte[] codecBytes = new byte[2];
      stream.Read(codecBytes, 0, 2);
     
      int codec = BitConverter.ToUInt16(codecBytes);

      if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
        Array.Reverse(codecBytes);
        Console.WriteLine();
        Console.WriteLine("Codec ID: 0x" + Convert.ToHexString(codecBytes));
      }

      return codec;
    }

    private int ConvertVGMStream(String wem) {
      if(useVGMStream) {
        try {
          string name = wem.Split(Path.DirectorySeparatorChar).Last().Replace(".wem", ".wav");
          ProcessStartInfo startInfo = new ProcessStartInfo();

          startInfo.CreateNoWindow = true;
          startInfo.UseShellExecute = false;
          startInfo.RedirectStandardOutput = true;
          startInfo.RedirectStandardError = true;
          startInfo.RedirectStandardInput = true;
          startInfo.WindowStyle = ProcessWindowStyle.Hidden;
          startInfo.FileName = vgmstreamPath;
          startInfo.Arguments = "-o \"" + Path.Combine(oggPath, name) + "\" \"" + wem + "\"";

          Process? process = new Process();
          process.StartInfo = startInfo;
          process.Start();
          if(process != null) {
            process.WaitForExit();
          }
          return 0;
        } catch {
          return 1;
        }
      } else {
        return 1;
      }
    }

    private int ConvertVorbis(String wem) {
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
        return 0;
      } catch {
        // we can't transparently convert this file, try using ConvertVGMStream
        return ConvertVGMStream(wem);
      }
    }

    public int ConvertWem(String wem) {
      switch(GetCodec(wem)) {
        case 0xFFFF:
          return ConvertVorbis(wem);
        case 0x8311:
          return ConvertVGMStream(wem);
        default:
          return 2;
      }
    }
  }
}
