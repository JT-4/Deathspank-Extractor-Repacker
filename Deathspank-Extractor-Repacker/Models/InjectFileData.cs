using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deathspank_Extractor_Repacker.Models
{
    internal class InjectFileData
    {
        public string RelativePath { get; set; }
        public string FullFilePath { get; set; }
        public string CorrectedRelativePath { get; set;}
    }
}
