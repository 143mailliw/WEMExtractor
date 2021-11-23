using System;
using System.Collections.Generic;

namespace AmnesiaExtractor {
  public static class NameFinder {
    public static List<INameFinderData> FindName(AkMediaAsset asset) {
      List<INameFinderData> result = new List<INameFinderData>();

      if(asset.Type == "AkAudioEventData") {
        if(asset.Properties != null && asset.Properties.MediaList != null) {
          foreach(AkMediaListItem item in asset.Properties.MediaList) {  
            if(AmnesiaExtractor.DisplayOptions.DisplayDebug) {
              Console.WriteLine("Found " + asset.Outer + " in " + item.ObjectName);
            }
            INameFinderData nameData = new INameFinderData();
            nameData.Name = asset.Outer;
            nameData.Id = (item.ObjectName ?? "AkMediaAsset null").Replace("AkMediaAsset ", "");
            result.Add(nameData);
          }
        }
      }  

      return result;
    } 
  }

  public class INameFinderData {
    public string? Name;
    public string? Id;
  }
}
