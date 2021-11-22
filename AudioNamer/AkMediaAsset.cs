using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NameFinder
{
    // implement just enough to find the file name and file id
    class AkMediaAsset
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Outer { get; set; }
        public AkAudioEventProperties Properties { get; set; }
    }
}
