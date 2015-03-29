using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

// 3D Panel
using WpfDiscipleBlogViewer3D;
using System.Diagnostics;
using System.Collections.ObjectModel;

// For Async Image Loader
using System.ComponentModel;

namespace JMC_Photo_Gallery
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window_Overview3D : Window
    {
        Collection<Panel3D> _panel3D;
        readonly TimeSpan ANIMATION_LENGTH = TimeSpan.FromSeconds(.7);

        public Window_Overview3D()
        {
            _panel3D = new Collection<Panel3D>();
            InitializeComponent();
        }

        void OnPanel3DLoaded(object sender, RoutedEventArgs e)
        {
            // Grab a reference to the Panel3D when it loads.
            _panel3D.Add(sender as Panel3D);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Determin Width
            int Panel3DWidth = (int)Math.Min(this.HostPanel.ActualWidth, this.HostPanel.ActualHeight) - 100;
            int Panel3DThumbWidth = 0;
            if (Panel3DWidth < 400)
                Panel3DThumbWidth = 50;
            else
                Panel3DThumbWidth = 100;

            // Setup collection
            OverviewDisplay_Collections DisplayCollections = (OverviewDisplay_Collections)this.FindResource("GalleryModel");
            DisplayCollections.Collections.Clear();
            for (int i = 0; i < AR.GalleryConfig.Folders.Length; i++)
            {
                Collection<string> filePaths = new Collection<string>();
                FindImagesInCollection(AR.GalleryConfig.Folders[i], filePaths);
                string[] filePathsArray = new string[filePaths.Count];
                filePaths.CopyTo(filePathsArray, 0);
                OverviewDisplay_Folder DisplayFolder = new OverviewDisplay_Folder(AR.GalleryConfig.Folders[i], i, Panel3DWidth, Panel3DThumbWidth);
                DisplayFolder.setupList(filePathsArray);
                DisplayCollections.Collections.Add(DisplayFolder);
            }

            // load larger images
            loader = new AsyncOverViewImageLoader(OverviewDisplay_File.shadowImageQ);
            loader.RunAsync();
        }

        private void FindImagesInCollection(string collectionPath, Collection<string> result)
        {
            try
            {
                // Prevent scanning its own data store.
                if (collectionPath.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                    return;

                System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(collectionPath);

                //JAL 20150329
                if (dirInfo.Name.StartsWith(".")) return;

                foreach (System.IO.FileInfo info in dirInfo.GetFiles())
                    if (AR.GalleryConfig.IsImage(info.Extension))
                    {
                        result.Add(info.FullName);
                        break;
                    }

                foreach (System.IO.DirectoryInfo info in dirInfo.GetDirectories())
                    FindImagesInCollection(info.FullName, result);
            }
            catch { }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }

        #region Regular Button Actions

        void MoveForwardButtonClicked(object sender, RoutedEventArgs e)
        {
            RepeatButton button = (RepeatButton)sender;
            OverviewDisplay_Folder imageFolder = (OverviewDisplay_Folder)button.DataContext;
            _panel3D[imageFolder.FolderNumber].MoveItems(1, true, ANIMATION_LENGTH);
        }

        void MoveBackButtonClicked(object sender, RoutedEventArgs e)
        {
            RepeatButton button = (RepeatButton)sender;
            OverviewDisplay_Folder imageFolder = (OverviewDisplay_Folder)button.DataContext;
            _panel3D[imageFolder.FolderNumber].MoveItems(1, false, ANIMATION_LENGTH);
        }

        private void Panel3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Panel3D panel = (Panel3D)sender;
            OverviewDisplay_Folder imageFolder = (OverviewDisplay_Folder)panel.DataContext;
            _panel3D[imageFolder.FolderNumber].MoveItems(1, true, ANIMATION_LENGTH);
        }

        private void ViewCollection_Click(object sender, RoutedEventArgs e)
        {
            Button element = (Button)sender;
            OverviewDisplay_Folder data = (OverviewDisplay_Folder)element.DataContext;
            AR.Window_PhotoGallery.Show();
            AR.Window_PhotoGallery.ShowCollection(new System.IO.FileInfo(data.Path).FullName);
            this.Hide();
        }

        private void BackToGallery_Click(object sender, RoutedEventArgs e)
        {
            AR.Window_PhotoGallery.Show();
            AR.Window_PhotoGallery.ShowHome();
            this.Hide();
        }

        #endregion

        #region Image Botton Actions

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            Button element = (Button)sender;
            OverviewDisplay_File data = (OverviewDisplay_File)element.DataContext;
            AR.Window_PhotoGallery.Show();
            AR.Window_PhotoGallery.ShowFolder(new System.IO.FileInfo(data.Path).DirectoryName);
            this.Hide();
        }

        private Brush tempBorderBrush;
        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            tempBorderBrush = (sender as Border).BorderBrush;
            System.Windows.Media.Color tempC = System.Windows.Media.Color.FromArgb(255, 0, 150, 250);
            (sender as Border).BorderBrush = new SolidColorBrush(tempC);
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Border).BorderBrush = tempBorderBrush;
        }

        #endregion

        #region Async Loader

        AsyncOverViewImageLoader loader;
        private class AsyncOverViewImageLoader
        {
            private System.Collections.Generic.Queue<OverviewDisplay_File> _tasks;
            private System.ComponentModel.BackgroundWorker _backgroundWorker;
            private bool _isCanceled;

            public AsyncOverViewImageLoader(System.Collections.Generic.Queue<OverviewDisplay_File> tasks)
            {
                this._tasks = tasks;
                this._isCanceled = false;

                this._backgroundWorker = new BackgroundWorker();
                this._backgroundWorker.DoWork += new DoWorkEventHandler(_backgroundWorker_DoWork);
                this._backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_backgroundWorker_RunWorkerCompleted);
            }

            public void CancelAsync()
            {
                this._isCanceled = true;
            }
            public void ClearQ()
            {
                this._tasks.Clear();
            }

            public void RunAsync()
            {
                ProcessNextItem();
            }

            private void ProcessNextItem()
            {
                if (_tasks.Count > 0)
                    this._backgroundWorker.RunWorkerAsync();
            }

            void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
            {
                try
                {
                    //Thread.Sleep(500);
                    OverviewDisplay_File tempTask = this._tasks.Dequeue();
                    tempTask.Resize();
                    e.Result = tempTask;
                }
                catch (Exception ex) { ex.ToString(); }
            }

            void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                try
                {
                    if (this._isCanceled == false && e.Error == null)
                        ProcessNextItem();
                }
                catch (Exception ex) { ex.ToString(); }
            }
        }

        #endregion
        
        #region Not Used From Sample

        void PageForwardButtonClicked(object sender, RoutedEventArgs e)
        {
            RepeatButton button = (RepeatButton)sender;
            OverviewDisplay_Folder imageFolder = (OverviewDisplay_Folder)button.DataContext;
            _panel3D[imageFolder.FolderNumber].MoveItems(3, true, ANIMATION_LENGTH);
        }

        void PageBackButtonClicked(object sender, RoutedEventArgs e)
        {
            RepeatButton button = (RepeatButton)sender;
            OverviewDisplay_Folder imageFolder = (OverviewDisplay_Folder)button.DataContext;
            _panel3D[imageFolder.FolderNumber].MoveItems(3, false, ANIMATION_LENGTH);
        }

        #endregion
    }
}
