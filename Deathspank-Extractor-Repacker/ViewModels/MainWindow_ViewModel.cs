using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Deathspank_Extractor_Repacker.ViewModels
{
    public class MainWindow_ViewModel : INotifyPropertyChanged
    {
        #region Private members
            private Models.FileInfo _selectedfileInfo;

            
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
                    OnPropertyChanged("FileInfo");

                }
            }
        }

        public ObservableCollection<Models.FileInfo> Files { get; set; }


        #endregion

        #region Constructor
        public MainWindow_ViewModel()
        {
            Files = new ObservableCollection<Models.FileInfo>();
            PopulateDummyInfo();
            ParseGGArchive();


        }
        #endregion

        #region Public Methods
        public void PopulateDummyInfo()
        {
            Models.FileInfo DemoFileInfo = new Models.FileInfo();
            DemoFileInfo.CompressedSize = "1";
            DemoFileInfo.FileName = "name";
            DemoFileInfo.UncompressedSize = "2";
            DemoFileInfo.FileExt = ".file";
            DemoFileInfo.FileOffset = "0x69";
            DemoFileInfo.Id = "1";
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
            string filepath = @"C:\Users\Joseph.Towns\OneDrive - Snohomish School District #201\Personal\Mods\Deathspank\GameData-000001000.gg";
            using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                byte[] b = new byte[1024];
                UTF8Encoding temp = new UTF8Encoding(true);

                while (fs.Read(b, 0, b.Length) > 0)
                {
                    //Console.WriteLine(temp.GetString(b));
                }
            }

        }
        #endregion

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
