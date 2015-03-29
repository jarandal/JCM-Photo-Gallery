using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Input;

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading; // DispatchTimer

namespace JMC_Photo_Gallery
{
    public partial class Window_PhotoGallery : Window
    {
        private ImageGallery _GalleryModel1;
        private ImageGallery _GalleryModel2;
        private ImageGallery _GalleryModel3;
        private AsyncImageLoader _Loader1;
        private AsyncImageLoader _Loader2;

        public Window_PhotoGallery()
        {
            InitializeComponent();
            _GalleryModel1 = (ImageGallery)this.FindResource("HomeModel");
            _GalleryModel2 = (ImageGallery)this.FindResource("CollectionModel");
            _GalleryModel3 = (ImageGallery)this.FindResource("FolderModel");
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            LoadViewConfig();
            LoadHome_Driver();
            //throw new Exception("exception test");
        }

        private void WindowClosing(object sender, EventArgs e)
        {
            SaveViewConfig();
            AR.ImportConfigs.StartExport("View Configuration");
            CloseExtras();
        }

        private void LoadViewConfig()
        {
            x_ThumbSize1.Value = AR.ViewConfig._L1_size;
            x_ThumbSize2.Value = AR.ViewConfig._L2_size;
            x_ThumbSize3.Value = AR.ViewConfig._L3_size;

            x_ThumbCount1.SelectedIndex = AR.ViewConfig._L1_count - 1;

            int tempIndex = AR.ViewConfig._L2_count;
            if (tempIndex == 25)
                tempIndex = 11;
            if (tempIndex == 50)
                tempIndex = 12;
            if (tempIndex == 75)
                tempIndex = 13;
            if (tempIndex == 100)
                tempIndex = 14;
            tempIndex--;
            x_ThumbCount2.SelectedIndex = tempIndex;
        }

        private void SaveViewConfig()
        {
            AR.ViewConfig._L1_size = (int)x_ThumbSize1.Value;
            AR.ViewConfig._L2_size = (int)x_ThumbSize2.Value;
            AR.ViewConfig._L3_size = (int)x_ThumbSize3.Value;
            AR.ViewConfig._L1_count = _comboboxItemValues[x_ThumbCount1.SelectedIndex];
            AR.ViewConfig._L2_count = _comboboxItemValues[x_ThumbCount2.SelectedIndex];
            //AR.ViewConfig._L3_count = 100;
        }

        private void CloseExtras()
        {
            if (_Window_Overview3D != null)
                _Window_Overview3D.Close();
            if (_Window_SlideShow != null)
                _Window_SlideShow.Close();
            if (_Window_MoviePlayer != null)
                _Window_MoviePlayer.Close();
        }

        // =====================================================================================
        // ================================= Regions ===========================================
        // =====================================================================================

        #region Load Images

        private void x_ThumbCount1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_GalleryModel1 != null)
                LoadHome();
        }

        private void x_ThumbCount2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_GalleryModel2 != null)
                LoadCollection(_currentCollectionPath);
        }

        private void LoadHome_Driver()
        {
            LoadHome();
            x_ImageScrollViewer.ScrollToTop();
            x_Display1.Visibility = System.Windows.Visibility.Visible;
            x_Display2.Visibility = System.Windows.Visibility.Collapsed;
            x_Display3.Visibility = System.Windows.Visibility.Collapsed;
            //return; // testing use
            x_ThumbControl1.Visibility = System.Windows.Visibility.Visible;
            x_ThumbControl2.Visibility = System.Windows.Visibility.Collapsed;
            x_ThumbControl3.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoadCollection_Driver(string path)
        {
            LoadCollection(path);
            x_ImageScrollViewer.ScrollToTop();
            x_Display1.Visibility = System.Windows.Visibility.Collapsed;
            x_Display2.Visibility = System.Windows.Visibility.Visible;
            x_Display3.Visibility = System.Windows.Visibility.Collapsed;
            //return; // testing use
            x_ThumbControl1.Visibility = System.Windows.Visibility.Collapsed;
            x_ThumbControl2.Visibility = System.Windows.Visibility.Visible;
            x_ThumbControl3.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LoadFolder_Driver(string path)
        {
            LoadFolder(path);
            x_ImageScrollViewer.ScrollToTop();
            x_Display1.Visibility = System.Windows.Visibility.Collapsed;
            x_Display2.Visibility = System.Windows.Visibility.Collapsed;
            x_Display3.Visibility = System.Windows.Visibility.Visible;
            //return; // testing use
            x_ThumbControl1.Visibility = System.Windows.Visibility.Collapsed;
            x_ThumbControl2.Visibility = System.Windows.Visibility.Collapsed;
            x_ThumbControl3.Visibility = System.Windows.Visibility.Visible;
        }

        private int[] _comboboxItemValues = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 25, 50, 75, 100 };
        private int _currentImageCount1 = 0;
        private void LoadHome()
        {
            try
            {
                // reset if gallery is changed, including ordering changes.
                bool hasReset = false;
                if (GalleryChanged())
                {
                    hasReset = true;
                    _GalleryModel1.Collections.Clear();
                    foreach (string folder in AR.GalleryConfig.Folders)
                    {
                        ImageCollection collection = new ImageCollection(folder, new ObservableCollection<ImageFile>());
                        _GalleryModel1.Collections.Add(collection);
                    }
                }

                // change collection when imageCount changed or hasReset
                int imageCount = _comboboxItemValues[x_ThumbCount1.SelectedIndex];
                if (_currentImageCount1 != imageCount || hasReset)
                {
                    _currentImageCount1 = imageCount;
                    Queue<ImageCollection> tasks = new Queue<ImageCollection>();
                    foreach (ImageCollection ic in _GalleryModel1.Collections)
                        tasks.Enqueue(ic);
                    if (_Loader1 != null)
                        _Loader1.CancelAsync();
                    _Loader1 = new AsyncImageLoader(tasks, imageCount, true, Dispatcher.CurrentDispatcher);
                    _Loader1.RunAsync();
                }
            }
            catch { }
        }

        private bool GalleryChanged()
        {
            if (_GalleryModel1.Collections.Count != AR.GalleryConfig.Folders.Length)
                return true;

            for (int i = 0; i < AR.GalleryConfig.Folders.Length; i++)
            {
                if (new FileInfo(_GalleryModel1.Collections[i].CollectionPath).FullName.ToLower() !=
                    new FileInfo(AR.GalleryConfig.Folders[i]).FullName.ToLower())
                    return true;
            }

            return false;
        }

        string _currentCollectionPath = "";
        private int _currentImageCount2 = 0;
        private void LoadCollection(string path)
        {
            try
            {
                if (path == null || path == "")
                {
                    _GalleryModel2.Collections.Clear();
                    return;
                }

                // Prevent scanning its own data store.
                if (path.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                    return;

                // change collection if path is different
                if (_currentCollectionPath.ToLower() != path.ToLower())
                {
                    _GalleryModel2.Collections.Clear();
                    Collection<string> folders = new Collection<string>();
                    FoldersInCollection(path, folders);
                    foreach (string folder in folders)
                    {
                        if (!folder.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                        {
                            ImageCollection collection = new ImageCollection(folder, new ObservableCollection<ImageFile>());
                            _GalleryModel2.Collections.Add(collection);
                        }
                    }
                }

                // change collection when imageCount changed or collection path is different
                int imageCount = _comboboxItemValues[x_ThumbCount2.SelectedIndex];
                if (_currentImageCount2 != imageCount || _currentCollectionPath.ToLower() != path.ToLower())
                {
                    _currentImageCount2 = imageCount;
                    _currentCollectionPath = path;
                    Queue<ImageCollection> tasks = new Queue<ImageCollection>();
                    foreach (ImageCollection ic in _GalleryModel2.Collections)
                        tasks.Enqueue(ic);
                    if (_Loader2 != null)
                        _Loader2.CancelAsync();
                    _Loader2 = new AsyncImageLoader(tasks, imageCount, false, Dispatcher.CurrentDispatcher);
                    _Loader2.RunAsync();
                }
            }
            catch { }
        }

        private void FoldersInCollection(string path, Collection<string> result)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);

                //JAL 20150329
                if (dir.Name.StartsWith(".")) return;

                foreach (FileInfo info in new DirectoryInfo(path).GetFiles())
                {
                    if (AR.GalleryConfig.IsImage(info.Extension))
                    {
                        result.Add(path);
                        break;
                    }
                }

                foreach (DirectoryInfo info in new DirectoryInfo(path).GetDirectories())
                    FoldersInCollection(info.FullName, result);
            }
            catch { }
        }

        private void LoadFolder(string path)
        {
            try
            {
                if (path == null || path == "")
                {
                    _GalleryModel3.Collections.Clear();
                    return;
                }

                // Prevent scanning its own data store.
                if (path.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                    return;

                // change folder collection path is different
                if (_GalleryModel3.Collections.Count < 1 ||
                    _GalleryModel3.Collections[0].CollectionPath.ToLower() != path.ToLower())
                {
                    _GalleryModel3.Collections.Clear();
                    ImageCollection temp = new ImageCollection(path, new ObservableCollection<ImageFile>());
                    foreach (FileInfo info in new DirectoryInfo(path).GetFiles())
                        if (AR.GalleryConfig.IsImage(info.Extension))
                            temp.Add(new ImageFile(info.FullName));
                    _GalleryModel3.Collections.Add(temp);
                }
            }
            catch { }
        }

        #endregion

        #region Top Buttons

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (x_Display3.IsVisible && _GalleryModel3.Collections != null && _GalleryModel3.Collections.Count > 0)
            {
                string folderPath = _GalleryModel3.Collections[0].CollectionPath.ToLower();
                foreach (string path in AR.GalleryConfig.Folders)
                {
                    string remain = folderPath.Replace(path.ToLower(), "");
                    if (remain.Length == 0 || remain[0] == '\\')
                    {
                        LoadCollection_Driver(path);
                        return;
                    }
                }
            }
            else
                LoadHome_Driver();
        }

        private void Home2D_Click(object sender, RoutedEventArgs e)
        {
            LoadHome_Driver();
        }

        private void Config_Click(object sender, RoutedEventArgs e)
        {
            Window_Config temp = new Window_Config();
            temp.ShowDialog();
            LoadHome_Driver();

            // To make integration simplier, 
            // close them to the refresh changes in Gallery Setting.
            CloseExtras();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            Window_About win = new Window_About();
            win.ShowDialog();
        }

        #endregion

        #region Image/Folder Buttons

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            System.Windows.Media.Color tempC = System.Windows.Media.Color.FromArgb(80, 0, 150, 250);
            System.Windows.Media.Color tempC2 = System.Windows.Media.Color.FromArgb(200, 120, 210, 255);
            (sender as Border).Background = new SolidColorBrush(tempC);
            (sender as Border).BorderBrush = new SolidColorBrush(tempC2);
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            System.Windows.Media.Color tempC = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
            System.Windows.Media.Color tempC2 = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
            (sender as Border).Background = new SolidColorBrush(tempC);
            (sender as Border).BorderBrush = new SolidColorBrush(tempC2);
        }

        private void ImageButton_DoubleClick1(object sender, MouseButtonEventArgs e)
        {
            Button button = (Button)sender;
            ImageFile imagefile = (ImageFile)button.DataContext;
            LoadFolder_Driver(imagefile.ParentPath);
        }

        private void ImageButton_DoubleClick3(object sender, MouseButtonEventArgs e)
        {
            // open in slide show
            ImageFile imagefile = (ImageFile)((Button)sender).DataContext;
            SlideShow_Open(imagefile.FilePath);

            // needed because double click brings gallery to front.
            // not really great solution, but, will keep it simple.
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void FolderButton_Click1(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            ImageCollection collection = (ImageCollection)button.DataContext;
            LoadCollection_Driver(collection.CollectionPath);
        }

        private void FolderButton_Click2(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            ImageCollection collection = (ImageCollection)button.DataContext;
            LoadFolder_Driver(collection.CollectionPath);
        }

        private void FolderButton_Click3(object sender, RoutedEventArgs e)
        {
            // open in slide show
            Button button = (Button)sender;
            ImageCollection collection = (ImageCollection)button.DataContext;
            SlideShow_Open(collection.CollectionPath);
        }

        private void MenuItem_Click1(object sender, RoutedEventArgs e)
        {
            ImageFile imagefile = (ImageFile)((MenuItem)sender).DataContext;
            LoadFolder_Driver(imagefile.ParentPath);
        }

        private void MenuItem_Click13(object sender, RoutedEventArgs e)
        {
            // open in slide show
            ImageFile imagefile = (ImageFile)((MenuItem)sender).DataContext;
            SlideShow_Open(imagefile.FilePath);
        }

        private void MenuItem_Click2(object sender, RoutedEventArgs e)
        {
            ImageFile imagefile = (ImageFile)((MenuItem)sender).DataContext;
            OpenUsingOS(imagefile.FilePath);
        }

        private void MenuItem_Click3(object sender, RoutedEventArgs e)
        {
            ImageFile imagefile = (ImageFile)((MenuItem)sender).DataContext;
            OpenUsingOS(imagefile.ParentPath);
        }

        private void MenuItem_Click4(object sender, RoutedEventArgs e)
        {
            ImageFile imagefile = (ImageFile)((MenuItem)sender).DataContext;
            string path = AR.GrandThumbPathMaker.GetThumbPath(imagefile.FilePath, false);
            OpenUsingOS(new FileInfo(path).DirectoryName);
        }

        private void OpenUsingOS(string path)
        {
            try
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = path;
                proc.Start();
            }
            catch { }
        }

        #endregion

        #region Integration With Window_Overview3D

        Window_Overview3D _Window_Overview3D;
        private void Home3D_Click(object sender, RoutedEventArgs e)
        {
            if (_Window_Overview3D == null || !_Window_Overview3D.IsLoaded)
            {
                OverviewDisplay_File.s_thumbPathMaker = AR.GrandThumbPathMaker;
                _Window_Overview3D = new Window_Overview3D();
                _Window_Overview3D.Show();
                _Window_Overview3D.Activate();
            }
            else
            {
                _Window_Overview3D.Show();
                _Window_Overview3D.Activate();
            }
        }

        public void ShowHome()
        {
            LoadHome_Driver();
        }

        public void ShowCollection(string path)
        {
            LoadCollection_Driver(path);
        }

        public void ShowFolder(string path)
        {
            LoadFolder_Driver(path);
        }

        #endregion

        #region Integration With Window_SlideShow

        Window_SlideShow _Window_SlideShow;
        private void SlideShow_Click(object sender, RoutedEventArgs e)
        {
            if (_Window_SlideShow != null)
                _Window_SlideShow.Close();

            _Window_SlideShow = new Window_SlideShow();
            _Window_SlideShow.Show();
            _Window_SlideShow.Load(GetSlidePaths());
        }

        private Collection<string> GetSlidePaths()
        {
            // Get paths based on current view mode
            Collection<string> result = new Collection<string>();
            if (x_Display3.IsVisible && _GalleryModel3 != null)
                GetSlidePaths(_GalleryModel3, result);
            else if (x_Display2.IsVisible && _GalleryModel2 != null)
                GetSlidePaths(_GalleryModel2, result);
            else
                GetSlidePaths(_GalleryModel1, result);

            return result;
        }

        private void GetSlidePaths(ImageGallery model, Collection<string> result)
        {
            foreach (ImageCollection collection in model.Collections)
                foreach (ImageFile file in collection.FlattenCollection)
                    result.Add(file.FilePath);
        }

        private void SlideShow_Open(string path)
        {
            if (_Window_SlideShow != null)
                _Window_SlideShow.Close();

            _Window_SlideShow = new Window_SlideShow();
            _Window_SlideShow.Show();
            _Window_SlideShow.Load(SlideShow_Files(path), true);
        }

        private Collection<string> SlideShow_Files(string path)
        {
            Collection<string> result = new Collection<string>();

            try
            {
                System.IO.DirectoryInfo dir = new DirectoryInfo(path);
                foreach (System.IO.FileInfo file in dir.GetFiles())
                    if (AR.GalleryConfig.IsImage(file.Extension))
                        result.Add(file.FullName);
            }
            catch { }

            try
            {
                System.IO.FileInfo targetFile = new FileInfo(path);
                foreach (System.IO.FileInfo file in targetFile.Directory.GetFiles())
                    if (AR.GalleryConfig.IsImage(file.Extension))
                        result.Add(file.FullName);

                for (int i = 0; i < result.Count && result[0].ToLower() != targetFile.FullName.ToLower(); i++)
                {
                    result.Add(result[0]);
                    result.RemoveAt(0);
                }
            }
            catch { }

            return result;
        }

        #endregion

        #region Integration With Window_MoviePlayer

        Window_MoviePlayer _Window_MoviePlayer;
        private void MoviePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (_Window_MoviePlayer == null || !_Window_MoviePlayer.IsLoaded)
            {
                UserControl_MediaElement_Ext.ThumbDB_BasePath = AR._MovieThumbDBPath;
                _Window_MoviePlayer = new Window_MoviePlayer();
                _Window_MoviePlayer.Show();
                _Window_MoviePlayer.Activate();
            }
            else
            {
                _Window_MoviePlayer.Show();
                _Window_MoviePlayer.Activate();
            }
        }

        #endregion

        #region Testing Buttons

        private void x_ToggleHome2D_Click(object sender, RoutedEventArgs e)
        {
            if (x_Display1.Visibility == System.Windows.Visibility.Visible)
                x_Display1.Visibility = System.Windows.Visibility.Collapsed;
            else
                x_Display1.Visibility = System.Windows.Visibility.Visible;
        }

        private void x_ToggleCollection_Click(object sender, RoutedEventArgs e)
        {
            if (x_Display2.Visibility == System.Windows.Visibility.Visible)
                x_Display2.Visibility = System.Windows.Visibility.Collapsed;
            else
                x_Display2.Visibility = System.Windows.Visibility.Visible;
        }

        private void x_ToggleFolder_Click(object sender, RoutedEventArgs e)
        {
            if (x_Display3.Visibility == System.Windows.Visibility.Visible)
                x_Display3.Visibility = System.Windows.Visibility.Collapsed;
            else
                x_Display3.Visibility = System.Windows.Visibility.Visible;
        }

        #endregion

    }
}