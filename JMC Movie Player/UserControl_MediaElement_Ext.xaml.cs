using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Threading; // DispatchTimer
using System.Threading; // Thread

// The PauseAll function is to make sure only one movie playing.
// I don't like distracting multiple playing movies.
namespace JMC_Photo_Gallery
{
    /// <summary>
    /// Interaction logic for UserControl_MediaElement_Ext.xaml
    /// </summary>
    public partial class UserControl_MediaElement_Ext : UserControl
    {
        public UserControl_MediaElement_Ext()
        {
            InitializeComponent();
            s_AllObjects.Add(this);

            if (ResetThumbQ_Low_Thread == null || !ResetThumbQ_Low_Thread.IsAlive)
            {
                ResetThumbQ_Low_Thread = new Thread(ResetThumbQ_Low_Processor);
                ResetThumbQ_Low_Thread.Priority = ThreadPriority.BelowNormal;
                ResetThumbQ_Low_Thread.Start();
            }
            if (ResetThumbQ_High_Thread == null || !ResetThumbQ_High_Thread.IsAlive)
            {
                ResetThumbQ_High_Thread = new Thread(ResetThumbQ_High_Processor);
                ResetThumbQ_High_Thread.Priority = ThreadPriority.Normal;
                ResetThumbQ_High_Thread.Start();
            }

            this.x_Indicator_Group1.Visibility = Group1_DefaultV;
            this.x_Indicator_Group2.Visibility = Group2_DefaultV;
            this.x_Indicator_Group3.Visibility = Group3_DefaultV;

            _imageTimer.Tick += new EventHandler(ImageTimer_Tick);
            _imageTimer.Interval = new TimeSpan(0, 0, 0, 0, 750);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            lock (s_AllObjects)
                s_AllObjects.Remove(this);
            lock (s_AllOpenedObjects)
                s_AllOpenedObjects.Remove(this);
            lock (ResetThumbQ_Low)
                ResetThumbQ_Low.Remove(this);
            lock (ResetThumbQ_High)
                ResetThumbQ_High.Remove(this);

            if (s_AllObjects.Count < 1)
                AbortThread();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            lock (_thisLock)
            {
                this.x_Player.Stop();
                this.x_Player.Play();
            }
        }

        private object _thisLock = new object();
        private RenderTargetBitmap _rtb;
        private TimeSpan _position = new TimeSpan(0, 0, 0, 0, 0);
        private bool _isPlayerClosed = true;
        private bool _isDPositionSet = false;

        #region SourceUri

        public Uri SourceUri
        {
            get { return (Uri)GetValue(SourceUriProperty); }
            set { SetValue(SourceUriProperty, value); }
        }

        public static readonly DependencyProperty SourceUriProperty = DependencyProperty.Register(
          "SourceUri", typeof(Uri), typeof(UserControl_MediaElement_Ext),
          new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender
              , new PropertyChangedCallback(OnSourceUriCallback))
              );

        private static void OnSourceUriCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UserControl_MediaElement_Ext selfObj = (UserControl_MediaElement_Ext)d;
            selfObj.x_Player.Source = (Uri)e.NewValue;
            if (selfObj.ThumbExist())
            {
                try
                {
                    BitmapFrame bf = BitmapFrame.Create(new Uri(selfObj.ThumbPath()),
                        BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    bf.Freeze();
                    selfObj.x_Thumb.Source = bf;
                }
                catch (Exception ex) { Console.WriteLine("UserControl_ME_Ext failed to load existing thumb\n" + ex.ToString()); }
            }
            else
            {
                lock (ResetThumbQ_Low)
                {
                    selfObj.ShowIndicator(State.ResetWaiting);
                    ResetThumbQ_Low.Add(selfObj);
                }
            }
            RoutedEventArgs newargs = new RoutedEventArgs(SourceUriChangedEvent);
            (d as FrameworkElement).RaiseEvent(newargs);
        }

        // Not Really Needed, But Better For Completeness.
        public static readonly RoutedEvent SourceUriChangedEvent = EventManager.RegisterRoutedEvent("SourceUriChangedEvent", RoutingStrategy.Bubble, typeof(DependencyPropertyChangedEventHandler), typeof(UserControl_MediaElement_Ext));

        public event RoutedEventHandler SourceUriChanged
        {
            add { AddHandler(SourceUriChangedEvent, value); }
            remove { RemoveHandler(SourceUriChangedEvent, value); }
        }

        #endregion

        #region None Bindable Properties

        public double MainWidth { get { return this.MainControl.Width; } set { this.MainControl.Width = value; } }
        public double MainHeight { get { return this.MainControl.Height; } set { this.MainControl.Height = value; } }
        public bool IsDPositionSet { get { return _isDPositionSet; } }
        public double TimePosition { get { return x_Player.Position.TotalMilliseconds; } }
        public string FileName
        {
            get
            {
                try { return new System.IO.FileInfo(x_Player.Source.LocalPath).Name; }
                catch (Exception ex) { ex.ToString(); return "Unknown"; }
            }
        }

        #endregion

        #region Static s_AllObjects and s_AllOpenedObjects

        private static Collection<UserControl_MediaElement_Ext> s_AllObjects = new Collection<UserControl_MediaElement_Ext>();
        private static Collection<UserControl_MediaElement_Ext> s_AllOpenedObjects = new Collection<UserControl_MediaElement_Ext>();
        public static int AllObjects_Count { get { return s_AllObjects.Count; } }
        public static int AllOpenedObjects_Count { get { return s_AllOpenedObjects.Count; } }
        private static int s_AllowOpenedObjects_Count = 5;

        public static int AllowOpenedObjects_Count
        {
            get { return s_AllowOpenedObjects_Count; }
            set
            {
                if (value < 1)
                    return;
                s_AllowOpenedObjects_Count = value;

                // Note MenuItem_Click_ConvertToThumb Already does
                // s_AllOpenedObjects.RemoveAt(0);
                while (s_AllOpenedObjects.Count > s_AllowOpenedObjects_Count)
                    s_AllOpenedObjects[0].MenuItem_Click_ConvertToThumb(null, null);
            }
        }

        private static void s_AllOpenedObjects_Add(UserControl_MediaElement_Ext obj)
        {
            // Note MenuItem_Click_ConvertToThumb Already does
            // s_AllOpenedObjects.RemoveAt(0);
            s_AllOpenedObjects.Remove(obj);
            while (s_AllOpenedObjects.Count >= s_AllowOpenedObjects_Count)
                s_AllOpenedObjects[0].MenuItem_Click_ConvertToThumb(null, null);
            s_AllOpenedObjects_PauseAll();
            s_AllOpenedObjects.Add(obj);
        }

        public static void s_AllOpenedObjects_Remove(UserControl_MediaElement_Ext obj)
        {
            // Note MenuItem_Click_ConvertToThumb Already does
            // s_AllOpenedObjects.RemoveAt(0);
            int index = s_AllOpenedObjects.IndexOf(obj);
            if (index >= 0)
                s_AllOpenedObjects[index].MenuItem_Click_ConvertToThumb(null, null);
        }

        public static void s_AllOpenedObjects_Clear()
        {
            // Note MenuItem_Click_ConvertToThumb Already does
            // s_AllOpenedObjects.RemoveAt(0);
            while (s_AllOpenedObjects.Count > 0)
                s_AllOpenedObjects[0].MenuItem_Click_ConvertToThumb(null, null);
        }

        public static void s_AllOpenedObjects_PauseAll()
        {
            foreach (UserControl_MediaElement_Ext tempME in s_AllOpenedObjects)
                tempME.x_Player.Pause();
        }

        #endregion

        #region Show(Open) / Hide(Close) Movie

        // Moved Up Here For Better Reference
        public void MenuItem_Click_Play(object sender, RoutedEventArgs e)
        {
            lock (_thisLock)
            {
                if (_isPlayerClosed)
                    this.x_Player.Position = new TimeSpan(0, 0, 0, 0, (int)_position.TotalMilliseconds);
                ShowIndicator(State.ShowPlayer);
                this.x_Player.Play();
                this._isPlayerClosed = false;
                s_AllOpenedObjects_Add(this);

                // Only Play Shoul Check Default Time
                if (!_isDPositionSet)
                    SetDPosition();
            }
        }

        public void MenuItem_Click_Pause(object sender, RoutedEventArgs e)
        {
            lock (_thisLock)
            {
                if (_isPlayerClosed)
                    this.x_Player.Position = new TimeSpan(0, 0, 0, 0, (int)_position.TotalMilliseconds);
                ShowIndicator(State.ShowPlayer);
                this.x_Player.Pause();
                this._isPlayerClosed = false;
                s_AllOpenedObjects_Add(this);
            }
        }

        public void MenuItem_Click_Stop(object sender, RoutedEventArgs e)
        {
            lock (_thisLock)
            {
                ShowIndicator(State.ShowPlayer);
                this.x_Player.Position = new TimeSpan(0, 0, 0, 0, 0);
                this.x_Player.Stop();
                this._isPlayerClosed = false;
                s_AllOpenedObjects_Add(this);
            }
        }

        // Moved Up Here For Better Reference
        public void MenuItem_Click_ConvertToThumb(object sender, RoutedEventArgs e)
        {
            lock (_thisLock)
            {
                if (ThumbExist())
                    Recording();
                else
                    MenuItem_Click_SaveThumb(null, null);

                _position = new TimeSpan(0, 0, 0, 0, (int)this.x_Player.Position.TotalMilliseconds);
                ShowIndicator(State.ShowThumb);
                this.x_Player.Close();
                this._isPlayerClosed = true;
                s_AllOpenedObjects.Remove(this);
            }
        }

        #endregion

        #region Reset and Save

        public void MenuItem_Click_Reset(object sender, RoutedEventArgs e)
        {
            lock (_thisLock)
            {
                if (this._isPlayerClosed)
                    lock (ResetThumbQ_High)
                    {
                        this.ShowIndicator(State.ResetWaiting);
                        ResetThumbQ_High.Add(this);
                    }
                else
                {
                    ShowIndicator(State.ResetWaiting);

                    _isDPositionSet = true;
                    this.x_Player.Play();
                    this.x_Player.Pause();
                    double jumpTime = GetDefaultTime(this);
                    _position = new TimeSpan(0, 0, 0, 0, (int)jumpTime);
                    this.x_Player.Position = new TimeSpan(0, 0, 0, 0, (int)jumpTime); ;
                    Thread.Sleep(500);
                    this.x_Player.Play();
                    int i = 0;
                    while (this.x_Player.Position.TotalMilliseconds <= jumpTime && i < 50)
                    {
                        i++; Thread.Sleep(50);
                        //Console.WriteLine(this.x_Player.Position.TotalMilliseconds + "  " + i);
                    }

                    MenuItem_Click_SaveThumb(null, null);
                }
            }
        }

        public void MenuItem_Click_ResetFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                System.IO.DirectoryInfo folder = new System.IO.FileInfo(this.SourceUri.LocalPath).Directory;
                System.IO.DirectoryInfo tempFolder;

                for (int i = 0; i < s_AllObjects.Count; i++)
                {
                    try
                    {
                        tempFolder = new System.IO.FileInfo(s_AllObjects[i].SourceUri.LocalPath).Directory;
                        if (folder.FullName.ToLower() == tempFolder.FullName.ToLower())
                            s_AllObjects[i].MenuItem_Click_Reset(null, null);
                    }
                    catch (Exception ex) { ex.ToString(); }
                }
            }
            catch (Exception ex) { ex.ToString(); }
        }

        public void MenuItem_Click_SaveThumb(object sender, RoutedEventArgs e)
        {
            lock (_thisLock)
            {
                // Recording Failed is fine because that means thumb is already created.
                Recording();
                SaveThumb(this);
            }
        }

        // You Have To Play Movie First
        private void Recording()
        {
            lock (_thisLock)
            {
                ShowIndicator(State.Recording);
                bool isSucessful = true;
                try
                {
                    if (!this._isPlayerClosed)
                    {
                        Size dpi = new Size(96, 96);
                        RenderTargetBitmap rtb = new RenderTargetBitmap(
                            (int)this.x_Player.ActualWidth, (int)this.x_Player.ActualHeight,
                            dpi.Width, dpi.Height, PixelFormats.Pbgra32);
                        rtb.Render(this.x_Player);
                        _rtb = rtb; // this is after rtb.Render didn't fail
                        this.x_Thumb.Source = BitmapFrame.Create(rtb);
                    }
                    else
                        isSucessful = false;
                }
                catch (Exception ex)
                {
                    ex.ToString();
                    isSucessful = false;
                }

                if (isSucessful)
                    ShowIndicator(State.RecordingSucessful);
                else
                    ShowIndicator(State.RecordingFailed);
            }
        }

        #endregion

        #region Helper Functions

        public static string aa = "";
        public static string ThumbDB_BasePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\UserControl_MediaElement_Ext\DBM";

        public string ThumbPath()
        {
            try
            {
                string thumbPath = this.SourceUri.LocalPath.Replace(":", "");
                thumbPath = ThumbDB_BasePath + @"\" + thumbPath + ".jpg";
                return thumbPath;
            }
            catch (Exception ex) { ex.ToString(); return ""; }
        }

        public bool ThumbExist()
        {
            try
            {
                return new System.IO.FileInfo(ThumbPath()).Exists;
            }
            catch (Exception ex) { ex.ToString(); return false; }
        }

        private static bool SaveImage(string thumbPath, RenderTargetBitmap rtb)
        {
            var enc = new System.Windows.Media.Imaging.JpegBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            try
            {
                System.IO.FileInfo _thumbFile = new System.IO.FileInfo(thumbPath);
                System.IO.DirectoryInfo _thumbParent = new System.IO.DirectoryInfo(_thumbFile.Directory.FullName);
                if (!_thumbParent.Exists)
                    _thumbParent.Create();

                using (var stm = System.IO.File.Create(thumbPath))
                {
                    enc.Save(stm);
                }
            }
            catch (Exception ex) { ex.ToString(); return false; }
            return true;
        }

        private static void SaveThumb(UserControl_MediaElement_Ext obj)
        {
            lock (obj._thisLock)
            {
                if (obj._rtb != null)
                {
                    obj.ShowIndicator(State.Saving);
                    bool isSucessful = SaveImage(obj.ThumbPath(), obj._rtb);
                    if (isSucessful)
                        obj.ShowIndicator(State.SavingSucessful);
                    else
                        obj.ShowIndicator(State.SavingFailed);
                }
                else
                    obj.ShowIndicator(State.SavingFailed);
            }
        }

        private static double GetDefaultTime(UserControl_MediaElement_Ext obj)
        {
            int i = 0;
            double tempTimeInSeconds = 0;
            if (obj._isPlayerClosed)
            {
                MediaPlayer mp = new MediaPlayer();
                mp.ScrubbingEnabled = true;
                mp.IsMuted = true;
                mp.Open(obj.SourceUri);
                mp.Play();
                mp.Pause();
                while (!mp.NaturalDuration.HasTimeSpan)
                { Thread.Sleep(50); i++; if (i > 40) break; }
                if (mp.NaturalDuration.HasTimeSpan)
                    tempTimeInSeconds = mp.NaturalDuration.TimeSpan.TotalMilliseconds;
                mp.Close();
            }
            else
            {
                while (!obj.x_Player.NaturalDuration.HasTimeSpan)
                { Thread.Sleep(50); i++; if (i > 40) break; }
                if (obj.x_Player.NaturalDuration.HasTimeSpan)
                    tempTimeInSeconds = obj.x_Player.NaturalDuration.TimeSpan.TotalMilliseconds;
            }
            tempTimeInSeconds = Math.Ceiling(tempTimeInSeconds / 10);
            if (tempTimeInSeconds > 2.5 * 60 * 1000)
                tempTimeInSeconds = 2.5 * 60 * 1000;

            //Console.WriteLine("GetDefaultTime " + tempTimeInSeconds + " i = " + i);
            return tempTimeInSeconds;
        }

        public void SetDPosition()
        {
            _isDPositionSet = true;
            double jumpTo = GetDefaultTime(this);
            this._position = new TimeSpan(0, 0, 0, 0, (int)jumpTo);
            // Not sure this will open x_Player or not
            this.x_Player.Position = new TimeSpan(0, 0, 0, 0, (int)jumpTo); ;
        }

        #endregion

        #region MouseEnter and Leave

        DispatcherTimer _imageTimer = new DispatcherTimer();
        void ImageTimer_Tick(object sender, EventArgs e)
        {
            _imageTimer.Stop();
            lock (_thisLock)
                MenuItem_Click_Play(null, null);
        }

        public void MainButton_MouseEnter(object sender, MouseEventArgs e)
        {
            this.MainButton.Opacity = 1;
            lock (_thisLock)
                if (this.x_Thumb.Visibility != Visibility.Visible)
                    MenuItem_Click_Play(null, null);
                else
                {
                    _imageTimer.Stop();
                    _imageTimer.Start();
                }
        }

        public void MainButton_MouseLeave(object sender, MouseEventArgs e)
        {
            this.MainButton.Opacity = 0.3;
            lock (_thisLock)
                if (this.x_Thumb.Visibility != Visibility.Visible)
                    MenuItem_Click_Pause(null, null);
                else
                    _imageTimer.Stop();
        }

        #endregion

        # region Open Folder

        private void MenuItem_Click_ShowThumbFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = new System.IO.FileInfo(ThumbPath()).DirectoryName;
                proc.Start();
            }
            catch (Exception ex) { ex.ToString(); }
        }

        private void MenuItem_Click_ShowMovieFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = new System.IO.FileInfo(this.SourceUri.LocalPath).DirectoryName;
                proc.Start();
            }
            catch (Exception ex) { ex.ToString(); }
        }

        #endregion

        #region Indicators
        // Not Invoke Set, thus, You Can't Control Across Threads Yet.
        public static Visibility Group1_DefaultV = Visibility.Visible;
        public static Visibility Group2_DefaultV = Visibility.Visible;
        public static Visibility Group3_DefaultV = Visibility.Visible;

        public enum State
        {
            ShowPlayer, ShowThumb,
            Recording, RecordingFailed, RecordingSucessful,
            ResetWaiting, Saving, SavingFailed, SavingSucessful
        }

        private void MenuItem_Click_Indicator(object sender, RoutedEventArgs e)
        {
            MenuItem result = (MenuItem)sender;
            string tags = result.Tag.ToString().ToLower();
            TurnIndicatorGroupOnOff_G(tags);
        }

        public void TurnIndicatorGroupOnOff_G(string tags)
        {
            if (tags.Contains("global"))
            {
                bool isOn = tags.Contains("on");
                if (tags.Contains("all"))
                {
                    if (isOn)
                    {
                        Group1_DefaultV = Visibility.Visible;
                        Group2_DefaultV = Visibility.Visible;
                        Group3_DefaultV = Visibility.Visible;
                    }
                    else
                    {
                        Group1_DefaultV = Visibility.Collapsed;
                        Group2_DefaultV = Visibility.Collapsed;
                        Group3_DefaultV = Visibility.Collapsed;
                    }
                }
                else if (tags.Contains("status"))
                {
                    if (isOn)
                        Group1_DefaultV = Visibility.Visible;
                    else
                        Group1_DefaultV = Visibility.Collapsed;
                }
                else if (tags.Contains("recording"))
                {
                    if (isOn)
                        Group2_DefaultV = Visibility.Visible;
                    else
                        Group2_DefaultV = Visibility.Collapsed;
                }
                else if (tags.Contains("saving"))
                {
                    if (isOn)
                        Group3_DefaultV = Visibility.Visible;
                    else
                        Group3_DefaultV = Visibility.Collapsed;
                }

                foreach (UserControl_MediaElement_Ext obj in s_AllObjects)
                    obj.TurnIndicatorGroupOnOff_S(tags);
            }
            else
                this.TurnIndicatorGroupOnOff_S(tags);
        }

        private void TurnIndicatorGroupOnOff_S(string tags)
        {
            tags = tags.ToLower();
            bool isOn = tags.Contains("on");
            if (tags.Contains("all"))
            {
                if (isOn)
                {
                    this.x_Indicator_Group1.Visibility = Visibility.Visible;
                    this.x_Indicator_Group2.Visibility = Visibility.Visible;
                    this.x_Indicator_Group3.Visibility = Visibility.Visible;
                }
                else
                {
                    this.x_Indicator_Group1.Visibility = Visibility.Collapsed;
                    this.x_Indicator_Group2.Visibility = Visibility.Collapsed;
                    this.x_Indicator_Group3.Visibility = Visibility.Collapsed;
                }
            }
            else if (tags.Contains("status"))
            {
                if (isOn)
                    this.x_Indicator_Group1.Visibility = Visibility.Visible;
                else
                    this.x_Indicator_Group1.Visibility = Visibility.Collapsed;
            }
            else if (tags.Contains("recording"))
            {
                if (isOn)
                    this.x_Indicator_Group2.Visibility = Visibility.Visible;
                else
                    this.x_Indicator_Group2.Visibility = Visibility.Collapsed;
            }
            else if (tags.Contains("saving"))
            {
                if (isOn)
                    this.x_Indicator_Group3.Visibility = Visibility.Visible;
                else
                    this.x_Indicator_Group3.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowIndicator(State state)
        {
            switch (state)
            {
                // Keep this.x_Player.Visibility = Visibility.Visible;
                case State.ShowPlayer:
                    this.x_Thumb.Visibility = Visibility.Hidden;
                    this.x_Indicator_ShowThumb.Visibility = Visibility.Hidden;
                    this.x_Indicator_ShowMedia.Visibility = Visibility.Visible;

                    this.x_Indicator_Recording.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingSucessful.Visibility = Visibility.Hidden;

                    this.x_Indicator_ResetWaiting.Visibility = Visibility.Hidden;
                    this.x_Indicator_Saving.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingSucessful.Visibility = Visibility.Hidden;
                    break;
                case State.ShowThumb:
                    this.x_Thumb.Visibility = Visibility.Visible;
                    this.x_Indicator_ShowThumb.Visibility = Visibility.Visible;
                    this.x_Indicator_ShowMedia.Visibility = Visibility.Hidden;
                    break;
                case State.Recording:
                    this.x_Indicator_Saving.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingSucessful.Visibility = Visibility.Hidden;

                    this.x_Indicator_Recording.Visibility = Visibility.Visible;
                    this.x_Indicator_RecordingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingSucessful.Visibility = Visibility.Hidden;
                    break;
                case State.RecordingFailed:
                    this.x_Indicator_Recording.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingFailed.Visibility = Visibility.Visible;
                    this.x_Indicator_RecordingSucessful.Visibility = Visibility.Hidden;
                    break;
                case State.RecordingSucessful:
                    this.x_Indicator_Recording.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingSucessful.Visibility = Visibility.Visible;
                    break;
                case State.ResetWaiting:
                    this.x_Indicator_Recording.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_RecordingSucessful.Visibility = Visibility.Hidden;

                    this.x_Indicator_ResetWaiting.Visibility = Visibility.Visible;
                    this.x_Indicator_Saving.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingSucessful.Visibility = Visibility.Hidden;
                    break;
                case State.Saving:
                    this.x_Indicator_ResetWaiting.Visibility = Visibility.Hidden;
                    this.x_Indicator_Saving.Visibility = Visibility.Visible;
                    this.x_Indicator_SavingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingSucessful.Visibility = Visibility.Hidden;
                    break;
                case State.SavingFailed:
                    this.x_Indicator_ResetWaiting.Visibility = Visibility.Hidden;
                    this.x_Indicator_Saving.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingFailed.Visibility = Visibility.Visible;
                    this.x_Indicator_SavingSucessful.Visibility = Visibility.Hidden;
                    break;
                case State.SavingSucessful:
                    this.x_Indicator_ResetWaiting.Visibility = Visibility.Hidden;
                    this.x_Indicator_Saving.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingFailed.Visibility = Visibility.Hidden;
                    this.x_Indicator_SavingSucessful.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        #region Threaded DefaultThumb Maker

        public delegate void VoidMethodDelegate1(UserControl_MediaElement_Ext arg);
        public delegate void VoidMethodDelegate2(UserControl_MediaElement_Ext arg, UserControl_MediaElement_Ext.State arg2);
        private static void SetUri(UserControl_MediaElement_Ext obj) { s_T_Uri = obj.SourceUri; }
        private static void SetDim(UserControl_MediaElement_Ext obj) { s_T_Width = obj.MainControl.ActualWidth; s_T_Height = obj.MainControl.ActualHeight; }
        private static void SetRtb(UserControl_MediaElement_Ext obj) { obj._rtb = s_T_Rtb; obj.x_Thumb.Source = BitmapFrame.Create(s_T_Rtb); }
        private static void SetIndicator(UserControl_MediaElement_Ext obj, UserControl_MediaElement_Ext.State state) { obj.ShowIndicator(state); }
        private static Uri s_T_Uri = null;
        private static double s_T_Time = 0;
        private static double s_T_Width = 0;
        private static double s_T_Height = 0;
        private static RenderTargetBitmap s_T_Rtb = null;
        private static Collection<UserControl_MediaElement_Ext> ResetThumbQ_Low = new Collection<UserControl_MediaElement_Ext>();
        private static Collection<UserControl_MediaElement_Ext> ResetThumbQ_High = new Collection<UserControl_MediaElement_Ext>();
        private static Thread ResetThumbQ_Low_Thread;
        private static Thread ResetThumbQ_High_Thread;
        private static object ResetThumbQ_Lock = new object();

        public static void AbortThread()
        {
            try
            {
                ResetThumbQ_Low_Thread.Abort();
            }
            catch (Exception ex) { ex.ToString(); }
            try
            {
                ResetThumbQ_High_Thread.Abort();
            }
            catch (Exception ex) { ex.ToString(); }
        }

        private static void ResetThumbQ_Low_Processor()
        {
#if DEBUG
            Console.WriteLine("ResetThumbQ_Low_Processor Is Running");
#endif
            while (s_AllObjects.Count > 0)
            {
                if (ResetThumbQ_High.Count <= 0 && ResetThumbQ_Low.Count > 0)
                {
                    lock (ResetThumbQ_Lock)
                    {
                        UserControl_MediaElement_Ext tempME = null;
                        lock (ResetThumbQ_Low)
                            if (ResetThumbQ_Low.Count > 0)
                            {
                                tempME = ResetThumbQ_Low[0];
                                ResetThumbQ_Low.RemoveAt(0);
                            }
                        if (tempME != null)
                            ResetThumbQ_General_Processor(tempME);
                    }
                }
                //Console.WriteLine("Low s_AllObjects.Count = " + s_AllObjects.Count);
                Thread.Sleep(1000);
            }
#if DEBUG
            Console.WriteLine("ResetThumbQ_Low_Processor Ended");
#endif
        }

        private static void ResetThumbQ_High_Processor()
        {
#if DEBUG
            Console.WriteLine("ResetThumbQ_High_Processor Is Running");
#endif
            while (s_AllObjects.Count > 0)
            {
                if (ResetThumbQ_High.Count > 0)
                {
                    lock (ResetThumbQ_Lock)
                    {
                        UserControl_MediaElement_Ext tempME = null;
                        lock (ResetThumbQ_High)
                            if (ResetThumbQ_High.Count > 0)
                            {
                                tempME = ResetThumbQ_High[0];
                                ResetThumbQ_High.RemoveAt(0);
                            }
                        if (tempME != null)
                            ResetThumbQ_General_Processor(tempME);
                    }
                }
                //Console.WriteLine("High s_AllObjects.Count = " + s_AllObjects.Count);
                Thread.Sleep(1000);
            }
#if DEBUG
            Console.WriteLine("ResetThumbQ_High_Processor Ended");
#endif
        }

        private static void ResetThumbQ_General_Processor(UserControl_MediaElement_Ext tempME)
        {
            lock (ResetThumbQ_Lock)
            {
                tempME.Dispatcher.Invoke(DispatcherPriority.Background,
                    new VoidMethodDelegate1(SetUri), tempME);
                GetDefaultTime(); // Not Need For Dispatcher, All Static
                tempME.Dispatcher.Invoke(DispatcherPriority.Background,
                    new VoidMethodDelegate1(SetDim), tempME);
                tempME.Dispatcher.Invoke(DispatcherPriority.Background,
                    new VoidMethodDelegate2(SetIndicator), tempME, State.Recording);

                if (Record_MediaPlayer())
                {
                    tempME.Dispatcher.Invoke(DispatcherPriority.Background,
                        new VoidMethodDelegate2(SetIndicator), tempME, State.RecordingSucessful);
                    tempME.Dispatcher.Invoke(DispatcherPriority.Background,
                        new VoidMethodDelegate1(SetRtb), tempME);
                    tempME.Dispatcher.Invoke(DispatcherPriority.Background,
                        new VoidMethodDelegate1(SaveThumb), tempME);
                }
                else
                    tempME.Dispatcher.Invoke(DispatcherPriority.Background,
                        new VoidMethodDelegate2(SetIndicator), tempME, State.RecordingFailed);
            }
        }

        private static void GetDefaultTime()
        {
            if (s_T_Uri == null)
            {
                s_T_Time = 0;
                return;
            }

            int i = 0;
            double tempTimeInSeconds = 0;
            MediaPlayer mp = new MediaPlayer();
            mp.ScrubbingEnabled = true;
            mp.IsMuted = true;
            mp.Open(s_T_Uri);
            mp.Play();
            mp.Pause();
            while (!mp.NaturalDuration.HasTimeSpan)
            { Thread.Sleep(50); i++; if (i > 40) break; }
            if (mp.NaturalDuration.HasTimeSpan)
                tempTimeInSeconds = mp.NaturalDuration.TimeSpan.TotalMilliseconds;
            mp.Close();

            tempTimeInSeconds = Math.Ceiling(tempTimeInSeconds / 10);
            if (tempTimeInSeconds > 2.5 * 60 * 1000)
                tempTimeInSeconds = 2.5 * 60 * 1000;

            //Console.WriteLine("GetDefaultTime " + tempTimeInSeconds + " i = " + i);
            s_T_Time = tempTimeInSeconds;
        }

        private static bool Record_MediaPlayer()
        {
            try
            {
                // Jump Position
                MediaPlayer mp = new MediaPlayer();
                mp.ScrubbingEnabled = true;
                mp.IsMuted = true;
                mp.Open(s_T_Uri);
                mp.Play();
                mp.Position = new TimeSpan(0, 0, 0, 0, (int)s_T_Time);
                int i = 0;
                while (mp.Position.TotalMilliseconds <= s_T_Time && i < 50)
                {
                    //Console.WriteLine(mp.Position.TotalMilliseconds + " " + s_T_Time);
                    Thread.Sleep(100); i++;
                }
                //Console.WriteLine(mp.Position.TotalMilliseconds + " " + s_T_Time + " i=" + i);

                // Make Thumb Image
                int mpWidth = mp.NaturalVideoWidth;
                int mpHeight = mp.NaturalVideoHeight;
                double scale = Math.Min((double)s_T_Width / mpWidth, (double)s_T_Height / mpHeight);
                mpWidth = (int)(mpWidth * scale);
                mpHeight = (int)(mpHeight * scale);
                s_T_Rtb = new RenderTargetBitmap(mpWidth, mpHeight, 1 / 200, 1 / 200, PixelFormats.Pbgra32);
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawVideo(mp, new Rect(0, 0, mpWidth, mpHeight));
                dc.Close();
                s_T_Rtb.Render(dv);
                s_T_Rtb.Freeze();
                mp.Close();
                return true;
            }
            catch (Exception ex) { ex.ToString(); s_T_Rtb = null; return false; }
        }

        #endregion

    }
}
