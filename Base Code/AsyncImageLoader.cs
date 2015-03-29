using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace JMC_Photo_Gallery
{
    public class AsyncImageLoader
    {
        private volatile bool _isCanceled;
        private BackgroundWorker _backgroundWorker;
        private Queue<ImageCollection> _tasks;
        private int _imageCount = 0;
        private bool _runDeep = false;
        private System.Windows.Threading.Dispatcher _dispather;
        private Collection<ImageFile> _addImages = new Collection<ImageFile>();
        private Collection<ImageFile> _delImages = new Collection<ImageFile>();
        private Window_Processing _processingWindow;

        public AsyncImageLoader(Queue<ImageCollection> tasks, int imageCount, bool runDeep, System.Windows.Threading.Dispatcher dispather)
        {
            this._tasks = tasks;
            this._imageCount = imageCount;
            this._runDeep = runDeep;
            this._dispather = dispather;
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
            lock (this._tasks)
                this._tasks.Clear();
        }

        public void RunAsync()
        {
            _processingWindow = new Window_Processing();
            _processingWindow.Show();
            ProcessNextItem();
        }

        private void ProcessNextItem()
        {
            // Need this "if" statement to end this thread. 
            lock (this._tasks)
                if (this._tasks.Count > 0)
                {
                    this._backgroundWorker.RunWorkerAsync();
                }
                else
                {
                    Thread.Sleep(1000);
                    _processingWindow.Dispatcher.Invoke(new delegate_Void(_processingWindow.Close));
                    return;
                }
        }

        // delegate for the next mothod.
        public delegate void delegate_Void();
        public delegate void delegate_Void_ImageFile(ImageFile[] imageFile);

        void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ImageCollection tempTask;
            lock (this._tasks)
                if (this._tasks.Count > 0)
                    tempTask = this._tasks.Dequeue();
                else
                    return;

            // Create all images at Background.
            _addImages.Clear();
            _delImages.Clear();
            AutoAdd(tempTask, new System.IO.DirectoryInfo(tempTask.CollectionPath));

            // Invoke to add images to display
            if (_addImages.Count > 0)
            {
                ImageFile[] images2 = new ImageFile[_addImages.Count];
                _addImages.CopyTo(images2, 0);
                delegate_Void_ImageFile myD1 = new delegate_Void_ImageFile(tempTask.Add);
                _dispather.Invoke(myD1, new object[1] { images2 });
            }

            // Invoke to del images to display
            if (_delImages.Count > 0)
            {
                ImageFile[] images2 = new ImageFile[_delImages.Count];
                _delImages.CopyTo(images2, 0);
                delegate_Void_ImageFile myD1 = new delegate_Void_ImageFile(tempTask.Remove);
                _dispather.Invoke(myD1, new object[1] { images2 });
            }


            e.Result = tempTask;
        }

        private void AutoAdd(ImageCollection addTo, System.IO.DirectoryInfo dir)
        {
            try
            {
                // Prevent scanning its own data store.
                if (dir.FullName.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                    return;
                //JAL 20150329
                if (dir.Name.StartsWith(".")) return;

                // Get Total Images of the Directory
                Collection<string> imagesInDir = new Collection<string>();
                foreach (System.IO.FileInfo file in dir.GetFiles())
                    if (AR.GalleryConfig.IsImage(file.Extension))
                        imagesInDir.Add(file.FullName);
                int totalImagesInDir = imagesInDir.Count;

                // Get Total Images of the Visual Tree
                int totalImagesInVisualTree = addTo.CountInDir(dir.FullName);

                // Get desired image count
                int wantImageCount = _imageCount;
                if (wantImageCount > 10)
                    wantImageCount = (int)(totalImagesInDir * wantImageCount / 100);    // apply percentage
                wantImageCount = Math.Max(1, wantImageCount);                           // at least one image
                wantImageCount = Math.Min(totalImagesInDir, wantImageCount);            // max at total images in directory

                // Add or Remove based on desired image count
                if (totalImagesInVisualTree < wantImageCount)
                {
                    for (int i = totalImagesInVisualTree; i < wantImageCount; i++)
                        _addImages.Add(new ImageFile(imagesInDir[i]));
                }
                else if (totalImagesInVisualTree > wantImageCount)
                {
                    for (int i = wantImageCount; i < totalImagesInVisualTree; i++)
                        _delImages.Add(new ImageFile(imagesInDir[i]));
                }

                // Go Deeper
                if (_runDeep)
                    foreach (System.IO.DirectoryInfo sub in dir.GetDirectories())
                        AutoAdd(addTo, sub);
            }
            catch { }
        }

        void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this._isCanceled == false && e.Error == null)
                ProcessNextItem();
        }
    }
}
