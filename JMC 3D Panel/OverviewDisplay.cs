using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel; //INotifyPropertyChanged

namespace JMC_Photo_Gallery
{
    class OverviewDisplay_File : INotifyPropertyChanged
    {
        public static ICanGetThumbPath s_thumbPathMaker;
        public static System.Collections.Generic.Queue<OverviewDisplay_File> shadowImageQ = new Queue<OverviewDisplay_File>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public OverviewDisplay_File(String filePath, int maxWidthOrHeight, bool returnShadow)
        {
            _uri = new Uri(filePath);
            _maxWidthOrHeight = maxWidthOrHeight;

            if (returnShadow)
            {
                //mini BitmapImage
                try
                {
                    BitmapImage tempImage = new BitmapImage();
                    tempImage.BeginInit();
                    tempImage.UriSource = _uri;
                    tempImage.SourceRect = new System.Windows.Int32Rect(0, 0, 1, 1);
                    tempImage.CacheOption = BitmapCacheOption.OnLoad;
                    tempImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    tempImage.DecodePixelWidth = _maxWidthOrHeight;
                    tempImage.EndInit();
                    tempImage.Freeze();
                    _image = tempImage;
                }
                catch (Exception ex) { ex.ToString(); Console.WriteLine(_uri.AbsolutePath + " Failed to returnShadow 3D Home Image.\n"); }
                shadowImageQ.Enqueue(this);
            }
            else
                Resize();
        }

        public void Resize()
        {
            try
            {
                lock (thisLock)
                {
                    Uri tempURI = _uri;
                    if (s_thumbPathMaker != null)
                    {
                        string path = s_thumbPathMaker.GetThumbPath(_uri.LocalPath, true);
                        if(path != null)
                            tempURI = new Uri(path);
                    }

                    BitmapImage tempImage = new BitmapImage();
                    tempImage.BeginInit();
                    tempImage.UriSource = tempURI;
                    BitmapImage tempBI_size = new BitmapImage(tempURI);
                    int extraSideThickness = Math.Abs(tempBI_size.PixelHeight - tempBI_size.PixelWidth) / 2;
                    if (tempBI_size.PixelWidth >= tempBI_size.PixelHeight)
                    {
                        // Crop, thus, the setting is backward.
                        tempImage.DecodePixelHeight = _maxWidthOrHeight;
                        tempImage.SourceRect = new System.Windows.Int32Rect(extraSideThickness, 0, tempBI_size.PixelHeight, tempBI_size.PixelHeight);
                    }
                    else
                    {
                        tempImage.DecodePixelWidth = _maxWidthOrHeight;
                        tempImage.SourceRect = new System.Windows.Int32Rect(0, extraSideThickness, tempBI_size.PixelWidth, tempBI_size.PixelWidth);
                    }
                    tempImage.CacheOption = BitmapCacheOption.OnLoad;
                    tempImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    tempImage.EndInit();
                    tempImage.Freeze();
                    _image = tempImage;
                }
                this.NotifyPropertyChanged("Image");
                this.NotifyPropertyChanged("Width");
                this.NotifyPropertyChanged("Height");
            }
            catch (Exception ex) { ex.ToString(); Console.WriteLine(_uri.AbsolutePath + " Failed to Resize 3D Home Image.\n"); }
        }

        private Object thisLock = new Object();
        private int _maxWidthOrHeight;
        public int Width { get { return _image.PixelWidth; } }
        public int Height { get { return _image.PixelHeight; } }

        private Uri _uri;
        public Uri Uri { get { return _uri; } }
        public string Path { get { return _uri.LocalPath; } }
        private BitmapImage _image;
        public BitmapImage Image
        {
            get
            {
                lock (thisLock)
                    return _image;
            }
        }
    }

    class OverviewDisplay_Page
    {
        private ObservableCollection<OverviewDisplay_File> _page = new ObservableCollection<OverviewDisplay_File>();
        public ObservableCollection<OverviewDisplay_File> Page { get { return _page; } }
        public int PageWidth
        {
            get
            {
                if (_page.Count > 0)
                    return _page[0].Width * (int)Math.Ceiling(Math.Sqrt(_page.Count));
                else
                    return 0;
            }
        }
        public int PageHeight
        {
            get
            {
                if (_page.Count > 0)
                    return _page[0].Height * (int)Math.Ceiling(Math.Sqrt(_page.Count));
                else
                    return 0;
            }
        }
    }

    class OverviewDisplay_Folder
    {
        public OverviewDisplay_Folder(String path, int folderNumber, int folderWidth, int minThumbWidth)
        {
            _path = path;
            _folderNumber = folderNumber;
            _folderWidth = folderWidth;
            _minThumbWidth = minThumbWidth;
        }
        private ObservableCollection<OverviewDisplay_Page> _folder = new ObservableCollection<OverviewDisplay_Page>();
        public ObservableCollection<OverviewDisplay_Page> Folder { get { return _folder; } }
        public void setupList(String[] imageList)
        {
            // Determin the optimal gridSize and pageCount based on maxGridWidth and minThumbWidth
            int maxRowCount = _folderWidth / _minThumbWidth;
            int maxCount = maxRowCount * maxRowCount;
            int fileCount = imageList.Length;
            int thumbPageCount = (int)Math.Ceiling((double)fileCount / maxCount);
            thumbPageCount = Math.Max(thumbPageCount, 3); // minimun of 3 pages.
            int optimalCount = (int)Math.Ceiling((double)fileCount / thumbPageCount);
            int optimalRowCount = (int)Math.Ceiling((double)Math.Sqrt(optimalCount));
            optimalCount = optimalRowCount * optimalRowCount;

            // Setup Pages
            int remaining = fileCount;
            int currentPageCount;
            int optimalThumbWidthPerPage;
            int index = 0;

            while (remaining > 0)
            {
                // Determin page width and image count of such page.
                if (remaining >= optimalCount)
                    currentPageCount = optimalCount;
                else
                    currentPageCount = remaining;

                optimalThumbWidthPerPage = _folderWidth / (int)Math.Ceiling((double)Math.Sqrt(currentPageCount));
                optimalThumbWidthPerPage = (int)((double)optimalThumbWidthPerPage * 0.8); // this is to adjust the 3D panel scaling.
                remaining -= currentPageCount;

                // Make pages
                OverviewDisplay_Page tempPage = new OverviewDisplay_Page();
                for (int i = 0; i < currentPageCount; i++, index++)
                    tempPage.Page.Add(new OverviewDisplay_File(imageList[index], optimalThumbWidthPerPage, true));
                _folder.Add(tempPage);

            }
        }
        private String _path;
        private int _folderNumber;
        private int _folderWidth; // This is the 3D Panel Width, 3D Panel only resize by width.
        private int _minThumbWidth;
        public String Path { set { _path = value; } get { return _path; } }
        public int FolderNumber { set { _folderNumber = value; } get { return _folderNumber; } }
        public int FolderWidth { set { _folderWidth = value; } get { return _folderWidth; } }
        public int MinThumbWidth { set { _minThumbWidth = value; } get { return _minThumbWidth; } }
    }

    class OverviewDisplay_Collections
    {
        private ObservableCollection<OverviewDisplay_Folder> _collections = new ObservableCollection<OverviewDisplay_Folder>();
        public ObservableCollection<OverviewDisplay_Folder> Collections { get { return _collections; } }
    }

}
