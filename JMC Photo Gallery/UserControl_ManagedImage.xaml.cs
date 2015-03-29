using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Threading;
using System.IO;

namespace JMC_Photo_Gallery
{
    /// <summary>
    /// Interaction logic for UserControl_ManagedImage.xaml
    /// </summary>
    public partial class UserControl_ManagedImage : UserControl
    {
        #region All Threads and Static Queues and Delegate for threaded handling.

        // To unsure one object is created and unloaded one at the time for create/abort threads.
        private static object s_classLockObj = new object();

        // Queues
        // Remember, we don't care visitibily. We should assume it is visible.
        // Collapsed control will cause slow down in general regardless we use this usercontrol or not.
        private static Collection<ScrollViewer> s_hostScrollViewers = new Collection<ScrollViewer>();
        private static Dictionary<ScrollViewer, ScrollViewerInfo> s_hostScrollViewerInfos = new Dictionary<ScrollViewer, ScrollViewerInfo>();
        private static Collection<UserControl_ManagedImage> s_miniThumbQueue = new Collection<UserControl_ManagedImage>();
        private static Collection<UserControl_ManagedImage> s_priorityThumbQueue = new Collection<UserControl_ManagedImage>();
        private static Collection<UserControl_ManagedImage> s_releaseThumbQueue = new Collection<UserControl_ManagedImage>();

        private static Thread s_miniThumbThread;
        private static Thread s_priorityThumbThread;
        private static Thread s_releaseThumbThread;
        private static Thread s_isInTheViewThread;

        // =============== Support Struct =============== 
        #region Support Struct
        // =============== Support Struct =============== 

        private struct ScrollViewerInfo
        {
            public double ScrollableHeight;
            public double ScrollableWidth;
            public double VerticalOffset;
            public double HorizontalOffset;

            public ScrollViewerInfo(ScrollViewer sv)
            {
                this.ScrollableHeight = sv.ScrollableHeight;
                this.ScrollableWidth = sv.ScrollableWidth;
                this.VerticalOffset = sv.VerticalOffset;
                this.HorizontalOffset = sv.HorizontalOffset;
            }

            public bool IsSameInfo(ScrollViewer sv)
            {
                if (this.ScrollableHeight != sv.ScrollableHeight)
                    return false;
                if (this.ScrollableWidth != sv.ScrollableWidth)
                    return false;
                if (this.VerticalOffset != sv.VerticalOffset)
                    return false;
                if (this.HorizontalOffset != sv.HorizontalOffset)
                    return false;

                return true;
            }
        }

        #endregion
        
        // =============== Activate/Deactivate Threads =============== 
        #region Activate/Deactivate Threads
        // =============== Activate/Deactivate Threads =============== 

        public static void s_ActivateThreads()
        {
            if (s_miniThumbThread == null || !s_miniThumbThread.IsAlive)
            {
                s_miniThumbThread = new Thread(s_miniThumbProcessor);
                s_miniThumbThread.Priority = ThreadPriority.Normal;
                s_miniThumbThread.Start();
            }
            if (s_priorityThumbThread == null || !s_priorityThumbThread.IsAlive)
            {
                s_priorityThumbThread = new Thread(s_priorityThumbProcessor);
                s_priorityThumbThread.Priority = ThreadPriority.BelowNormal;
                s_priorityThumbThread.Start();
            }
            if (s_releaseThumbThread == null || !s_releaseThumbThread.IsAlive)
            {
                s_releaseThumbThread = new Thread(s_releaseThumbProcessor);
                s_releaseThumbThread.Priority = ThreadPriority.Lowest;
                s_releaseThumbThread.Start();
            }
            if (s_isInTheViewThread == null || !s_isInTheViewThread.IsAlive)
            {
                s_isInTheViewThread = new Thread(s_isInTheViewProcessor);
                s_isInTheViewThread.Priority = ThreadPriority.Lowest;
                s_isInTheViewThread.Start();
            }
            while (!s_miniThumbThread.IsAlive || !s_priorityThumbThread.IsAlive ||
                !s_releaseThumbThread.IsAlive || !s_isInTheViewThread.IsAlive)
            {
                Console.WriteLine("Waiting thread: " +
                    s_miniThumbThread.IsAlive.ToString() + " " +
                    s_priorityThumbThread.IsAlive.ToString() + " " +
                    s_releaseThumbThread.IsAlive.ToString() + " " +
                    s_isInTheViewThread.IsAlive.ToString() + " ");
            }
        }

        public static void s_DeactivateThreads()
        {
            try
            {
                s_miniThumbThread.Abort();
            }
            catch { }
            try
            {
                s_priorityThumbThread.Abort();
            }
            catch { }
            try
            {
                s_releaseThumbThread.Abort();
            }
            catch { }
            try
            {
                s_isInTheViewThread.Abort();
            }
            catch { }
        }

        #endregion

        // =============== Delegates =============== 
        #region Delegates
        // =============== Delegates =============== 

        // delegate is used to invoke those method from Owner Thread of the object instead of calling thread.
        public delegate void delegate_Void();
        public delegate void delegate_Void_BI(BitmapImage obj);
        public delegate void delegate_Void_BI_int(BitmapImage obj, int pixelWidth);

        private void ReleaseMini()
        {
            x_Grid.Background = System.Windows.Media.Brushes.Gray;
            x_ImageThumb.Source = null;
        }

        private void SetMini(BitmapImage obj)
        {
            x_Grid.Background = System.Windows.Media.Brushes.Transparent;
            x_ImageThumb.Source = obj;
        }

        private void ReleaseImage()
        {
            x_Image.Source = null;
            _decodedPiexlWidth = 0;
        }

        private void SetImage(BitmapImage obj, int decodeWidth)
        {
            x_Image.Source = obj;
            _decodedPiexlWidth = decodeWidth;
        }

        #endregion

        // =============== Delegates s_isInTheViewProcessor =============== 
        #region Delegates s_isInTheViewProcessor
        // =============== Delegates s_isInTheViewProcessor =============== 

        public delegate void delegate_Void_SV(ScrollViewer topScrollViewer);

        private static void s_ChildInView_Driver(ScrollViewer sv)
        {
            if (s_hostScrollViewerInfos.ContainsKey(sv))
            {
                if (s_hostScrollViewerInfos[sv].IsSameInfo(sv))
                    return;
                else
                    s_hostScrollViewerInfos[sv] = new ScrollViewerInfo(sv);
            }
            else
                s_hostScrollViewerInfos.Add(sv, new ScrollViewerInfo(sv));

            // Only works for single ScrollViewer.
            // If you want to support multiple ScrollViewers,
            // Please implement Dictionary<ScrollViewers, Collection<UserControl_ManagedImage>>
            // Or maybe a special ScrollViewerThread class to have their own thread per SV.
            Console.WriteLine("Running s_ChildInView(sv, sv).");
            lock (s_classLockObj)
            {
                s_priorityThumbQueue.Clear();
                s_releaseThumbQueue.Clear();
            }

            s_ChildInView(sv, sv);
        }

        private static void s_ChildInView(ScrollViewer sv, DependencyObject currentParent)
        {
            if (currentParent.GetType() == typeof(UserControl_ManagedImage))
            {
                UserControl_ManagedImage thisObj = (UserControl_ManagedImage)currentParent;
                if (thisObj.x_MainControl.ActualWidth <= c_miniThumbWidth)
                {
                    thisObj.ReleaseImage();
                }
                else if (thisObj._decodedPiexlWidth != thisObj.x_MainControl.ActualWidth)
                {
                    lock (s_classLockObj)
                        if (!s_priorityThumbQueue.Contains(thisObj))
                            s_priorityThumbQueue.Add(thisObj);
                }
            }
            else
            {
                // this will avoid further in-view testing by knowing the rest of images in the
                // visual tree will not be in-view by simple logic.
                int statusChanges = 0; // 0 = not in view, 1 = in view, 2 = become not in view.
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(currentParent); i++)
                {
                    DependencyObject childDObj = VisualTreeHelper.GetChild(currentParent, i);
                    if (statusChanges >= 2)
                    {
                        s_DeepRelease(childDObj);
                    }
                    else
                    {
                        if (UIElementInTheView(sv, childDObj))
                        {
                            if (statusChanges == 0)
                                statusChanges++;
                            s_ChildInView(sv, childDObj);
                        }
                        else
                        {
                            if (statusChanges == 1)
                                statusChanges++;
                            s_DeepRelease(childDObj);
                        }
                    }
                }
            }
        }

        private static bool UIElementInTheView(ScrollViewer topScrollViewer, DependencyObject dObj)
        {
            try
            {
                if (!((UIElement)dObj).IsVisible)
                    return false;

                // position of your visual inside the scrollviewer    
                GeneralTransform childTransform = ((UIElement)dObj).TransformToAncestor(topScrollViewer);
                Rect rectangle = childTransform.TransformBounds(new Rect(new Point(0, 0), ((UIElement)dObj).RenderSize));

                //Check if the elements Rect intersects with that of the scrollviewer's
                //Rect result = Rect.Intersect(new Rect(new Point(0, 0), topScrollViewer.RenderSize), rectangle);
                Rect result = Rect.Intersect(new Rect(
                    new Point(topScrollViewer.RenderSize.Width * -4, topScrollViewer.RenderSize.Height * -4),
                    new Point(topScrollViewer.RenderSize.Width * 5, topScrollViewer.RenderSize.Height * 5)), rectangle);

                //if result is Empty then the element is not in view
                if (result == Rect.Empty)
                    return false;
                else
                    //obj is partially Or completely visible
                    return true;
            }
            catch { return false; }
        }

        private static void s_DeepRelease(DependencyObject currentParent)
        {
            if (currentParent.GetType() == typeof(UserControl_ManagedImage))
            {
                UserControl_ManagedImage temp = (UserControl_ManagedImage)currentParent;
                if (temp.x_Image.Source != null)
                    lock (s_classLockObj)
                        if (!s_releaseThumbQueue.Contains(temp))
                            s_releaseThumbQueue.Add(temp);
            }
            else
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(currentParent); i++)
                {
                    DependencyObject childDObj = VisualTreeHelper.GetChild(currentParent, i);
                    s_DeepRelease(childDObj);
                }
            }
        }

        #endregion

        // =============== Processors =============== 
        #region Processors
        // =============== Processors =============== 

        private static void s_miniThumbProcessor()
        {
            UserControl_ManagedImage obj;
            while (true)
            {
                try
                {
                    obj = null;
                    lock (s_miniThumbQueue)
                        if (s_miniThumbQueue.Count > 0)
                        {
                            obj = s_miniThumbQueue[0];
                            s_miniThumbQueue.RemoveAt(0);
                        }

                    if (obj == null)
                        Thread.Sleep(500);
                    else
                    {
                        //Console.WriteLine("making thumb on: " + obj._uri);
                        BitmapImage tempImage = MakeBitmapImage(obj._uri, c_miniThumbWidth);
                        if (tempImage != null)
                        {
                            delegate_Void_BI myD1 = new delegate_Void_BI(obj.SetMini);
                            obj.Dispatcher.Invoke(myD1, new object[1] { tempImage });
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("s_miniThumbProcessor Error: \n" + ex.ToString()); }
            }
        }

        private static void s_priorityThumbProcessor()
        {
            // no need for lock because the entire queue is replaced.
            UserControl_ManagedImage obj;
            while (true)
            {
                try
                {
                    obj = null;
                    if (s_priorityThumbQueue.Count > 0)
                    {
                        obj = s_priorityThumbQueue[0];
                        s_priorityThumbQueue.RemoveAt(0);
                    }

                    if (obj == null)
                        Thread.Sleep(500);
                    else if (obj.x_MainControl.ActualWidth <= c_miniThumbWidth)
                    {
                        delegate_Void myD1 = new delegate_Void(obj.ReleaseImage);
                        obj.Dispatcher.Invoke(myD1);
                    }
                    //else if (obj._decodedPiexlWidth == 0 || obj._decodedPiexlWidth != obj.x_MainControl.ActualWidth)
                    else if (obj._decodedPiexlWidth != obj.x_MainControl.ActualWidth)
                    {
                        //*
                        BitmapImage tempImage = null;
                        tempImage = MakeBitmapImage(obj._uri, (int)obj.x_MainControl.ActualWidth);
                        if (tempImage != null)
                        {
                            //Console.WriteLine("loading: " + obj._uri.AbsolutePath + "\n" + obj._decodedPiexlWidth + " vs " + obj.x_MainControl.ActualWidth);
                            delegate_Void_BI_int myD1 = new delegate_Void_BI_int(obj.SetImage);
                            obj.Dispatcher.Invoke(myD1, new object[2] { tempImage, (int)obj.x_MainControl.ActualWidth });
                        }
                        //*/
                    }
                    //else 
                    //    Console.WriteLine("skip cuz already loaded: " + obj._uri.AbsolutePath + "\n" + obj._decodedPiexlWidth + " vs " + obj.x_MainControl.ActualWidth);
                }
                catch (Exception ex) { Console.WriteLine("s_priorityThumbProcessor Error: \n" + ex.ToString()); }
            }
        }

        private static void s_releaseThumbProcessor()
        {
            UserControl_ManagedImage obj;
            while (true)
            {
                try
                {
                    obj = null;
                    lock (s_releaseThumbQueue)
                        if (s_releaseThumbQueue.Count > 0)
                        {
                            obj = s_releaseThumbQueue[0];
                            s_releaseThumbQueue.RemoveAt(0);
                        }

                    if (obj == null)
                        Thread.Sleep(10000);
                    else
                    {
                        delegate_Void myD1 = new delegate_Void(obj.ReleaseImage);
                        obj.Dispatcher.Invoke(myD1);
                    }
                }
                catch (Exception ex) { Console.WriteLine("s_releaseThumbQueue Error: \n" + ex.ToString()); }
            }
        }

        private static void s_isInTheViewProcessor()
        {
            while (true)
            {
                // currently this is a bad code. I wanted to support multiple viwers,
                // but, need a lot more work on dealing with sync and removing ScrollViewer
                // thus, in reality this code only support ONE ScrollViewer so far.
                foreach (ScrollViewer sv in s_hostScrollViewers)
                {
                    delegate_Void_SV myD1 = new delegate_Void_SV(s_ChildInView_Driver);
                    sv.Dispatcher.Invoke(myD1, new object[] { sv });
                }

                Console.WriteLine("s_isInTheViewProcessor Update: " + s_priorityThumbQueue.Count + " Release: " + s_releaseThumbQueue.Count);
                Thread.Sleep(1000);
            }
        }

        #endregion

        #endregion

        // =============== Member Fields and Handlers =============== 

        public UserControl_ManagedImage()
        {
            InitializeComponent();
        }

        private void x_MainControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ScrollViewer topViewer = null;
                DependencyObject currentElement = VisualTreeHelper.GetParent(this);
                while (true)
                {
                    if (currentElement == null)
                        break;
                    if (currentElement.GetType() == typeof(ScrollViewer))
                        topViewer = (ScrollViewer)currentElement;
                    currentElement = VisualTreeHelper.GetParent(currentElement);
                }

                if (topViewer != null && !s_hostScrollViewers.Contains(topViewer))
                    s_hostScrollViewers.Add(topViewer);
            }
            catch { }
        }

        private void x_MainControl_Unloaded(object sender, RoutedEventArgs e)
        {
            lock (s_miniThumbQueue)
                s_miniThumbQueue.Remove(this);
        }

        private void x_MainControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                lock (s_miniThumbQueue)
                    if (!s_miniThumbQueue.Contains(this))
                        s_miniThumbQueue.Add(this);         // Loading mini
            }
            else
            {
                lock (s_miniThumbQueue)
                    if (s_miniThumbQueue.Contains(this))
                        s_miniThumbQueue.Remove(this);     // Stop Loading mini, needed to let other visible mini loads quicker.

                // And clear mini
                if (this.x_ImageThumb.Source != null)
                    this.ReleaseMini();
            }
        }

        #region Bindable SourceUri

        public Uri SourceUri
        {
            get { return (Uri)GetValue(SourceUriProperty); }
            set { SetValue(SourceUriProperty, value); }
        }

        public static readonly DependencyProperty SourceUriProperty = DependencyProperty.Register(
          "SourceUri", typeof(Uri), typeof(UserControl_ManagedImage),
          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OnSourceUriCallback))
              );

        private static void OnSourceUriCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UserControl_ManagedImage selfObj = (UserControl_ManagedImage)d;
            Uri uri = (Uri)e.NewValue;
            selfObj._uri = uri;

            RoutedEventArgs newargs = new RoutedEventArgs(SourceUriChangedEvent);
            (d as FrameworkElement).RaiseEvent(newargs);
        }

        // Not Really Needed, But Better For Completeness.
        public static readonly RoutedEvent SourceUriChangedEvent = EventManager.RegisterRoutedEvent("SourceUriChangedEvent", RoutingStrategy.Bubble, typeof(DependencyPropertyChangedEventHandler), typeof(UserControl_ManagedImage));

        public event RoutedEventHandler SourceUriChanged
        {
            add { AddHandler(SourceUriChangedEvent, value); }
            remove { RemoveHandler(SourceUriChangedEvent, value); }
        }

        #endregion

        #region Bindable MainWidth and MainHeight

        public static DependencyProperty MainWidthProperty = DependencyProperty.Register(
            "MainWidth", typeof(string), typeof(UserControl_ManagedImage),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OnMainWidthCallback))
            );

        public string MainWidth
        {
            get { return (string)GetValue(MainWidthProperty); }
            set { SetValue(MainWidthProperty, value); }
        }

        private static void OnMainWidthCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UserControl_ManagedImage selfObj = (UserControl_ManagedImage)d;
            string size = (string)e.NewValue;
            double result = 0;
            if (size != null && double.TryParse(size, out result))
                selfObj.x_MainControl.Width = result;
        }

        public static DependencyProperty MainHeightProperty = DependencyProperty.Register(
            "MainHeight", typeof(string), typeof(UserControl_ManagedImage),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OnMainHeightCallback))
            );

        public string MainHeight
        {
            get { return (string)GetValue(MainHeightProperty); }
            set { SetValue(MainHeightProperty, value); }
        }

        private static void OnMainHeightCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UserControl_ManagedImage selfObj = (UserControl_ManagedImage)d;
            string size = (string)e.NewValue;
            double result = 0;
            if (size != null && double.TryParse(size, out result))
                selfObj.x_MainControl.Height = result;
        }

        #endregion

        private const int c_miniThumbWidth = 75;
        private Uri _uri;
        private int _decodedPiexlWidth = 0;
        public static ICanGetThumbPath _thumbPathMaker = null;
        public static ICanGetThumbBitmapImage _thumbBitmapMaker = null;

        private static BitmapImage MakeBitmapImage(Uri uri, int decodeWidth)
        {
            try
            {
                string localPath = uri.LocalPath;
                if (_thumbPathMaker != null)
                {
                    string thumbPath = null;
                    if (decodeWidth <= c_miniThumbWidth)
                        thumbPath = _thumbPathMaker.GetMiniThumbPath(localPath, true);
                    else
                        thumbPath = _thumbPathMaker.GetThumbPath(localPath, true);

                    if (thumbPath == null)
                        return MakeBitmapImage_Default(uri, decodeWidth);
                    else
                        return MakeBitmapImage_Default(new Uri(thumbPath), decodeWidth);
                }

                if (_thumbBitmapMaker != null)
                {
                    System.Windows.Media.Imaging.BitmapImage thumbBitmap = null;
                    if (decodeWidth <= c_miniThumbWidth)
                        thumbBitmap = _thumbBitmapMaker.GetMiniThumb_BitmapImage(localPath, decodeWidth);
                    else
                        thumbBitmap = _thumbBitmapMaker.GetThumb_BitmapImage(localPath, decodeWidth);

                    if (thumbBitmap == null)
                        return MakeBitmapImage_Default(uri, decodeWidth);
                    else
                        return thumbBitmap;
                }
            }
            catch { }
            return MakeBitmapImage_Default(uri, decodeWidth);
        }

        private static BitmapImage MakeBitmapImage_Default(Uri uri, int decodeWidth)
        {
            try
            {
                BitmapImage tempImage = new BitmapImage();
                tempImage.BeginInit();
                tempImage.UriSource = uri;
                tempImage.DecodePixelWidth = decodeWidth;
                tempImage.CacheOption = BitmapCacheOption.OnLoad;
                tempImage.CreateOptions = BitmapCreateOptions.None;
                tempImage.EndInit();
                tempImage.Freeze();
                return tempImage;
            }
            catch { return null; }
        }

    }
}
