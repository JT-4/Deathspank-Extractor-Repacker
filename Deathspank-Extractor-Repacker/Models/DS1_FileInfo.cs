using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deathspank_Extractor_Repacker.Models
{
    public class DS1_FileInfo : INotifyPropertyChanged
    {
        private string filepath;
        private string filename;
        private byte[] attr4;
        private byte[] attr5;
        /// <summary>
        /// This number simply keeps track of the number of FileInfo
        /// objects that have been created and placed into the UI
        /// bound observable collection
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The directory path of the file. Attribute 1
        /// </summary>
        public string FilePath
        {
            get
            {
                return filepath;
            }
            set
            {
                if (value != filepath)
                {
                    filepath = value;
                    OnFilePathChanged(value);
                    OnPropertyChanged("FilePath");
                }
            }
        }

        public byte[] Attr1_FilepathOffset { get; set;}

        public string WinFilePath { get; set; }
        /// <summary>
        /// The name of the file. Attribute 2
        /// </summary>
        public string FileName 
        { 
            get 
            {
                return filename;
            } 
            set
            {
                if(value != filename)
                {
                    filename = value;
                    OnFileNameChanged();
                }
            }
        }

        private void OnFileNameChanged()
        {
            FullFilePathName = $@"{WinFilePath}{FileName}";
        }

        public string FullFilePathName { get; set; }

        public byte[] Attr2_FilenameOffset { get; set; }
        /// <summary>
        /// The internal AssetID name of the file. Attribute 3
        /// </summary>
        public string ItemID { get; set; }

        public byte[] Attr3_ItemID { get; set; }
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
        public byte[] Attr9_FileOffset { get; set; }
        /// <summary>
        /// The compressed number of bytes in the file's data stream. Attribute 7
        /// </summary>
        public int CompressedSize { get; set; }
        public byte[] Attr7_CompressedSize { get; set; }
        /// <summary>
        /// The uncompressed number of bytes in the file's data stream. Can 
        /// be used to validate that the file uncompressed correctly. Attribute 6
        /// </summary>
        public int UncompressedSize { get; set; }

        public byte[] Attr6_UncompressedSize { get; set; }

        /// <summary>
        /// A value showing if the file stream has been compressed or not. Attribute 8
        /// </summary>
        public int IsCompressed { get; set; }

        public byte[] Attr8_IsCompressed { get; set; }

        public byte[] Attr4
        {
            get
            {
                return attr4;
            }
            set
            {
                attr4 = value;
                Attribute4 = BitConverter.ToInt32(attr4,0);
            } 
        }
        public int Attribute4 { get; private set;}
        public byte[] Attr5 
        {
            get
            {
                return attr5;
            }
            set
            {
                attr5 = value;
                Attribute5 = BitConverter.ToInt32(attr5, 0);
            }
        }
        public int Attribute5 { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnFilePathChanged(string fp)
        {
             WinFilePath = fp.Replace(@"/", @"\");
    
        }

        public void OnPropertyChanged(string propertyName)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
