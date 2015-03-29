using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;

// Added For Async Video Image Creation
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;

// For Converter
using System.Windows.Data;
using System.Globalization;  // Culture

namespace JMC_Photo_Gallery
{
    class MovieFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public MovieFile() { }
        public MovieFile(string path)
        {
            Path = path;
        }

        string _path;
        Uri _uri;
        public string Path
        {
            get { return _path; }
            set
            {
                _path = value; _uri = new Uri(value);
                OnPropertyChanged("Path");
                OnPropertyChanged("Uri");
                OnPropertyChanged("Name");
            }
        }
        public Uri Uri { get { return _uri; } }
        public string Name { get { return new System.IO.FileInfo(_path).Name; } }
    }

    class MovieFolder
    {
        public MovieFolder(string path)
        {
            _path = path;
        }
        string _path;
        public string Path { get { return _path; } }
        ObservableCollection<MovieFile> _movieFiles = new ObservableCollection<MovieFile>();
        public ObservableCollection<MovieFile> MovieFiles { get { return _movieFiles; } }
    }

    class MovieCollection
    {
        ObservableCollection<MovieFolder> _movieFolders = new ObservableCollection<MovieFolder>();
        public ObservableCollection<MovieFolder> MovieFolders { get { return _movieFolders; } }

        public Collection<MovieFile> GetMovieList()
        {
            Collection<MovieFile> resultC = new Collection<MovieFile>();
            foreach (MovieFolder tempMFolder in _movieFolders)
                foreach (MovieFile tempMFile in tempMFolder.MovieFiles)
                    resultC.Add(tempMFile);
            return resultC;
        }

        public MovieFile NextMovieFile(Uri uri)
        {
            Collection<MovieFile> movieC = GetMovieList();
            if (uri == null)
                return movieC[0];

            int index = 0;
            for (int i = 0; i < movieC.Count; i++)
                if (movieC[i].Uri.AbsolutePath.ToLower() == uri.AbsolutePath.ToLower())
                    index = i + 1;
            if (index >= movieC.Count)
                index = 0;

            return movieC[index];
        }

        public MovieFile PreviousMovieFile(Uri uri)
        {
            Collection<MovieFile> movieC = GetMovieList();
            if (uri == null)
                return movieC[0];

            int index = 0;
            for (int i = 0; i < movieC.Count; i++)
                if (movieC[i].Uri.AbsolutePath.ToLower() == uri.AbsolutePath.ToLower())
                    index = i - 1;
            if (index < 0)
                index = movieC.Count - 1;

            return movieC[index];
        }
    }
}
