using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel; //INotifyPropertyChanged

namespace JMC_Photo_Gallery
{
    // Wish to consolidate with former ImageFile with the MaxWidth(ViewWidth) value
    // But unable to do so when the MaxWidth targets different value
    // And I still want MaxWidth to be static for ease to managment.
    public class ImageFile_SS : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public ImageFile_SS() { }
        public ImageFile_SS(string path, string thumbPath)
        {
            // Set paths
            Path = path; ThumbPath = thumbPath;

            // Set Image Pixel Size
            BitmapImage tempBI_size;
            try
            {
                tempBI_size = new BitmapImage(new Uri(path));
                _pathPixelWidth = tempBI_size.PixelWidth;
                _pathPixelHeight = tempBI_size.PixelHeight;
            }
            catch (Exception ex) { ex.ToString(); }
            try
            {
                tempBI_size = new BitmapImage(new Uri(thumbPath));
                _thumbPathPixelWidth = tempBI_size.PixelWidth;
                _thumbPathPixelHeight = tempBI_size.PixelHeight;
            }
            catch (Exception ex) { ex.ToString(); }

            // Set Max Width and Allowed Width
            SetMaxWidthAndHeight(ViewWidth, ViewHeight);
            LoadImage();
        }

        public override string ToString()
        {
            String result = "<ImageFile>\r\n";
            result += "<Path>" + Path + "</Path>\r\n";
            result += "<Width>" + Width + "</Width>\r\n";
            result += "<Height>" + Height + "</Height>\r\n";
            result += "<Rotation>" + Rotation + "</Rotation>\r\n";
            result += "<Scale>" + Scale + "</Scale>\r\n";
            result += "<LocationX>" + LocationX + "</LocationX>\r\n";
            result += "<LocationY>" + LocationY + "</LocationY>\r\n";
            result += "</ImageFile>\r\n";
            return result;
        }

        private String _path;
        public String Path { get { return _path; } set { _path = value; _uri = new Uri(_path); } }
        private String _thumbPath;
        public String ThumbPath { get { return _thumbPath; } set { _thumbPath = value; } }
        private Uri _uri;
        public Uri Uri { get { return _uri; } }
        private BitmapImage _image;
        public BitmapImage Image { get { return _image; } }

        public void LoadImage()
        {
            try
            {
                if (_image != null && _image.PixelWidth == Width && _image.PixelHeight == Height)
                    return;

                if (_path != "")
                {
                    //RandomProperties();
                    string tempPath = _path;
                    if (Width < _thumbPathPixelWidth || Height < _thumbPathPixelHeight)
                        tempPath = ThumbPath;

                    BitmapImage tempImage = new BitmapImage();
                    tempImage.BeginInit();
                    tempImage.UriSource = new Uri(tempPath);
                    tempImage.DecodePixelWidth = Width;
                    tempImage.CacheOption = BitmapCacheOption.OnLoad;
                    tempImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    tempImage.EndInit();
                    tempImage.Freeze();
                    _image = tempImage;

                    this.NotifyPropertyChanged("Image");
                }
            }
            catch (Exception e) { e.ToString(); Console.WriteLine(_path + " Failed to Load SlideShow Image.\n"); }
        }

        public void UnloadImage()
        {
            _image = null;
            this.NotifyPropertyChanged("Image");
        }

        #region Width and Height

        private int _pathPixelWidth, _pathPixelHeight;
        private int _thumbPathPixelWidth, _thumbPathPixelHeight;
        private int _maxWidth = -1, _maxHeight = -1;
        private int _allowedWidth = -1, _allowedHeight = -1;
        public void SetMaxWidthAndHeight(int maxWidth, int maxHeight)
        {
            _maxWidth = maxWidth;
            if (_maxWidth > -1 && _pathPixelWidth > _maxWidth)
                _allowedWidth = _maxWidth;
            else
                _allowedWidth = _pathPixelWidth;

            _maxHeight = maxHeight;
            if (_maxHeight > -1 && _pathPixelHeight > _maxHeight)
                _allowedHeight = _maxHeight;
            else
                _allowedHeight = _pathPixelHeight;

            // Make Sure Ratio Is Correct
            if (_allowedWidth != _pathPixelWidth || _allowedHeight != _pathPixelHeight)
            {
                double widthRatio = (double)_allowedWidth / _pathPixelWidth;
                double heightRatio = (double)_allowedHeight / _pathPixelHeight;
                if (heightRatio < widthRatio)
                    _allowedWidth = (int)(_pathPixelWidth * heightRatio);
                else if (widthRatio < heightRatio)
                    _allowedHeight = (int)(_pathPixelHeight * widthRatio);
            }
        }
        public int Width { get { return (int)(_allowedWidth * Scale); } }
        public int Height { get { return (int)(_allowedHeight * Scale); } }

        #endregion

        #region Random Processor

        private double _scale = 1; // this means 100%
        private int _rotation = 0;
        private int _locationX = 0, _locationY = 0;
        public double Scale { get { return _scale; } set { _scale = value; this.NotifyPropertyChanged("Scale"); } }
        public int Rotation { get { return _rotation; } set { _rotation = value; this.NotifyPropertyChanged("Rotation"); } }
        public int LocationX { get { return _locationX; } set { _locationX = value; this.NotifyPropertyChanged("LocationX"); } }
        public int LocationY { get { return _locationY; } set { _locationY = value; this.NotifyPropertyChanged("LocationY"); } }
        public int BorderThickness { get { return BorderThicknessG; } set { BorderThicknessG = value; this.NotifyPropertyChanged("BorderThickness"); } }

        private static Random _random = new Random();
        public static int MinScale = 100, MaxScale = 100;
        public static int MinRotation = 0, MaxRotation = 0; // Clock Wise
        public static bool LocationRandom = true;
        public static int ViewWidth = 0, ViewHeight = 0;
        public static int ViewExpandThickness = 0; // negative is shink
        public static int BorderThicknessG = 0;

        public void RandomProperties() // Resize, Rotation, Location
        {
            //System.Threading.Thread.Sleep(50); // Minimum 10, otherwise won't be so random.
            BorderThickness = BorderThicknessG;
            double tempScale = (double)(_random.Next(MinScale, MaxScale)) / 100;
            double tempMaxWidth = _maxWidth * tempScale;
            double tempMaxHeight = _maxHeight * tempScale;
            if (_allowedWidth > tempMaxWidth || _allowedHeight > tempMaxHeight)
                Scale = Math.Min(tempMaxWidth / _allowedWidth, tempMaxHeight / _allowedHeight);
            else
                Scale = 1; // Scale to 1 if the original is small enough already
            Rotation = _random.Next(MinRotation, MaxRotation);

            if (LocationRandom)
            {
                int tempViewWidth = ViewWidth + ViewExpandThickness * 2 - BorderThickness * 2;
                int tempViewHeight = ViewHeight + ViewExpandThickness * 2 - BorderThickness * 2;
                _locationX = ViewExpandThickness * -1;
                _locationY = ViewExpandThickness * -1;
                if (tempViewWidth - Width > 0)
                    _locationX += _random.Next(tempViewWidth - Width);
                if (tempViewHeight - Height > 0)
                    _locationY += _random.Next(tempViewHeight - Height);
                LocationX = _locationX;
                LocationY = _locationY;
            }
            else
            {
                LocationX = (int)(ViewWidth - Width) / 2;
                LocationY = (int)(ViewHeight - Height) / 2;
            }
            this.NotifyPropertyChanged("Width");
            this.NotifyPropertyChanged("Height");
            // Notify Scale
            // Notify Rotation
            // Notify LocationX LocationY
        }

        #endregion
    }

    public class ImageFolder_SS
    {
        public ObservableCollection<ImageFile_SS> _images = new ObservableCollection<ImageFile_SS>();
        public ObservableCollection<ImageFile_SS> Images { get { return _images; } }

        public override string ToString()
        {
            String result = "<Folder>\r\n";
            foreach (ImageFile_SS tempImage in _images)
                result += tempImage.ToString() + "\r\n";
            result += "</Folder>\r\n";
            return result;
        }
    }
}
