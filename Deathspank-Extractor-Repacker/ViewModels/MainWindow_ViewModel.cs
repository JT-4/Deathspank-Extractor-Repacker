using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Deathspank_Extractor_Repacker.ViewModels
{
    public class MainWindow_ViewModel : INotifyPropertyChanged
    {
        #region Private members
            private Models.FileInfo _selectedfileInfo;
            private byte[] _headerBytes;

            
        #endregion

        #region Public members
        public Models.FileInfo SelectedFileInfo
        {
            get
            {
                return _selectedfileInfo;
            }
            set
            {
                if (value != _selectedfileInfo)
                {
                    _selectedfileInfo = value;
                    OnPropertyChanged("SelectedFileInfo");

                }
            }
        }

        public byte[] HeaderBytes { 
            get
            {
                return _headerBytes;
            }
            set
            {
                if (value != _headerBytes)
                {
                    _headerBytes = value;
                    OnHeaderBytesChanged();
                    OnPropertyChanged("HeaderBytes");

                }
            } 
        }

        public ICommand Export_SingleFile_Command { get; set; }

        public byte[] FileID { get; set;}
        public int FileNameDataLength { get; set;}
        public int NumArchiveFiles { get; set;}

        

        public ObservableCollection<Models.FileInfo> Files { get; set; }


        #endregion

        #region Constructor
        public MainWindow_ViewModel()
        {
            this.Export_SingleFile_Command = new RelayCommand(Export_SingleFile_Execute);

            Files = new ObservableCollection<Models.FileInfo>();
            HeaderBytes = new byte[16];

            //PopulateDummyInfo();
            //ParseGGArchive();
            //CreateReadableFileInfo();
            ParseGGArchiveNew();


        }

        private void Export_SingleFile_Execute()
        {
            
        }
        #endregion

        #region Public Methods
        public void PopulateDummyInfo()
        {
            Models.FileInfo DemoFileInfo = new Models.FileInfo();
            DemoFileInfo.CompressedSize = 1;
            DemoFileInfo.FileName = "name";
            DemoFileInfo.UncompressedSize = 2;
            DemoFileInfo.FileExt = ".file";
            DemoFileInfo.FileOffset = 69;
            DemoFileInfo.Id = 1;
            DemoFileInfo.ItemID = "2";

            Files.Add(DemoFileInfo);
            Files.Add(DemoFileInfo);
            Files.Add(DemoFileInfo);
            Files.Add(DemoFileInfo);
            Files.Add(DemoFileInfo);
            Files.Add(DemoFileInfo);
        }

        public void ParseGGArchive() 
        {
            string filepath = @"C:\Users\Joseph.Towns\OneDrive - Snohomish School District #201\Personal\Mods\Deathspank\GameData-000000000.gg";
            
            using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                
                byte[] HeaderBuffer = new byte[16];
                fs.Read(HeaderBuffer,0,16);
                HeaderBytes = HeaderBuffer;

                //get the file path and name objects. skip first 16 +800 take the amount from 
                // bytes 9-12. use that byte array to parse file names and whatever. Find a way to stream this instead of holding
                //in memory?
                fs.Seek(800, SeekOrigin.Current);
              
                int ChunkLength = FileNameDataLength;
                byte[] FileNameBuffer = new byte[ChunkLength];
                fs.Read(FileNameBuffer, 0, ChunkLength);



                //initialize a file counter to continue the loop until all files have been read
                int i = 0;
                string CurrentFileDirectory = "";
                int AttrPerFileType = 2;
                //Start a new loop that will run until the right number of files have been parsed
                for (int filecounter = 0; filecounter < NumArchiveFiles; filecounter++) 
                {
                    //crate a new fileinfor object and set the object ID
                    //Id will be used later to read the files in order and get their attributes
                    Models.FileInfo fileInfo = new Models.FileInfo()
                    {
                        //set the file Id
                        Id = filecounter
                    };

                    
                    //create a new loop. Each filinfo object has two parameter that can be read at this stage
                    //Get the filepath and the file name and add the objects to the base array
                    for (int j = 0; j < AttrPerFileType; j++)
                    {
                       //read the first 0 terminated string found
                        string currentstring = ReadNextString(FileNameBuffer, i);

                        //check if the currently found string is a file attribute or a file path
                        if (currentstring.EndsWith(@"/")) 
                        {
                            if(currentstring.EndsWith(@"chunks/"))
                            {
                                AttrPerFileType = 1;
                            }
                            else
                            {
                                AttrPerFileType = 2;
                            }
                            //set the working filepath 
                            CurrentFileDirectory = currentstring;
                            //advance index past the current string that was found
                            i = i + currentstring.Length + 1;
                            //get next string as currentstring
                            currentstring = ReadNextString(FileNameBuffer, i);
                        }
                        
                        //if this is the first of the two attributes it is the filename
                        if (j==0)
                        {
                            fileInfo.FileName = currentstring;
                        }
                        else //else it must must the 2nd attribute which is the internal fileID
                        {
                            fileInfo.ItemID = currentstring;
                        }
                        //advance the array index past the currently read string
                        i = i + currentstring.Length +1;

                    }
                    fileInfo.FilePath = CurrentFileDirectory;
                    //can I simply look up attributes here???
                    if (Files.Count == 637) 
                    { 
                    }

                    //add the current file information the UI bound observable collection. 
                    Files.Add(fileInfo);
                
                }
            }

        }

        public void CreateReadableFileInfo()
        {
            string filepath = @"C:\Users\Joseph.Towns\OneDrive - Snohomish School District #201\Personal\Mods\Deathspank\GameData-000000000_NEW.gg";

            using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] HeaderBuffer = new byte[16];
                fs.Read(HeaderBuffer, 0, 16);
                HeaderBytes = HeaderBuffer;

                fs.Seek(800, SeekOrigin.Current);

                int ChunkLength = FileNameDataLength;
                byte[] FileNameBuffer = new byte[ChunkLength];
                fs.Read(FileNameBuffer, 0, ChunkLength);

                
                byte[] output = new byte[FileNameDataLength];

                byte XorValue = 135;

                byte currentsinglebyte = new byte();

                for (int i =0;i < output.Length; i++)
                {
                    currentsinglebyte = FileNameBuffer[i];
                    
                    if(currentsinglebyte == 0)
                    {
                        XorValue = 135;
                        output[i] = 32;

                    }
                    else
                    {
                        byte unxored = (byte)(currentsinglebyte ^ XorValue);
                        output[i] = unxored;
                        XorValue++;
                    }

                    

                }

                File.WriteAllBytes(@"C:\users\joseph.towns\ds00000000.gg", output);


            }
        }

        public void ParseGGArchiveNew()
        {
            string filepath = @"C:\Users\Joseph.Towns\OneDrive - Snohomish School District #201\Personal\Mods\Deathspank\GameData-000000100.gg";

            using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                byte[] HeaderBuffer = new byte[16];
                fs.Read(HeaderBuffer, 0, 16);
                HeaderBytes = HeaderBuffer;

                //get the file path and name objects. skip first 16 +800 take the amount from 
                // bytes 9-12. use that byte array to parse file names and whatever. Find a way to stream this instead of holding
                //in memory?
                fs.Seek(800, SeekOrigin.Current);

                int ChunkLength = FileNameDataLength;
                byte[] FileNameBuffer = new byte[ChunkLength];
                fs.Read(FileNameBuffer, 0, ChunkLength);

                //we have the length of the string contining datablock
                //move past the strings data and start reading the 9 attributes per file data
                //loop through num of objects
                //loop through 9 attributes
                //get the first three, file path offset, filename offset, fileid offset
                //attributes 4,5 don't super matter
                // get 6,7 uncompressed and compressed size
                //8, is encrypted (obfuscated)
                //offset to begining of file stream from beginning of file, no relative offsets

                //fs read 4 bytes at a time.
                //Lookup value from table
                //after 9 attributes, add fileinfo to files and move to next
                //repeat until done
                byte[] atrributebuffer = new byte[36*NumArchiveFiles];
                

                for (int o =0; o < NumArchiveFiles; o++)
                {
                    //crate a new fileinfor object and set the object ID
                    //Id will be used later to read the files in order and get their attributes
                    Models.FileInfo fileInfo = new Models.FileInfo()
                    {
                        //set the file Id
                        Id = o
                    };


                    //create a loop to read through the 9 attributes
                    for (int a = 0; a<9; a++)
                    {

                        
                        fs.Read(atrributebuffer, ((o * 36) + (a * 4)), 4);
                        
                        switch (a) { 
                            case 0:
                                int offset = BitConverter.ToInt32(atrributebuffer, ((o * 36) + (a * 4)));
                                string currentstring = ReadNextString(FileNameBuffer, offset);
                                fileInfo.FilePath = currentstring;
                                break;
                            case 1:
                                offset = BitConverter.ToInt32(atrributebuffer, ((o * 36) + (a * 4)));
                                currentstring = ReadNextString(FileNameBuffer, offset);
                                fileInfo.FileName = currentstring;
                                break;
                            case 2:
                                offset = BitConverter.ToInt32(atrributebuffer, ((o * 36) + (a * 4)));
                                currentstring = ReadNextString(FileNameBuffer, offset);
                                fileInfo.ItemID = currentstring;
                                break;
                            case 3:
                                fileInfo.Attribute4 = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));

                                break;
                            case 4:
                                fileInfo.Attribute5 = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            case 5:
                                //new int value of last four bytes
                                //save as uncompressed
                                
                                fileInfo.UncompressedSize = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            case 6:
                                //new int value of last four bytes
                                //save as uncompressed
                                fileInfo.CompressedSize = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            case 7:
                                //new int value of last four bytes
                                //save as iscompressed
                                fileInfo.IsCompressed = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            case 8:
                                //new int value of last four bytes
                                //save as start of filestream
                                fileInfo.FileOffset = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            default:
                                break;
                        }
                    }

                    Files.Add(fileInfo);
                }

            }
        }

        private string ReadNextString(byte[] fileNameBuffer, int i)
        {
            int EndOfString = Array.IndexOf<byte>(fileNameBuffer, 0, i) - i;

            byte[] ObfNameBytes = fileNameBuffer.Skip(i).Take(EndOfString).ToArray();

            byte[] Namebytes = DSFileNameXor(ObfNameBytes);
            string Name = System.Text.Encoding.UTF8.GetString(Namebytes);
            
            return Name;
        }

        private byte[] DSFileNameXor(byte[] obfNameBytes)
        {
            byte XorValue = 135;
            byte[] ReturnBytes = new byte[obfNameBytes.Length];

            for (int i = 0;i < obfNameBytes.Length; i++)
            {
                ReturnBytes[i] = (byte)(obfNameBytes[i] ^ XorValue);
                XorValue++;

            }

            return ReturnBytes;

        }
        #endregion

        private void OnHeaderBytesChanged() { 
            FileID = HeaderBytes.Take(8).Skip(0).ToArray();
            FileNameDataLength = BitConverter.ToInt32(HeaderBytes.Skip(8).Take(4).ToArray(),0);
            NumArchiveFiles = BitConverter.ToInt32(HeaderBytes.Skip(12).Take(4).ToArray(),0); 
        }

        #region Propert Changed Method and Event

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the property changed event based on the property named passed in
        /// </summary>
        /// <param name="propertyName" > A string representing the display name of the propery
        /// raising the Property changed event</param>
        public void OnPropertyChanged(string propertyName)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
