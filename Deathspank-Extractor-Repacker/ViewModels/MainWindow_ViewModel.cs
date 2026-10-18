using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace Deathspank_Extractor_Repacker.ViewModels
{
    public class MainWindow_ViewModel : INotifyPropertyChanged
    {
        #region Private members
        //private backing for SelectedFileName to impliment onpropertychanged
        private Models.DS1_FileInfo _selectedfileInfo;
        //private backing for HeaderBytes to impliment onpropertychanged
        private byte[] _headerBytes;
        //private backing for CurrentOpenFileName to impliment onpropertychanged
        private string currentOpenFileName;
        //private backing for NumArchiveFiles to impliment onpropertychanged
        private int numarchivefiles;
        //private backing for RepackFileDirectory to impliment onpropertychanged
        private string repackfiledirectory;
        //private backing for CurrentOpenFileFullName to impliment onpropertychanged
        private string currentopenfilefullname;
        //private backing for TasksCompleted to impliment onpropertychanged
        private int taskscompleted;
        //private backing for PercentComplete to impliment onpropertychanged
        private decimal percentcomplete;
        //private backing for ProgressText to impliment onpropertychanged
        private string progresstext;
        //private backing for OperationText to impliment onpropertychanged
        private string operationtext;
        #endregion

        #region Public members
        /// <summary>
        /// The filestream block of data must align to an 8 byte value. This parameter
        /// denotes the number of 0s between the file name/attribute block of data and the
        /// file streams. Set each time a file header is read
        /// </summary>
        public int FileStreamBegin_BufferSize { get; set; }

        /// <summary>
        /// Each game in the series has a .gg file hash at the beginning of the file. 
        /// DS1 files are 800 bytes and DS2/3 are 960 bytes 
        /// </summary>
        public int HashLength { get; set; }

        /// <summary>
        /// Save a string representing the full filepath of the currently opened
        /// Datadict file. Implement notify on property changed.
        /// </summary>
        public string CurrentOpenFileFullName 
        {
            get 
            { 
                return currentopenfilefullname;
            }
            set 
            {
                if (value != currentopenfilefullname)
                {
                    currentopenfilefullname = value;
                    SetOpenFileName(value);
                    OnPropertyChanged("CurrentOpenFileName");
                    OnPropertyChanged("CurrentOpenFileFullName");

                }
            } 
        }

        /// <summary>
        /// A string describing the currently performed operation
        /// gets displayed next to progress bar
        /// </summary>
        public string OperationText
        {
            get
            {
                return operationtext;
            }
            set
            {
                if (value != operationtext)
                {
                    operationtext = value;
                    OnPropertyChanged("OperationText");

                }
            }
        }

        /// <summary>
        /// A string holding the file path where a .gg archive will be extracted
        /// </summary>
        public string ArchiveExportPath { get; set; }


        public string RepackArchive { get; set; }
        /// <summary>
        /// Save a string representing the filename of the currently opened
        /// Datadict file. Implement notify on property changed.
        /// </summary>
        public string CurrentOpenFileName
        {
            get
            {
                return currentOpenFileName;
            }
            set
            {
                if (value != currentOpenFileName)
                {
                    currentOpenFileName = value;
                    OnPropertyChanged("CurrentOpenFileName");
                }
            }
        }

        public string RepackFileDirectory
        {
            get
            {
                return repackfiledirectory;
            }
            set
            {
                if (value != repackfiledirectory)
                {
                    repackfiledirectory = value;
                    OnPropertyChanged("RepackFileDirectory");
                }
            }
        }

        public string ProgressText
        {
            get
            {
                return progresstext;
            }
            set
            {
                if (value != progresstext)
                {
                    progresstext = value;
                    OnPropertyChanged("ProgressText");
                }
            }
        }

        public Models.DS1_FileInfo SelectedFileInfo
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
        public ICommand OpenFile_Command_DS1 { get; set; }
        public ICommand OpenFile_Command_DS2_3 { get; set; }
        public ICommand Export_AllFiles_Command { get; set; }

        public byte[] FileID { get; set;}
        public int FileNameDataLength { get; set;}

        public string ExportFilePath { get;set; }
        public int NumArchiveFiles 
        {
            get
            {
                return numarchivefiles;
            }
            set
            {
                if (value != numarchivefiles)
                {
                    numarchivefiles = value;
                    OnPropertyChanged("NumArchiveFiles");

                }
            }
        }

        public int TasksToComplete { get; set; }
        public int TasksCompleted { 
            get
            {
                return taskscompleted;
            }
            set 
            {
                if (value != taskscompleted)
                {
                    taskscompleted = value;
                    OnPropertyChanged("TasksCompleted");
                    CalculatePercentCompleted();                }  
            } 
        }

        public decimal PercentComplete {
            get 
            {
                return percentcomplete; 
            }
            set
            {
                if (value != percentcomplete)
                {
                    percentcomplete = value;
                    OnPropertyChanged("PercentComplete");
                }
            }
        }

        public Models.DS1_FileInfo ExportFile { get; set; }

        public ObservableCollection<Models.DS1_FileInfo> Files { get; set; }
        public ObservableCollection<Models.DS1_FileInfo>SelectedFiles { get; set; }

        public RelayCommand Import_Files_Command { get; set; }


        #endregion

        #region Constructor
        public MainWindow_ViewModel()
        {
            currentopenfilefullname = "No open file";
            this.Export_SingleFile_Command = new RelayCommand(Export_SingleFile_Execute);
            this.OpenFile_Command_DS1 = new RelayCommand(OpenFile_Execute_DS1);
            this.OpenFile_Command_DS2_3 = new RelayCommand(OpenFile_Execute_DS2_3);
            this.Export_AllFiles_Command = new RelayCommand(Export_AllFiles_Execute);
            this.Import_Files_Command = new RelayCommand(Import_Files_Execute);
            System.IO.Directory.CreateDirectory(@"C:\programdata\DSExtRepkr");

            Files = new ObservableCollection<Models.DS1_FileInfo>();
            SelectedFiles = new ObservableCollection<Models.DS1_FileInfo>();
            HeaderBytes = new byte[16];

         
        }


        #endregion

        #region Public/Private Methods

        private void OnHeaderBytesChanged()
        {
            FileID = HeaderBytes.Take(8).Skip(0).ToArray();
            FileNameDataLength = BitConverter.ToInt32(HeaderBytes.Skip(8).Take(4).ToArray(), 0);
            NumArchiveFiles = BitConverter.ToInt32(HeaderBytes.Skip(12).Take(4).ToArray(), 0);

            //start of file data streams must align to 8 byte chunk
            int calculate_modulus = (16 + HashLength + FileNameDataLength + (36 * NumArchiveFiles)) % 8;
            if (!(calculate_modulus == 0))
            {
                FileStreamBegin_BufferSize = 8 - calculate_modulus;
            }
            else {
                FileStreamBegin_BufferSize = 0;
            }
        }


        private void CalculatePercentCompleted()
        {
            PercentComplete = ((decimal)TasksCompleted / (decimal)TasksToComplete) * 100;
        }

        private void SetOpenFileName(string fn)
        {
            CurrentOpenFileName = fn.Substring(fn.LastIndexOf(@"\") + 1, fn.Length - fn.LastIndexOf(@"\") - 1);
        }

        private async void Import_Files_Execute()
        {
            
            //store the directory containing the injection files in variable fbd
            var fbd = new FolderBrowserDialog();
            fbd.Description = "Select the folder containing inject Files";
            if (!(fbd.ShowDialog() == DialogResult.OK))
            {
                    return;
            }
            //set the directory where the repack files are located
            RepackFileDirectory = fbd.SelectedPath;

            //store the directory where the new gg archive will be created in variable exportfbd
            var exportfbd = new FolderBrowserDialog();
            exportfbd.Description = "Select the location to create a new .gg archive";
            if (!(exportfbd.ShowDialog() == DialogResult.OK))
            {
                    return;
            }
            await Task.Run(() => {
                //set the directory where the repack files are located
                ArchiveExportPath = exportfbd.SelectedPath;

            //define the length of data that should never change. This data will simply be copied to the new archive
            //int bytelimit = 16 + SelectedGame.HashLength + FileNameDataLength;
            int bytelimit = 16 + HashLength + FileNameDataLength;
            //write the new bytes that will never change into the new archive
            WriteUnchangedGGBytes($"{ArchiveExportPath}\\{CurrentOpenFileName.Substring(0, CurrentOpenFileName.Length - 3)}-New.gg", 4096, bytelimit);


            //get the file paths for the files the user wants to inject
            Models.InjectFileData[] importfiles = GetImportFiles(fbd);

            //combine file append stream file and delete 
            int streamlength = FileStreamBegin_BufferSize;
            //int mismatchsize = 0;
            //foreach (Models.DS1_FileInfo dsfile in Files)
            //{
            PercentComplete = 0;
            for(int i = 0; i < Files.Count; i++)
            {
                Models.DS1_FileInfo dsfile = Files[i];
                /* used for testing file size of problematic compression. Fixed with external dll
                if (dsfile.Id == 363)
                {
                     int test = 1;
                }
                */
                //check if the currently archive file is contained this list of files being imported
                if (importfiles.Any(j => j.CorrectedRelativePath == dsfile.FullFilePathName))
                {
                    
                    //get the importing file that matches the current dsfile
                    var importfilepath = importfiles.Where(k => k.CorrectedRelativePath == dsfile.FullFilePathName).FirstOrDefault();


                    //List<byte> encryptedfilebytes = EncryptBytes(File.ReadAllBytes(importfilepath.FullFilePath));
                    //zeropad the filestream
                    byte[] encryptedfilebytes;
                    byte[] compressedbytes;

                    //check if the currently importing file should be compressed to match the file in the archive it is replaceing
                    if (!(dsfile.IsCompressed == 1))
                    {
                        //compress the filestream data for the currently importing file
                        //var compressor = new wrapper.Compression();
                        //var compressor = new DeathspankcZlib.Zlib114();

                            //TODO: this doesn't seem to work for tov 00201
                        //encryptedfilebytes = compressor.Switch(File.ReadAllBytes(importfilepath.FullFilePath));
                        encryptedfilebytes = EncryptBytes(File.ReadAllBytes(importfilepath.FullFilePath)).ToArray();
                        compressedbytes = encryptedfilebytes;
                    }
                    else
                    {
                        //compress the filestream data for the currently importing file
                        //compressedbytes = CompressFile(encryptedfilebytes.ToArray());
                        //var compressor = new wrapper.Compression();
                        var compressor = new DeathspankcZlib.Zlib114();
                        encryptedfilebytes = compressor.Switch(File.ReadAllBytes(importfilepath.FullFilePath));
                        compressedbytes = compressor.Zlib114_Compress(encryptedfilebytes.ToArray());
                    }
                    /*
                    if (compressedbytes.Length != dsfile.CompressedSize)
                    {
                        mismatchsize++;
                    }
                    */
                        //get the number of zeros needed to be placed onto the end of the file stream. 
                        //All streams should appear in blocks of 64 bytes.
                        int paddingzeros = GetZerosPaddingStream(compressedbytes.Length);
                        
                        //add the compressedbytes to a new list so it can easily be padded with zeros at the end
                        List<byte> writebytes = new List<byte>();
                        writebytes.AddRange(compressedbytes);

                        //if the stream needs to be padded with zeros
                        //add a byte array full of zeros in the amount necessary
                        if (paddingzeros != 0)
                        {
                            writebytes.AddRange(new byte[paddingzeros]);
                        }

                        //write the filestream bytes to the temporary stream file
                        //this file will be appended to the end of the file name and attributes data
                        //to make a complete Archive
                        using (FileStream streamwriter = new FileStream(@"C:\programdata\DSExtRepkr\Streams.file", FileMode.OpenOrCreate, FileAccess.Write))
                        {

                            streamwriter.Seek(0, SeekOrigin.End);
                            BinaryWriter bw = new BinaryWriter(streamwriter);
                            bw.Write(writebytes.ToArray());
                            bw.Flush();
                            bw.Close();


                        }
                    
                    //write updated attribute bytes to extractfile
                    using (FileStream streamwriter = File.OpenWrite($"{ArchiveExportPath}\\{CurrentOpenFileName.Substring(0, CurrentOpenFileName.Length - 3)}-New.gg"))
                        {
                            streamwriter.Seek(0, SeekOrigin.End);
                            BinaryWriter bw = new BinaryWriter(streamwriter);
                            bw.Write(dsfile.Attr1_FilepathOffset);
                            bw.Write(dsfile.Attr2_FilenameOffset);
                            bw.Write(dsfile.Attr3_ItemID);
                            bw.Write(dsfile.Attr4);
                            bw.Write(dsfile.Attr5);
                            bw.Write(BitConverter.GetBytes(encryptedfilebytes.Length));
                            bw.Write(BitConverter.GetBytes(compressedbytes.Length));
                            bw.Write(dsfile.Attr8_IsCompressed);
                            bw.Write(BitConverter.GetBytes(16 + HashLength + FileNameDataLength + (36 * Files.Count) + streamlength));
                            bw.Flush();
                            bw.Close();
                        }

                    //update the value of stream length to reflect the number of bytes written to the temporary stream file
                    //this updated value should account for a larger or smaller file size than the original gg archive file
                    //if it has been modified
                    streamlength += writebytes.Count;



                }
                else //copy the original file values
                {
                    //copy stream from gg file to temp stream file
                    AppendOriginalArchiveStream(@"C:\programdata\DSExtRepkr\Streams.file", BitConverter.ToInt32(dsfile.Attr9_FileOffset,0) ,4096, BitConverter.ToInt32(dsfile.Attr7_CompressedSize,0));




                    //copy attributes from dsfile object to new gg file

                    using (FileStream streamwriter = File.OpenWrite($"{ArchiveExportPath}\\{CurrentOpenFileName.Substring(0, CurrentOpenFileName.Length - 3)}-New.gg"))
                    {
                        streamwriter.Seek(0, SeekOrigin.End);
                        BinaryWriter bw = new BinaryWriter(streamwriter);
                        bw.Write(dsfile.Attr1_FilepathOffset);
                        bw.Write(dsfile.Attr2_FilenameOffset);
                        bw.Write(dsfile.Attr3_ItemID);
                        bw.Write(dsfile.Attr4);
                        bw.Write(dsfile.Attr5);
                        //bw.Write(BitConverter.GetBytes(encryptedfilebytes.Count));
                        //bw.Write(BitConverter.GetBytes(compressedbytes.Length));
                        bw.Write(dsfile.Attr6_UncompressedSize);
                        bw.Write(dsfile.Attr7_CompressedSize);
                        bw.Write(dsfile.Attr8_IsCompressed);
                        bw.Write(BitConverter.GetBytes(16 + HashLength + FileNameDataLength + (36 * Files.Count) + streamlength));
                        //bw.Write(dsfile.Attr9_FileOffset);
                        bw.Flush();
                        bw.Close();
                    }




                    //advance the slream length counter by the stream length plus the length of the zeros padding the stream
                    streamlength += (BitConverter.ToInt32(dsfile.Attr7_CompressedSize,0) +  
                                    GetZerosPaddingStream(BitConverter.ToInt32(dsfile.Attr7_CompressedSize, 0)));
                }


                percentcomplete = (decimal)i / Files.Count;
            }
            //pad the file with "0" bytes to get the total length to be a multiple of 8
            //before the start of the data streams
            FileInfo fileInfo = new FileInfo($"{ArchiveExportPath}\\{CurrentOpenFileName.Substring(0, CurrentOpenFileName.Length - 3)}-New.gg");
            long size = fileInfo.Length;
            int remainder = (int)(size % 8);
            if (!(remainder == 0))
            {
                int zeros = 8 - remainder;
                PadFIleWithZeros($"{ArchiveExportPath}\\{CurrentOpenFileName.Substring(0, CurrentOpenFileName.Length - 3)}-New.gg", zeros);
            }

            //write the filestream temporary file to the end of the new archive file
            AppendFileWithFilestream(@"C:\programdata\DSExtRepkr\Streams.file", $"{ArchiveExportPath}\\{CurrentOpenFileName.Substring(0, CurrentOpenFileName.Length - 3)}-New.gg");

            //cleanup the filestream temporary file
            if (File.Exists(@"C:\programdata\DSExtRepkr\Streams.file"))
            {
                File.Delete(@"C:\programdata\DSExtRepkr\Streams.file");
            }
            PercentComplete = 0;
            });

        }

        private void WriteUnchangedGGBytes(string outputfile, int defaultbuffer, int bytelimit)
        {
            using (FileStream fs = File.Open(CurrentOpenFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream writeStream = File.OpenWrite($"{ArchiveExportPath}\\{CurrentOpenFileName.Substring(0, CurrentOpenFileName.Length - 3)}-New.gg"))
            {
                BinaryReader reader = new BinaryReader(fs);
                BinaryWriter writer = new BinaryWriter(writeStream);


                byte[] buffer = new byte[defaultbuffer];
                int bytesRead;

                // while the read method returns bytes
                // keep writing them to the output stream
                // stop writing when you get to the beginning of the attribute table for files
                while ((bytesRead = fs.Read(buffer, 0, defaultbuffer)) > 0 && fs.Position <= bytelimit)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                }
                if (writeStream.Length < bytelimit)
                {
                    //purge write buffer to get up to total required number of bytes
                    writeStream.Write(buffer, 0, (int)(bytelimit - writeStream.Length));

                }

            }
        }

        private void AppendOriginalArchiveStream(string outputfile,int startingoffset, int defaultbuffer, int bytelimit)
        {
            
            using (FileStream fs = File.Open(CurrentOpenFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream writeStream = File.OpenWrite(outputfile))
            using (BinaryReader br = new BinaryReader(fs))
            {
                
                br.BaseStream.Position = startingoffset;
                byte[] writebytes = br.ReadBytes(bytelimit);
                writeStream.Seek(0, SeekOrigin.End);
                int paddingzeros = GetZerosPaddingStream(writebytes.Length);

                //add the compressedbytes to a new list so it can easily be padded with zeros at the end
                List<byte> lwritebytes = new List<byte>();
                lwritebytes.AddRange(writebytes);

                //if the stream needs to be padded with zeros
                //add a byte array full of zeros in the amount necessary
                if (paddingzeros != 0)
                {
                    lwritebytes.AddRange(new byte[paddingzeros]);
                }
                writeStream.Write(lwritebytes.ToArray(),0,lwritebytes.Count);
            }
            


        }

        /// <summary>
        /// Writes the specified number of zeros to the end of the file at the given filepath
        /// </summary>
        /// <param name="filepath"> The full file path of the file that needs to be padded</param>
        /// <param name="numzeros"> The number of zeros to pad the file with</param>
        private void PadFIleWithZeros(string filepath, int numzeros)
        {

            using (FileStream zerowriter = File.OpenWrite(filepath))
            {
                zerowriter.Seek(0, SeekOrigin.End);
                byte[] zerobytearray = new byte[numzeros];
                BinaryWriter bw = new BinaryWriter(zerowriter);
                bw.Write(zerobytearray);

            }
        }

        private void AppendFileWithFilestream(string sourcefile, string destfile)
        {
            using (FileStream sourcestream = File.Open(sourcefile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream deststream = File.OpenWrite(destfile))
            {
                BinaryReader reader = new BinaryReader(sourcestream);
                BinaryWriter writer = new BinaryWriter(deststream);

                // create a buffer to hold the bytes 
                var defaultbuffer = 1024;
                byte[] buffer = new byte[defaultbuffer];
                int bytesRead;

                deststream.Seek(0, SeekOrigin.End);
                // while the read method returns bytes
                // keep writing them to the output stream
                while ((bytesRead = sourcestream.Read(buffer, 0, defaultbuffer)) > 0)
                {
                    deststream.Write(buffer, 0, bytesRead);
                }


            }
        }

        private int GetZerosPaddingStream(int length)
        {
            int output = 0;
            for (int numzeros = 15; numzeros >= 0; numzeros--)
            {
                if ((length + numzeros) % 16 == 0)
                {
                    output = numzeros;
                    break;
                }
            }
            return output;
        }

        private Models.InjectFileData[] GetImportFiles(FolderBrowserDialog fbd)
        {
            List<Models.InjectFileData> files = new List<Models.InjectFileData>();
            string[] foundfiles = Directory.GetFiles(fbd.SelectedPath, "*", SearchOption.AllDirectories);

            foreach (string file in foundfiles)
            {
                string trimmedpath = file.Substring(fbd.SelectedPath.Length + 1, file.Length - fbd.SelectedPath.Length - 1);
                string correctedpath;
                if (trimmedpath.ToLower().StartsWith("dlc"))
                {
                    correctedpath = trimmedpath.Replace("dlc", 
                            "c:\\hudson\\jobs\\DeathSpank2_PC_Release\\workspace\\DeathSpank\\ThongsOfVirtue\\DLC\\DLCPack2"
                        );
                }
                else
                {
                    correctedpath = trimmedpath;
                }

                if (Files.Any(f => f.FullFilePathName == correctedpath))
                {
                    Models.InjectFileData ifile = new Models.InjectFileData()
                    {
                        RelativePath = trimmedpath,
                        FullFilePath = file,
                        CorrectedRelativePath = correctedpath
                    };
                    files.Add(ifile);
                }
            }

            return files.ToArray();
        }

        private async void Export_AllFiles_Execute()
        {
            TasksToComplete = NumArchiveFiles;
            TasksCompleted = 0;

            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                //set the global export folder
                ExportFilePath = fbd.SelectedPath;

                for (int i = 0; i < Files.Count; i++)
                {

                    //await Task.Run(() => ExportSingleFile(Files[i]));
                    ExportSingleFile(Files[i]);
                    TasksCompleted++;
                    ProgressText = $"File {TasksCompleted}/{TasksToComplete}";

                }
            }

            TasksCompleted = 0;
        }

        public void ExportSingleFile(Models.DS1_FileInfo file)
        {

            byte[] decompressed = GetFileStreamFromGGArchive(   CurrentOpenFileFullName, 
                                                                file.CompressedSize, 
                                                                file.FileOffset, 
                                                                file.IsCompressed, 
                                                                4096);

            byte[] decrypted = DecryptFile(decompressed);

            //convert c:\ to c~\ to allow windows file system exports for DS DLC files
            string path;
            if (file.WinFilePath.ToLower().StartsWith("c:\\") )
            {
                //path = file.WinFilePath.Replace("c:\\","c~\\");
                path = file.WinFilePath.Replace("c:\\hudson\\jobs\\DeathSpank2_PC_Release\\workspace\\DeathSpank\\ThongsOfVirtue\\DLC\\DLCPack2",
                                                "dlc");
            }
            else
            {
                path = file.WinFilePath;
            }

            //must be an error here
            //Make sure the export directory exists
            Directory.CreateDirectory($@"{ExportFilePath}\{path}");

            
            using (FileStream fs = new FileStream($@"{ExportFilePath}\{path}{file.FileName}", 
                                                  FileMode.OpenOrCreate, 
                                                  FileAccess.ReadWrite)) 
            {
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(decrypted);
                bw.Flush();
                bw.Close();

            }
            
        }

        private byte[] GetFileStreamFromGGArchive(string currentOpenFileFullName, int compressedSize, int offset, int iscompressed, int buffersize)
        {
            using (FileStream fs = File.Open(currentOpenFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (MemoryStream ws = new MemoryStream())
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    byte[] streambuffer = new byte[buffersize];
                    int bytesRead;

                    while ((bytesRead = fs.Read(streambuffer, 0, streambuffer.Length)) > 0 && fs.Position <= (offset + compressedSize))
                    {
                        ws.Write(streambuffer, 0, bytesRead);
                    }
                    if (ws.Length < compressedSize)
                    {
                        ws.Write(streambuffer, 0, (int)(compressedSize - ws.Length));

                    }
                    if (iscompressed == 1)
                    {
                        return DecompressFile(ws.ToArray());
                    }
                    else
                    {
                        return ws.ToArray();
                    }
                }

            }
            //no code should ever get here
            return new byte[0];
        }

        private async void OpenFile_Execute_DS1()
        {

            //create a new open file dialog and filter it to only show datadict files
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            //openFileDialog.Filter = "DeathSpank Data Files (*.datadict)|*.datadict|DeathSpank TextDict Files(*.textdict)|*.textdict|All files (*.*)|*.*";
            openFileDialog.Filter = "DeathSpank Data Files (*.GG)|*.GG|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                //byte[] PreOpenFormatCheck = File.ReadAllBytes(openFileDialog.FileName);
                //bool IsDeathspankDictFormat = DeathspankFormatCheck(PreOpenFormatCheck.Skip(0).Take(4).ToArray());

                /*
                if (IsDeathspankDictFormat == true)
                {
                    //store the file as a byte array
                    OpenFileBytes = File.ReadAllBytes(openFileDialog.FileName);
                }
                else
                {
                    WrongFileFormat();
                    return;
                }
                */

                //store the full file path of the file
                CurrentOpenFileFullName = openFileDialog.FileName;
                //get and store the filename portion of the full file path
                int index = openFileDialog.FileName.LastIndexOf('\\') + 1;
                CurrentOpenFileName = openFileDialog.FileName.Substring(index, openFileDialog.FileName.Length - index);

            }
            else
            {
                return;
            }
            Files.Clear();
            HashLength = 800;
            await Task.Run(()=> ParseGGArchiveNew());

        }

        private async void OpenFile_Execute_DS2_3()
        {

            //create a new open file dialog and filter it to only show datadict files
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            //openFileDialog.Filter = "DeathSpank Data Files (*.datadict)|*.datadict|DeathSpank TextDict Files(*.textdict)|*.textdict|All files (*.*)|*.*";
            openFileDialog.Filter = "DeathSpank Data Files (*.GG)|*.GG|All files (*.*)|*.*";


            if (openFileDialog.ShowDialog() == true)
            {
                //byte[] PreOpenFormatCheck = File.ReadAllBytes(openFileDialog.FileName);
                //bool IsDeathspankDictFormat = DeathspankFormatCheck(PreOpenFormatCheck.Skip(0).Take(4).ToArray());

                /*
                if (IsDeathspankDictFormat == true)
                {
                    //store the file as a byte array
                    OpenFileBytes = File.ReadAllBytes(openFileDialog.FileName);
                }
                else
                {
                    WrongFileFormat();
                    return;
                }
                */

                //store the full file path of the file
                CurrentOpenFileFullName = openFileDialog.FileName;
                //get and store the filename portion of the full file path
                int index = openFileDialog.FileName.LastIndexOf('\\') + 1;
                CurrentOpenFileName = openFileDialog.FileName.Substring(index, openFileDialog.FileName.Length - index);

            }
            else
            {
                return;
            }
            Files.Clear();
            HashLength = 960;
            await Task.Run(() => ParseGGArchiveNew());

        }

        private async void Export_SingleFile_Execute()
        {
            //set a new FileinfoObject to hold the data of the currently selected file
            //the selected file could change during execution so this will be a snapshot
            //of the selected file
            ExportFile = SelectedFileInfo;

            TasksToComplete = 1;
            TasksCompleted = 0;

            var fbd = new FolderBrowserDialog();

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                //set the global export folder
                ExportFilePath = fbd.SelectedPath;

                ProgressText = $"File {TasksCompleted}/{TasksToComplete}";
                //await Task.Run(() => ExportSingleFile(ExportFile));
                ExportSingleFile(ExportFile);
                TasksCompleted++;
                ProgressText = $"File {TasksCompleted}/{TasksToComplete}";

            }

        }


        static byte[] DecryptFile(byte[] data)
        {
            List<byte> returnbytes = new List<byte>();
            foreach (byte mByte in data)
            {
                returnbytes.Add(Decrypt_ByteSwap(mByte));
            }
            return returnbytes.ToArray();

        }

        static List<byte> EncryptBytes(byte[] data)
        {
            List<byte> returnbytes = new List<byte>();
            foreach (byte mByte in data)
            {
                returnbytes.Add(Encrypt_ByteSwap(mByte));
            }
            return returnbytes;

        }

        static byte Encrypt_ByteSwap(byte inputbyte)
        {
            byte returnbyte;

            switch (inputbyte)
            {


                case 0:
                    returnbyte = 92;
                    break;
                case 1:
                    returnbyte = 248;
                    break;
                case 2:
                    returnbyte = 153;
                    break;
                case 3:
                    returnbyte = 38;
                    break;
                case 4:
                    returnbyte = 185;
                    break;
                case 5:
                    returnbyte = 209;
                    break;
                case 6:
                    returnbyte = 177;
                    break;
                case 7:
                    returnbyte = 183;
                    break;
                case 8:
                    returnbyte = 83;
                    break;
                case 9:
                    returnbyte = 49;
                    break;
                case 10:
                    returnbyte = 159;
                    break;
                case 11:
                    returnbyte = 44;
                    break;
                case 12:
                    returnbyte = 6;
                    break;
                case 13:
                    returnbyte = 104;
                    break;
                case 14:
                    returnbyte = 127;
                    break;
                case 15:
                    returnbyte = 105;
                    break;
                case 16:
                    returnbyte = 166;
                    break;
                case 17:
                    returnbyte = 251;
                    break;
                case 18:
                    returnbyte = 19;
                    break;
                case 19:
                    returnbyte = 94;
                    break;
                case 20:
                    returnbyte = 41;
                    break;
                case 21:
                    returnbyte = 11;
                    break;
                case 22:
                    returnbyte = 218;
                    break;
                case 23:
                    returnbyte = 28;
                    break;
                case 24:
                    returnbyte = 37;
                    break;
                case 25:
                    returnbyte = 57;
                    break;
                case 26:
                    returnbyte = 13;
                    break;
                case 27:
                    returnbyte = 226;
                    break;
                case 28:
                    returnbyte = 253;
                    break;
                case 29:
                    returnbyte = 151;
                    break;
                case 30:
                    returnbyte = 176;
                    break;
                case 31:
                    returnbyte = 213;
                    break;
                case 32:
                    returnbyte = 201;
                    break;
                case 33:
                    returnbyte = 175;
                    break;
                case 34:
                    returnbyte = 52;
                    break;
                case 35:
                    returnbyte = 110;
                    break;
                case 36:
                    returnbyte = 43;
                    break;
                case 37:
                    returnbyte = 239;
                    break;
                case 38:
                    returnbyte = 238;
                    break;
                case 39:
                    returnbyte = 32;
                    break;
                case 40:
                    returnbyte = 75;
                    break;
                case 41:
                    returnbyte = 174;
                    break;
                case 42:
                    returnbyte = 118;
                    break;
                case 43:
                    returnbyte = 46;
                    break;
                case 44:
                    returnbyte = 216;
                    break;
                case 45:
                    returnbyte = 164;
                    break;
                case 46:
                    returnbyte = 26;
                    break;
                case 47:
                    returnbyte = 171;
                    break;
                case 48:
                    returnbyte = 100;
                    break;
                case 49:
                    returnbyte = 252;
                    break;
                case 50:
                    returnbyte = 79;
                    break;
                case 51:
                    returnbyte = 135;
                    break;
                case 52:
                    returnbyte = 47;
                    break;
                case 53:
                    returnbyte = 169;
                    break;
                case 54:
                    returnbyte = 101;
                    break;
                case 55:
                    returnbyte = 158;
                    break;
                case 56:
                    returnbyte = 172;
                    break;
                case 57:
                    returnbyte = 129;
                    break;
                case 58:
                    returnbyte = 235;
                    break;
                case 59:
                    returnbyte = 103;
                    break;
                case 60:
                    returnbyte = 80;
                    break;
                case 61:
                    returnbyte = 139;
                    break;
                case 62:
                    returnbyte = 154;
                    break;
                case 63:
                    returnbyte = 145;
                    break;
                case 64:
                    returnbyte = 231;
                    break;
                case 65:
                    returnbyte = 10;
                    break;
                case 66:
                    returnbyte = 125;
                    break;
                case 67:
                    returnbyte = 120;
                    break;
                case 68:
                    returnbyte = 146;
                    break;
                case 69:
                    returnbyte = 138;
                    break;
                case 70:
                    returnbyte = 150;
                    break;
                case 71:
                    returnbyte = 196;
                    break;
                case 72:
                    returnbyte = 233;
                    break;
                case 73:
                    returnbyte = 225;
                    break;
                case 74:
                    returnbyte = 243;
                    break;
                case 75:
                    returnbyte = 203;
                    break;
                case 76:
                    returnbyte = 249;
                    break;
                case 77:
                    returnbyte = 237;
                    break;
                case 78:
                    returnbyte = 62;
                    break;
                case 79:
                    returnbyte = 119;
                    break;
                case 80:
                    returnbyte = 0;
                    break;
                case 81:
                    returnbyte = 197;
                    break;
                case 82:
                    returnbyte = 199;
                    break;
                case 83:
                    returnbyte = 97;
                    break;
                case 84:
                    returnbyte = 141;
                    break;
                case 85:
                    returnbyte = 250;
                    break;
                case 86:
                    returnbyte = 192;
                    break;
                case 87:
                    returnbyte = 59;
                    break;
                case 88:
                    returnbyte = 137;
                    break;
                case 89:
                    returnbyte = 242;
                    break;
                case 90:
                    returnbyte = 244;
                    break;
                case 91:
                    returnbyte = 81;
                    break;
                case 92:
                    returnbyte = 246;
                    break;
                case 93:
                    returnbyte = 108;
                    break;
                case 94:
                    returnbyte = 236;
                    break;
                case 95:
                    returnbyte = 17;
                    break;
                case 96:
                    returnbyte = 16;
                    break;
                case 97:
                    returnbyte = 178;
                    break;
                case 98:
                    returnbyte = 116;
                    break;
                case 99:
                    returnbyte = 56;
                    break;
                case 100:
                    returnbyte = 173;
                    break;
                case 101:
                    returnbyte = 90;
                    break;
                case 102:
                    returnbyte = 134;
                    break;
                case 103:
                    returnbyte = 228;
                    break;
                case 104:
                    returnbyte = 66;
                    break;
                case 105:
                    returnbyte = 89;
                    break;
                case 106:
                    returnbyte = 73;
                    break;
                case 107:
                    returnbyte = 96;
                    break;
                case 108:
                    returnbyte = 107;
                    break;
                case 109:
                    returnbyte = 255;
                    break;
                case 110:
                    returnbyte = 200;
                    break;
                case 111:
                    returnbyte = 48;
                    break;
                case 112:
                    returnbyte = 144;
                    break;
                case 113:
                    returnbyte = 7;
                    break;
                case 114:
                    returnbyte = 40;
                    break;
                case 115:
                    returnbyte = 221;
                    break;
                case 116:
                    returnbyte = 87;
                    break;
                case 117:
                    returnbyte = 99;
                    break;
                case 118:
                    returnbyte = 91;
                    break;
                case 119:
                    returnbyte = 133;
                    break;
                case 120:
                    returnbyte = 117;
                    break;
                case 121:
                    returnbyte = 70;
                    break;
                case 122:
                    returnbyte = 4;
                    break;
                case 123:
                    returnbyte = 114;
                    break;
                case 124:
                    returnbyte = 147;
                    break;
                case 125:
                    returnbyte = 140;
                    break;
                case 126:
                    returnbyte = 14;
                    break;
                case 127:
                    returnbyte = 22;
                    break;
                case 128:
                    returnbyte = 72;
                    break;
                case 129:
                    returnbyte = 31;
                    break;
                case 130:
                    returnbyte = 180;
                    break;
                case 131:
                    returnbyte = 254;
                    break;
                case 132:
                    returnbyte = 60;
                    break;
                case 133:
                    returnbyte = 78;
                    break;
                case 134:
                    returnbyte = 128;
                    break;
                case 135:
                    returnbyte = 241;
                    break;
                case 136:
                    returnbyte = 5;
                    break;
                case 137:
                    returnbyte = 65;
                    break;
                case 138:
                    returnbyte = 112;
                    break;
                case 139:
                    returnbyte = 186;
                    break;
                case 140:
                    returnbyte = 136;
                    break;
                case 141:
                    returnbyte = 211;
                    break;
                case 142:
                    returnbyte = 3;
                    break;
                case 143:
                    returnbyte = 98;
                    break;
                case 144:
                    returnbyte = 93;
                    break;
                case 145:
                    returnbyte = 219;
                    break;
                case 146:
                    returnbyte = 12;
                    break;
                case 147:
                    returnbyte = 39;
                    break;
                case 148:
                    returnbyte = 33;
                    break;
                case 149:
                    returnbyte = 113;
                    break;
                case 150:
                    returnbyte = 109;
                    break;
                case 151:
                    returnbyte = 84;
                    break;
                case 152:
                    returnbyte = 152;
                    break;
                case 153:
                    returnbyte = 18;
                    break;
                case 154:
                    returnbyte = 50;
                    break;
                case 155:
                    returnbyte = 160;
                    break;
                case 156:
                    returnbyte = 234;
                    break;
                case 157:
                    returnbyte = 148;
                    break;
                case 158:
                    returnbyte = 64;
                    break;
                case 159:
                    returnbyte = 130;
                    break;
                case 160:
                    returnbyte = 165;
                    break;
                case 161:
                    returnbyte = 69;
                    break;
                case 162:
                    returnbyte = 214;
                    break;
                case 163:
                    returnbyte = 207;
                    break;
                case 164:
                    returnbyte = 132;
                    break;
                case 165:
                    returnbyte = 76;
                    break;
                case 166:
                    returnbyte = 71;
                    break;
                case 167:
                    returnbyte = 232;
                    break;
                case 168:
                    returnbyte = 74;
                    break;
                case 169:
                    returnbyte = 168;
                    break;
                case 170:
                    returnbyte = 202;
                    break;
                case 171:
                    returnbyte = 220;
                    break;
                case 172:
                    returnbyte = 230;
                    break;
                case 173:
                    returnbyte = 167;
                    break;
                case 174:
                    returnbyte = 240;
                    break;
                case 175:
                    returnbyte = 95;
                    break;
                case 176:
                    returnbyte = 53;
                    break;
                case 177:
                    returnbyte = 35;
                    break;
                case 178:
                    returnbyte = 193;
                    break;
                case 179:
                    returnbyte = 212;
                    break;
                case 180:
                    returnbyte = 61;
                    break;
                case 181:
                    returnbyte = 184;
                    break;
                case 182:
                    returnbyte = 36;
                    break;
                case 183:
                    returnbyte = 194;
                    break;
                case 184:
                    returnbyte = 115;
                    break;
                case 185:
                    returnbyte = 195;
                    break;
                case 186:
                    returnbyte = 149;
                    break;
                case 187:
                    returnbyte = 27;
                    break;
                case 188:
                    returnbyte = 206;
                    break;
                case 189:
                    returnbyte = 204;
                    break;
                case 190:
                    returnbyte = 55;
                    break;
                case 191:
                    returnbyte = 179;
                    break;
                case 192:
                    returnbyte = 122;
                    break;
                case 193:
                    returnbyte = 189;
                    break;
                case 194:
                    returnbyte = 45;
                    break;
                case 195:
                    returnbyte = 51;
                    break;
                case 196:
                    returnbyte = 23;
                    break;
                case 197:
                    returnbyte = 181;
                    break;
                case 198:
                    returnbyte = 42;
                    break;
                case 199:
                    returnbyte = 215;
                    break;
                case 200:
                    returnbyte = 157;
                    break;
                case 201:
                    returnbyte = 161;
                    break;
                case 202:
                    returnbyte = 190;
                    break;
                case 203:
                    returnbyte = 121;
                    break;
                case 204:
                    returnbyte = 227;
                    break;
                case 205:
                    returnbyte = 21;
                    break;
                case 206:
                    returnbyte = 124;
                    break;
                case 207:
                    returnbyte = 162;
                    break;
                case 208:
                    returnbyte = 30;
                    break;
                case 209:
                    returnbyte = 210;
                    break;
                case 210:
                    returnbyte = 182;
                    break;
                case 211:
                    returnbyte = 187;
                    break;
                case 212:
                    returnbyte = 67;
                    break;
                case 213:
                    returnbyte = 86;
                    break;
                case 214:
                    returnbyte = 77;
                    break;
                case 215:
                    returnbyte = 8;
                    break;
                case 216:
                    returnbyte = 85;
                    break;
                case 217:
                    returnbyte = 9;
                    break;
                case 218:
                    returnbyte = 208;
                    break;
                case 219:
                    returnbyte = 229;
                    break;
                case 220:
                    returnbyte = 1;
                    break;
                case 221:
                    returnbyte = 222;
                    break;
                case 222:
                    returnbyte = 68;
                    break;
                case 223:
                    returnbyte = 205;
                    break;
                case 224:
                    returnbyte = 142;
                    break;
                case 225:
                    returnbyte = 82;
                    break;
                case 226:
                    returnbyte = 106;
                    break;
                case 227:
                    returnbyte = 15;
                    break;
                case 228:
                    returnbyte = 24;
                    break;
                case 229:
                    returnbyte = 63;
                    break;
                case 230:
                    returnbyte = 54;
                    break;
                case 231:
                    returnbyte = 111;
                    break;
                case 232:
                    returnbyte = 217;
                    break;
                case 233:
                    returnbyte = 29;
                    break;
                case 234:
                    returnbyte = 223;
                    break;
                case 235:
                    returnbyte = 188;
                    break;
                case 236:
                    returnbyte = 131;
                    break;
                case 237:
                    returnbyte = 198;
                    break;
                case 238:
                    returnbyte = 88;
                    break;
                case 239:
                    returnbyte = 34;
                    break;
                case 240:
                    returnbyte = 224;
                    break;
                case 241:
                    returnbyte = 25;
                    break;
                case 242:
                    returnbyte = 126;
                    break;
                case 243:
                    returnbyte = 170;
                    break;
                case 244:
                    returnbyte = 163;
                    break;
                case 245:
                    returnbyte = 191;
                    break;
                case 246:
                    returnbyte = 247;
                    break;
                case 247:
                    returnbyte = 102;
                    break;
                case 248:
                    returnbyte = 58;
                    break;
                case 249:
                    returnbyte = 156;
                    break;
                case 250:
                    returnbyte = 20;
                    break;
                case 251:
                    returnbyte = 155;
                    break;
                case 252:
                    returnbyte = 245;
                    break;
                case 253:
                    returnbyte = 2;
                    break;
                case 254:
                    returnbyte = 143;
                    break;
                case 255:
                    returnbyte = 123;
                    break;
                default:
                    returnbyte = inputbyte;
                    break;

            }
            return returnbyte;
        }


        static byte Decrypt_ByteSwap(byte inputbyte)
        {
            byte returnbyte;

            switch (inputbyte)
            {
                case 92:
                    returnbyte = 0;
                    break;
                case 248:
                    returnbyte = 1;
                    break;
                case 153:
                    returnbyte = 2;
                    break;
                case 38:
                    returnbyte = 3;
                    break;
                case 185:
                    returnbyte = 4;
                    break;
                case 209:
                    returnbyte = 5;
                    break;
                case 177:
                    returnbyte = 6;
                    break;
                case 183:
                    returnbyte = 7;
                    break;
                case 83:
                    returnbyte = 8;
                    break;
                case 49:
                    returnbyte = 9;
                    break;
                case 159:
                    returnbyte = 10;
                    break;
                case 44:
                    returnbyte = 11;
                    break;
                case 6:
                    returnbyte = 12;
                    break;
                case 104:
                    returnbyte = 13;
                    break;
                case 127:
                    returnbyte = 14;
                    break;
                case 105:
                    returnbyte = 15;
                    break;
                case 166:
                    returnbyte = 16;
                    break;
                case 251:
                    returnbyte = 17;
                    break;
                case 19:
                    returnbyte = 18;
                    break;
                case 94:
                    returnbyte = 19;
                    break;
                case 41:
                    returnbyte = 20;
                    break;
                case 11:
                    returnbyte = 21;
                    break;
                case 218:
                    returnbyte = 22;
                    break;
                case 28:
                    returnbyte = 23;
                    break;
                case 37:
                    returnbyte = 24;
                    break;
                case 57:
                    returnbyte = 25;
                    break;
                case 13:
                    returnbyte = 26;
                    break;
                case 226:
                    returnbyte = 27;
                    break;
                case 253:
                    returnbyte = 28;
                    break;
                case 151:
                    returnbyte = 29;
                    break;
                case 176:
                    returnbyte = 30;
                    break;
                case 213:
                    returnbyte = 31;
                    break;
                case 201:
                    returnbyte = 32;
                    break;
                case 175:
                    returnbyte = 33;
                    break;
                case 52:
                    returnbyte = 34;
                    break;
                case 110:
                    returnbyte = 35;
                    break;
                case 43:
                    returnbyte = 36;
                    break;
                case 239:
                    returnbyte = 37;
                    break;
                case 238:
                    returnbyte = 38;
                    break;
                case 32:
                    returnbyte = 39;
                    break;
                case 75:
                    returnbyte = 40;
                    break;
                case 174:
                    returnbyte = 41;
                    break;
                case 118:
                    returnbyte = 42;
                    break;
                case 46:
                    returnbyte = 43;
                    break;
                case 216:
                    returnbyte = 44;
                    break;
                case 164:
                    returnbyte = 45;
                    break;
                case 26:
                    returnbyte = 46;
                    break;
                case 171:
                    returnbyte = 47;
                    break;
                case 100:
                    returnbyte = 48;
                    break;
                case 252:
                    returnbyte = 49;
                    break;
                case 79:
                    returnbyte = 50;
                    break;
                case 135:
                    returnbyte = 51;
                    break;
                case 47:
                    returnbyte = 52;
                    break;
                case 169:
                    returnbyte = 53;
                    break;
                case 101:
                    returnbyte = 54;
                    break;
                case 158:
                    returnbyte = 55;
                    break;
                case 172:
                    returnbyte = 56;
                    break;
                case 129:
                    returnbyte = 57;
                    break;
                case 235:
                    returnbyte = 58;
                    break;
                case 103:
                    returnbyte = 59;
                    break;
                case 80:
                    returnbyte = 60;
                    break;
                case 139:
                    returnbyte = 61;
                    break;
                case 154:
                    returnbyte = 62;
                    break;
                case 145:
                    returnbyte = 63;
                    break;
                case 231:
                    returnbyte = 64;
                    break;
                case 10:
                    returnbyte = 65;
                    break;
                case 125:
                    returnbyte = 66;
                    break;
                case 120:
                    returnbyte = 67;
                    break;
                case 146:
                    returnbyte = 68;
                    break;
                case 138:
                    returnbyte = 69;
                    break;
                case 150:
                    returnbyte = 70;
                    break;
                case 196:
                    returnbyte = 71;
                    break;
                case 233:
                    returnbyte = 72;
                    break;
                case 225:
                    returnbyte = 73;
                    break;
                case 243:
                    returnbyte = 74;
                    break;
                case 203:
                    returnbyte = 75;
                    break;
                case 249:
                    returnbyte = 76;
                    break;
                case 237:
                    returnbyte = 77;
                    break;
                case 62:
                    returnbyte = 78;
                    break;
                case 119:
                    returnbyte = 79;
                    break;
                case 0:
                    returnbyte = 80;
                    break;
                case 197:
                    returnbyte = 81;
                    break;
                case 199:
                    returnbyte = 82;
                    break;
                case 97:
                    returnbyte = 83;
                    break;
                case 141:
                    returnbyte = 84;
                    break;
                case 250:
                    returnbyte = 85;
                    break;
                case 192:
                    returnbyte = 86;
                    break;
                case 59:
                    returnbyte = 87;
                    break;
                case 137:
                    returnbyte = 88;
                    break;
                case 242:
                    returnbyte = 89;
                    break;
                case 244:
                    returnbyte = 90;
                    break;
                case 81:
                    returnbyte = 91;
                    break;
                case 246:
                    returnbyte = 92;
                    break;
                case 108:
                    returnbyte = 93;
                    break;
                case 236:
                    returnbyte = 94;
                    break;
                case 17:
                    returnbyte = 95;
                    break;
                case 16:
                    returnbyte = 96;
                    break;
                case 178:
                    returnbyte = 97;
                    break;
                case 116:
                    returnbyte = 98;
                    break;
                case 56:
                    returnbyte = 99;
                    break;
                case 173:
                    returnbyte = 100;
                    break;
                case 90:
                    returnbyte = 101;
                    break;
                case 134:
                    returnbyte = 102;
                    break;
                case 228:
                    returnbyte = 103;
                    break;
                case 66:
                    returnbyte = 104;
                    break;
                case 89:
                    returnbyte = 105;
                    break;
                case 73:
                    returnbyte = 106;
                    break;
                case 96:
                    returnbyte = 107;
                    break;
                case 107:
                    returnbyte = 108;
                    break;
                case 255:
                    returnbyte = 109;
                    break;
                case 200:
                    returnbyte = 110;
                    break;
                case 48:
                    returnbyte = 111;
                    break;
                case 144:
                    returnbyte = 112;
                    break;
                case 7:
                    returnbyte = 113;
                    break;
                case 40:
                    returnbyte = 114;
                    break;
                case 221:
                    returnbyte = 115;
                    break;
                case 87:
                    returnbyte = 116;
                    break;
                case 99:
                    returnbyte = 117;
                    break;
                case 91:
                    returnbyte = 118;
                    break;
                case 133:
                    returnbyte = 119;
                    break;
                case 117:
                    returnbyte = 120;
                    break;
                case 70:
                    returnbyte = 121;
                    break;
                case 4:
                    returnbyte = 122;
                    break;
                case 114:
                    returnbyte = 123;
                    break;
                case 147:
                    returnbyte = 124;
                    break;
                case 140:
                    returnbyte = 125;
                    break;
                case 14:
                    returnbyte = 126;
                    break;
                case 22:
                    returnbyte = 127;
                    break;
                case 72:
                    returnbyte = 128;
                    break;
                case 31:
                    returnbyte = 129;
                    break;
                case 180:
                    returnbyte = 130;
                    break;
                case 254:
                    returnbyte = 131;
                    break;
                case 60:
                    returnbyte = 132;
                    break;
                case 78:
                    returnbyte = 133;
                    break;
                case 128:
                    returnbyte = 134;
                    break;
                case 241:
                    returnbyte = 135;
                    break;
                case 5:
                    returnbyte = 136;
                    break;
                case 65:
                    returnbyte = 137;
                    break;
                case 112:
                    returnbyte = 138;
                    break;
                case 186:
                    returnbyte = 139;
                    break;
                case 136:
                    returnbyte = 140;
                    break;
                case 211:
                    returnbyte = 141;
                    break;
                case 3:
                    returnbyte = 142;
                    break;
                case 98:
                    returnbyte = 143;
                    break;
                case 93:
                    returnbyte = 144;
                    break;
                case 219:
                    returnbyte = 145;
                    break;
                case 12:
                    returnbyte = 146;
                    break;
                case 39:
                    returnbyte = 147;
                    break;
                case 33:
                    returnbyte = 148;
                    break;
                case 113:
                    returnbyte = 149;
                    break;
                case 109:
                    returnbyte = 150;
                    break;
                case 84:
                    returnbyte = 151;
                    break;
                case 152:
                    returnbyte = 152;
                    break;
                case 18:
                    returnbyte = 153;
                    break;
                case 50:
                    returnbyte = 154;
                    break;
                case 160:
                    returnbyte = 155;
                    break;
                case 234:
                    returnbyte = 156;
                    break;
                case 148:
                    returnbyte = 157;
                    break;
                case 64:
                    returnbyte = 158;
                    break;
                case 130:
                    returnbyte = 159;
                    break;
                case 165:
                    returnbyte = 160;
                    break;
                case 69:
                    returnbyte = 161;
                    break;
                case 214:
                    returnbyte = 162;
                    break;
                case 207:
                    returnbyte = 163;
                    break;
                case 132:
                    returnbyte = 164;
                    break;
                case 76:
                    returnbyte = 165;
                    break;
                case 71:
                    returnbyte = 166;
                    break;
                case 232:
                    returnbyte = 167;
                    break;
                case 74:
                    returnbyte = 168;
                    break;
                case 168:
                    returnbyte = 169;
                    break;
                case 202:
                    returnbyte = 170;
                    break;
                case 220:
                    returnbyte = 171;
                    break;
                case 230:
                    returnbyte = 172;
                    break;
                case 167:
                    returnbyte = 173;
                    break;
                case 240:
                    returnbyte = 174;
                    break;
                case 95:
                    returnbyte = 175;
                    break;
                case 53:
                    returnbyte = 176;
                    break;
                case 35:
                    returnbyte = 177;
                    break;
                case 193:
                    returnbyte = 178;
                    break;
                case 212:
                    returnbyte = 179;
                    break;
                case 61:
                    returnbyte = 180;
                    break;
                case 184:
                    returnbyte = 181;
                    break;
                case 36:
                    returnbyte = 182;
                    break;
                case 194:
                    returnbyte = 183;
                    break;
                case 115:
                    returnbyte = 184;
                    break;
                case 195:
                    returnbyte = 185;
                    break;
                case 149:
                    returnbyte = 186;
                    break;
                case 27:
                    returnbyte = 187;
                    break;
                case 206:
                    returnbyte = 188;
                    break;
                case 204:
                    returnbyte = 189;
                    break;
                case 55:
                    returnbyte = 190;
                    break;
                case 179:
                    returnbyte = 191;
                    break;
                case 122:
                    returnbyte = 192;
                    break;
                case 189:
                    returnbyte = 193;
                    break;
                case 45:
                    returnbyte = 194;
                    break;
                case 51:
                    returnbyte = 195;
                    break;
                case 23:
                    returnbyte = 196;
                    break;
                case 181:
                    returnbyte = 197;
                    break;
                case 42:
                    returnbyte = 198;
                    break;
                case 215:
                    returnbyte = 199;
                    break;
                case 157:
                    returnbyte = 200;
                    break;
                case 161:
                    returnbyte = 201;
                    break;
                case 190:
                    returnbyte = 202;
                    break;
                case 121:
                    returnbyte = 203;
                    break;
                case 227:
                    returnbyte = 204;
                    break;
                case 21:
                    returnbyte = 205;
                    break;
                case 124:
                    returnbyte = 206;
                    break;
                case 162:
                    returnbyte = 207;
                    break;
                case 30:
                    returnbyte = 208;
                    break;
                case 210:
                    returnbyte = 209;
                    break;
                case 182:
                    returnbyte = 210;
                    break;
                case 187:
                    returnbyte = 211;
                    break;
                case 67:
                    returnbyte = 212;
                    break;
                case 86:
                    returnbyte = 213;
                    break;
                case 77:
                    returnbyte = 214;
                    break;
                case 8:
                    returnbyte = 215;
                    break;
                case 85:
                    returnbyte = 216;
                    break;
                case 9:
                    returnbyte = 217;
                    break;
                case 208:
                    returnbyte = 218;
                    break;
                case 229:
                    returnbyte = 219;
                    break;
                case 1:
                    returnbyte = 220;
                    break;
                case 222:
                    returnbyte = 221;
                    break;
                case 68:
                    returnbyte = 222;
                    break;
                case 205:
                    returnbyte = 223;
                    break;
                case 142:
                    returnbyte = 224;
                    break;
                case 82:
                    returnbyte = 225;
                    break;
                case 106:
                    returnbyte = 226;
                    break;
                case 15:
                    returnbyte = 227;
                    break;
                case 24:
                    returnbyte = 228;
                    break;
                case 63:
                    returnbyte = 229;
                    break;
                case 54:
                    returnbyte = 230;
                    break;
                case 111:
                    returnbyte = 231;
                    break;
                case 217:
                    returnbyte = 232;
                    break;
                case 29:
                    returnbyte = 233;
                    break;
                case 223:
                    returnbyte = 234;
                    break;
                case 188:
                    returnbyte = 235;
                    break;
                case 131:
                    returnbyte = 236;
                    break;
                case 198:
                    returnbyte = 237;
                    break;
                case 88:
                    returnbyte = 238;
                    break;
                case 34:
                    returnbyte = 239;
                    break;
                case 224:
                    returnbyte = 240;
                    break;
                case 25:
                    returnbyte = 241;
                    break;
                case 126:
                    returnbyte = 242;
                    break;
                case 170:
                    returnbyte = 243;
                    break;
                case 163:
                    returnbyte = 244;
                    break;
                case 191:
                    returnbyte = 245;
                    break;
                case 247:
                    returnbyte = 246;
                    break;
                case 102:
                    returnbyte = 247;
                    break;
                case 58:
                    returnbyte = 248;
                    break;
                case 156:
                    returnbyte = 249;
                    break;
                case 20:
                    returnbyte = 250;
                    break;
                case 155:
                    returnbyte = 251;
                    break;
                case 245:
                    returnbyte = 252;
                    break;
                case 2:
                    returnbyte = 253;
                    break;
                case 143:
                    returnbyte = 254;
                    break;
                case 123:
                    returnbyte = 255;
                    break;
                default:
                    returnbyte = inputbyte;
                    break;
            }

            return returnbyte;
        }

        static byte[] DecompressFile(byte[] data)
        {
            
            // standard Gzip decompression
            using (var compressedStream = new MemoryStream(data))
            {
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        
                        zipStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                        
                    }
                }
            }
            
            /*
            //return Ionic.Zlib.ZlibStream.UncompressBuffer(data);
            //return Ionic.Zlib.GZipStream.UncompressBuffer(data);
            byte[] decompressed;
            using (var outputStream = new MemoryStream(data))
            {
                using (var inflateStream = new System.IO.Compression.DeflateStream(outputStream, System.IO.Compression.CompressionMode.Decompress))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        inflateStream.CopyTo(decompressedStream);
                        decompressed = decompressedStream.ToArray();
                    }                       
                }               
            }
            return decompressed;
            */
        }
        /*
        static byte[]CompressFile(byte[] data)
        {
            using (MemoryStream originalStream = new MemoryStream(data))
            using (MemoryStream compressedStream = new MemoryStream())
            {
                var compressor = new ZOutputStream(compressedStream, 9);

                try
                {
                    byte[] buffer = data.Length < 4096 ? new byte[data.Length] : new byte[4096];
                    int read;
                    while ((read = originalStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        compressor.Write(buffer, 0, read);
                    }
                    compressor.finish();
                    return compressedStream.ToArray();
                }
                finally
                {
                    compressor.Dispose();
                }
            }
          
            
        }
        */
        public void PopulateDummyInfo()
        {
            Models.DS1_FileInfo DemoFileInfo = new Models.DS1_FileInfo();
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

        public void ParseGGArchiveNew()
        {
            string filepath = CurrentOpenFileFullName;
           
            Process_Update("Loading .gg archive data:", "  /  ");
            using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                byte[] HeaderBuffer = new byte[16];
                fs.Read(HeaderBuffer, 0, 16);
                HeaderBytes = HeaderBuffer;

                //get the file path and name objects. skip first 16 +800 take the amount from 
                // bytes 9-12. use that byte array to parse file names and whatever. Find a way to stream this instead of holding
                //in memory?

                //800 for ds1
                fs.Seek(HashLength, SeekOrigin.Current);
                //960 for ds2/3
                //fs.Seek(960, SeekOrigin.Current);

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
                    Models.DS1_FileInfo fileInfo = new Models.DS1_FileInfo()
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
                                fileInfo.Attr1_FilepathOffset = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                //int offset = BitConverter.ToInt32(atrributebuffer, ((o * 36) + (a * 4)));
                                int offset = BitConverter.ToInt32(fileInfo.Attr1_FilepathOffset, 0);
                                string currentstring = ReadNextString(FileNameBuffer, offset);
                                fileInfo.FilePath = currentstring;
                                break;
                            case 1:
                                fileInfo.Attr2_FilenameOffset = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                //offset = BitConverter.ToInt32(atrributebuffer, ((o * 36) + (a * 4)));
                                offset = BitConverter.ToInt32(fileInfo.Attr2_FilenameOffset,0);
                                currentstring = ReadNextString(FileNameBuffer, offset);
                                fileInfo.FileName = currentstring;
                                break;
                            case 2:
                                fileInfo.Attr3_ItemID = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                //offset = BitConverter.ToInt32(atrributebuffer, ((o * 36) + (a * 4)));
                                offset = BitConverter.ToInt32(fileInfo.Attr3_ItemID, 0);
                                currentstring = ReadNextString(FileNameBuffer, offset);
                                fileInfo.ItemID = currentstring;
                                break;
                            case 3:
                                //fileInfo.Attribute4 = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                //take the last 4 bytes in the buffer
                                fileInfo.Attr4 = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();

                                break;
                            case 4:
                                //fileInfo.Attribute5 = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                fileInfo.Attr5 = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                break;
                            case 5:
                                //new int value of last four bytes
                                //save as uncompressed
                                fileInfo.Attr6_UncompressedSize = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                fileInfo.UncompressedSize = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            case 6:
                                //new int value of last four bytes
                                //save as uncompressed
                                fileInfo.Attr7_CompressedSize = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                fileInfo.CompressedSize = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            case 7:
                                //new int value of last four bytes
                                //save as iscompressed
                                fileInfo.Attr8_IsCompressed = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                fileInfo.IsCompressed = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            case 8:
                                //new int value of last four bytes
                                //save as start of filestream
                                fileInfo.Attr9_FileOffset = atrributebuffer.Skip((o * 36) + (a * 4)).Take(4).ToArray();
                                fileInfo.FileOffset = BitConverter.ToInt32(atrributebuffer, (o * 36) + (a * 4));
                                break;
                            default:
                                break;
                        }
                    }
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PercentComplete = ((decimal)o / numarchivefiles) * 100;
                        ProgressText = $"{o} / {NumArchiveFiles}";
                        Files.Add(fileInfo);
                        
                    });
                        
                    
                }

            }
            
            Process_Update(string.Empty,string.Empty);
        }

        private string ReadNextString(byte[] fileNameBuffer, int i)
        {
            int EndOfString = Array.IndexOf<byte>(fileNameBuffer, 0, i) - i;

            byte[] ObfNameBytes = fileNameBuffer.Skip(i).Take(EndOfString).ToArray();

            byte[] Namebytes = DSFileNameXor(ObfNameBytes);
            string Name = System.Text.Encoding.UTF8.GetString(Namebytes);
            
            return Name;
        }
        private void Process_Update(string processDesc, string progressTxt)
        {
            PercentComplete = 0;
            OperationText = processDesc; 
            ProgressText = progressTxt;
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
      
        #region Property Changed Method and Event

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
