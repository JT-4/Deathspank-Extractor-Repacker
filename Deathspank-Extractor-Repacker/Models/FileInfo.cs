using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deathspank_Extractor_Repacker.Models
{
    public class FileInfo
    {
        /// <summary>
        /// This number simply keeps track of the number of FileInfo
        /// objects that have been created and placed into the UI
        /// bound observable collection
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The directory path of the file. Attribute 1
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// The name of the file. Attribute 2
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// The internal AssetID name of the file. Attribute 3
        /// </summary>
        public string ItemID { get; set; }
        /// <summary>
        /// the file extension. Useful for sorting the UI by file type. Not
        /// a true atrribute. Gets set automatically when filename is populated
        /// </summary>
        public string FileExt { get; set; }
        /// <summary>
        /// The offset to the start of the file's data stream in the archive. This
        /// Value is relative to the start of the archive file. Attribute 9
        /// </summary>
        public int FileOffset { get; set; }
        /// <summary>
        /// The compressed number of bytes in the file's data stream. Attribute 7
        /// </summary>
        public int CompressedSize { get; set; }
        /// <summary>
        /// The uncompressed number of bytes in the file's data stream. Can 
        /// be used to validate that the file uncompressed correctly. Attribute 6
        /// </summary>
        public int UncompressedSize { get; set; }

        /// <summary>
        /// A value showing if the file stream has been compressed or not. Attribute 8
        /// </summary>
        public int IsCompressed { get; set; }

        public int Attribute4 { get; set; }
        public int Attribute5 { get; set; }
    }
}
