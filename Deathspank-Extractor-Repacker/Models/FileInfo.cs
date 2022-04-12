using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deathspank_Extractor_Repacker.Models
{
    public class FileInfo
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ItemID { get; set; }
        public string FileExt { get; set; }

        public string FileOffset { get; set; }
        public string CompressedSize { get; set; }
        public string UncompressedSize { get; set; }
    }
}
