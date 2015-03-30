using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel; //INotifyPropertyChanged

namespace JMC_Photo_Gallery
{
    public class ImageFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public ImageFile() { }
        public ImageFile(string filePath)
        {
            this.FilePath = filePath;
        }

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                _parentPath = new System.IO.FileInfo(_filePath).DirectoryName;
                this.NotifyPropertyChanged("FilePath");
                this.NotifyPropertyChanged("ParentPath");
                this.NotifyPropertyChanged("ColorCode");
            }
        }
        private string _parentPath;
        public string ParentPath { get { return _parentPath; } }

        private System.Windows.Media.SolidColorBrush _colorCode;
        public System.Windows.Media.SolidColorBrush ColorCode
        {
            get { return _colorCode; }
            set
            {
                _colorCode = value;

                this.NotifyPropertyChanged("ColorCode");
            }
        }

        private Boolean _selected;

        public Boolean Selected
        {
            get { 
                return _selected; 
            }
            set { 
                _selected = value;
                this.NotifyPropertyChanged("Selected");
            }
        }

        
    }

    public class ImageCollection
    {
        private string _collectionPath = "";
        public string CollectionPath { get { return _collectionPath; } }
        private ObservableCollection<ImageFile> _flattenCollection;
        public ObservableCollection<ImageFile> FlattenCollection { get { return _flattenCollection; } }
        private ObservableCollection2D<string, string, ImageFile> _collection;
        private Random r = new Random();

        public ImageCollection() { }
        public ImageCollection(string folderPath, ObservableCollection<ImageFile> linkedCollection)
        {
            _collectionPath = folderPath;
            _flattenCollection = linkedCollection;
            _collection = new ObservableCollection2D<string, string, ImageFile>(_flattenCollection);
        }

        public void Add(ImageFile imageFile)
        {
            if (_collection.HasKey(imageFile.ParentPath))
            {
                foreach (ImageFile fromSameParent in _collection.Get(imageFile.ParentPath).Values)
                {
                    imageFile.ColorCode = fromSameParent.ColorCode;
                    break;
                }
            }
            else
            {
                imageFile.ColorCode = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(150, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)));
            }

            _collection.Set(imageFile.ParentPath, imageFile.FilePath, imageFile);
        }

        public void Add(ImageFile[] imageFile)
        {
            foreach (ImageFile file in imageFile)
                Add(file);
        }

        public void Remove(ImageFile imageFile)
        {
            _collection.Remove(imageFile.ParentPath, imageFile.FilePath);
        }

        public void Remove(ImageFile[] imageFile)
        {
            foreach (ImageFile file in imageFile)
                Remove(file);
        }

        public bool Contains(ImageFile imageFile)
        {
            return _collection.HasKey(imageFile.ParentPath, imageFile.FilePath);
        }

        public int CountInDir(string path)
        {
            return _collection.CountIn(path);
        }
    }

    public class ImageGallery
    {
        private ObservableCollection<ImageCollection> _collections = new ObservableCollection<ImageCollection>();
        public ObservableCollection<ImageCollection> Collections { get { return _collections; } }
        public ImageGallery() { }
    }
}
