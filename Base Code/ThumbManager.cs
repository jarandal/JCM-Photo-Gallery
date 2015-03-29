using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;

namespace JMC_Photo_Gallery
{
    // Manage Thumb Database.
    // Will never alter the original files.
    public class ThumbManager
    {
        private ICanGetThumbPath _ThumbPathMaker = null;
        public int MiniThumbWidth = 75;
        public int MiniThumbHeight = 75;
        public int ThumbWidth = 600;
        public int ThumbHeight = 600;
        private string _ImageSupport = ".bmp;.gif;.jpg;.jpe;.jpeg;.jfif;.png;.tif;.tiff";
        private string[] _ImageSupports = null;
        public string ImageSupport
        {
            get { return _ImageSupport; }
            set
            {
                _ImageSupport = value.ToLower();
                _ImageSupports = _ImageSupport.Split(';');
            }
        }

        public ThumbManager(ICanGetThumbPath ThumbPathMaker)
        {
            _ThumbPathMaker = ThumbPathMaker;
            ImageSupport = ImageSupport; // init _ImageSupports[]
        }

        #region CreateThumb

        public void SaveThumb(string path)
        {
            if (!IsSupported(path))                                     // not supported
                return;
            if (_ThumbPathMaker.GetThumbPath(path, true) != null)       // already exist
                return;
            if (_ThumbPathMaker.GetMiniThumbPath(path, true) != null)   // already exist
                return;
            
            string supposedThumbPath = _ThumbPathMaker.GetThumbPath(path, false);
            string supposedMiniPath = _ThumbPathMaker.GetMiniThumbPath(path, false);
            if (supposedThumbPath == null)                              // unable to save
                return;
            if (supposedMiniPath == null)                               // unable to save
                return;

            try
            {
                System.Drawing.Image imgPhoto = new System.Drawing.Bitmap(path);
                System.Drawing.Image thumbImage = Resize(imgPhoto, ThumbWidth, ThumbHeight);
                System.Drawing.Image miniThumbImage = Resize(thumbImage, MiniThumbWidth, MiniThumbHeight);
                CreateParentFolder(supposedThumbPath);
                CreateParentFolder(supposedMiniPath);

                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                System.Drawing.Imaging.Encoder myEncoder =System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 25L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                thumbImage.Save(supposedThumbPath, jpgEncoder, myEncoderParameters);
                miniThumbImage.Save(supposedMiniPath, jpgEncoder, myEncoderParameters);
                thumbImage.Dispose();
                miniThumbImage.Dispose();
            }
            catch { }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private System.Drawing.Image Resize(System.Drawing.Image imgPhoto, int Width, int Height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            double nPercent = Math.Min((double)Width / sourceWidth, (double)Height / sourceHeight);
            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            System.Drawing.Bitmap canvas = new System.Drawing.Bitmap(destWidth, destHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            canvas.SetResolution(96, 96);
            System.Drawing.Graphics pen = System.Drawing.Graphics.FromImage(canvas);
            pen.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            pen.DrawImage(imgPhoto,
                new System.Drawing.Rectangle(0, 0, destWidth, destHeight),
                new System.Drawing.Rectangle(0, 0, sourceWidth, sourceHeight),
                System.Drawing.GraphicsUnit.Pixel);

            pen.Dispose();
            return canvas;
        }

        private void CreateParentFolder(string path)
        {
            FileInfo info = new FileInfo(path);
            if (!info.Directory.Exists)
                info.Directory.Create();
        }

        private bool IsSupported(string path)
        {
            try
            {
                string ext = new FileInfo(path).Extension.ToLower();
                foreach (string item in _ImageSupports)
                {
                    if (ext == item)
                        return true;
                }
                return false;
            }
            catch { return false; }
        }

        #endregion
    }

    public class ThumbManagerAsync : ThumbManager
    {
        private Queue<string> _queue = new Queue<string>();
        private Thread _thread;

        public ThumbManagerAsync(ICanGetThumbPath ThumbPathMaker)
            : base(ThumbPathMaker)
        {
        }

        #region Methods

        public new void SaveThumb(string path)
        {
            lock (_queue)
                _queue.Enqueue(path);
        }

        private void Processor()
        {
            string path;
            while (true)
            {
                try
                {
                    path = null;
                    lock (_queue)
                        if (_queue.Count > 0)
                            path = _queue.Dequeue();

                    if (path == null)
                        Thread.Sleep(500);
                    else
                        base.SaveThumb(path);
                }
                catch { }
            }
        }

        public void StartThread()
        {
            if (_thread == null || !_thread.IsAlive)
            {
                _thread = new Thread(Processor);
                _thread.Priority = ThreadPriority.Lowest;
                _thread.Start();
            }
            while (!_thread.IsAlive) { }
        }

        public void EndThread()
        {
            try
            {
                _thread.Abort();
            }
            catch { }
        }

        public void ClearQueue()
        {
            lock (_queue)
                _queue.Clear();
        }

        #endregion

    }

    public class ThumbManagerAsyncRunner : ThumbManagerAsync
    {
        public ThumbManagerAsyncRunner(ICanGetThumbPath ThumbPathMaker)
            : base(ThumbPathMaker)
        {
        }

        public void AddRoot(string rootPath)
        {
            try
            {
                // Prevent scanning its own data store.
                if (rootPath.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                    return;
                
                DirectoryInfo dir = new DirectoryInfo(rootPath);

                //JAL 20150329
                if (dir.Name.StartsWith(".")) return;

                foreach (FileInfo file in dir.GetFiles())
                    SaveThumb(file.FullName);
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                    AddRoot(subDir.FullName);
            }
            catch { }
        }
    }
}
