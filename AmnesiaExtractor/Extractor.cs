using System;
using System.Text;

namespace AmnesiaExtractor {
  public static class Extractor {
    public static byte[] Extract(byte[] bulkData, byte[] exp) {
      if (exp.Length < 168) {
        if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
          Console.WriteLine("Single chunk asset found (fingers crossed), copying");
        }
        // read the first 4 bytes to ensure that it is an audio file
        byte[] header = new byte[4];
        header = bulkData[0..4];

        if (Encoding.ASCII.GetString(header) == "RIFF") {
          return bulkData;
        } else {
          if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
            Console.WriteLine("Invalid file");
          }
        }
      }
      else if (exp.Length == 168) {
        if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
          Console.WriteLine("2 chunk asset found, splitting");
        }
        // read the first 4 bytes to ensure that it is an audio file
        byte[] header = new byte[4];
        header = bulkData[0..4];

        if (Encoding.ASCII.GetString(header) == "RIFF") {
          if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
            Console.WriteLine("Valid file");
          }

          // hopefully read the offset
          // i hope to god this is always correct
          byte[] offset = new byte[4];

          offset = exp[124..128];
          Array.Reverse(offset);

          // it's too late to figure out how to do this right
          int offsetInt = int.Parse(Convert.ToHexString(offset), System.Globalization.NumberStyles.HexNumber);

          if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
            Console.WriteLine("Offset: 0x" + Convert.ToHexString(offset));
          }

          // read the second chunk
          byte[] chunk2 = bulkData[offsetInt..bulkData.Length];
          if(Encoding.ASCII.GetString(chunk2[0..4]) == "RIFF") {
            return chunk2; 
          } else {
            if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
              Console.WriteLine("Invalid chunk 2, bad header");
            }
          }
        } else {
          if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
            Console.WriteLine("Invalid file");
          }
        }
      } else {
        if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
          Console.WriteLine("Multi-chunk asset found. This is not handled. Skipping");
        }
      }

      return new byte[0];
    }
  }
}
