using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.ComponentModel; // INotifyPropertyChanged
using System.Windows.Threading; // DispatchTimer
using System.Threading; // Thread
using System.Windows.Media.Animation; // Storyboard
using System.IO;

namespace JMC_Photo_Gallery
{
    /// <summary>
    /// Interaction logic for Window_MoviePlayer.xaml
    /// </summary>
    public partial class Window_MoviePlayer : Window, INotifyPropertyChanged
    {
        Storyboard _sb = new Storyboard();
        MediaTimeline _mt = new MediaTimeline();

        #region Windows Actions

        public Window_MoviePlayer()
        {
            InitializeComponent();
            _sliderTimer.Tick += new EventHandler(SliderTimer_Tick);
            _sliderTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _sliderSeekTimer.Tick += new EventHandler(SliderSeekTimer_Tick);
            _sliderSeekTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _sliderStoppedTimer.Tick += new EventHandler(SliderStoppedTimer_Tick);
            _sliderStoppedTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);

            _mt.FillBehavior = FillBehavior.HoldEnd;
            Storyboard.SetTargetName(_mt, "x_MediaElement");
            _sb.Children.Add(_mt);

            // Populate List
            PopulateMovieList();
        }

        private void PopulateMovieList()
        {
            MovieCollection tempCollection = (MovieCollection)this.FindResource("DisplayMovies");
            tempCollection.MovieFolders.Clear();

            foreach (string path in AR.GalleryConfig.Folders)
            {
                Collection<MovieFolder> result = new Collection<MovieFolder>();
                FindVideoFoldersInCollection(path, result);
                foreach (MovieFolder item in result)
                    tempCollection.MovieFolders.Add(item);
            }
        }

        private void FindVideoFoldersInCollection(string collectionPath, Collection<MovieFolder> result)
        {
            try
            {
                // Prevent scanning its own data store.
                if (collectionPath.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                    return;

                DirectoryInfo dirx = new DirectoryInfo(collectionPath);

                //JAL 20150329
                if (dirx.Name.StartsWith(".")) return;
                
                string[] videoPaths = FindVideosInFolder(collectionPath);
                if (videoPaths != null)
                {
                    MovieFolder tempMovieFolder = new MovieFolder(collectionPath);
                    foreach (string videoPath in videoPaths)
                        tempMovieFolder.MovieFiles.Add(new MovieFile(videoPath));
                    result.Add(tempMovieFolder);
                }

                foreach (System.IO.DirectoryInfo dir in new System.IO.DirectoryInfo(collectionPath).GetDirectories())
                    FindVideoFoldersInCollection(dir.FullName, result);
            }
            catch { }
        }

        private string[] FindVideosInFolder(string folderPath)
        {
            Collection<string> result = new Collection<string>();
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(folderPath);
            foreach (System.IO.FileInfo info in dirInfo.GetFiles())
                if (AR.GalleryConfig.IsVideo(info.Extension))
                    result.Add(info.FullName);

            if (result.Count == 0)
                return null;
            string[] result2 = new string[result.Count];
            result.CopyTo(result2, 0);
            return result2;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Binding b2 = new Binding();
            b2.Source = this;
            b2.Path = new PropertyPath("MyProp");

            // Bind to the slider and the textbox
            BindingOperations.SetBinding(x_TimelineSlider, Slider.ValueProperty, b2);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _sliderTimer.Stop();
            _sliderSeekTimer.Stop();
            UserControl_MediaElement_Ext.AbortThread();
        }

        private void Key_Press(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.MediaPlayPause:
                case Key.NumPad2:
                case Key.NumPad5:
                case Key.NumPad8:
                case Key.D2:
                case Key.D5:
                case Key.D8:
                    Play_Click(null, null);
                    break;
                case Key.MediaPreviousTrack:
                case Key.NumPad1:
                case Key.NumPad4:
                case Key.NumPad7:
                case Key.D1:
                case Key.D4:
                case Key.D7:
                    Previous_Click(null, null);
                    break;
                case Key.MediaNextTrack:
                case Key.NumPad3:
                case Key.NumPad6:
                case Key.NumPad9:
                case Key.D3:
                case Key.D6:
                case Key.D9:
                    Next_Click(null, null);
                    break;
                case Key.MediaStop:
                case Key.NumPad0:
                case Key.D0:
                    Stop_Click(null, null);
                    break;
                case Key.Add:
                case Key.OemPlus:
                    x_VolumeSlider.Value = x_VolumeSlider.Value + 0.1;
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    x_VolumeSlider.Value = x_VolumeSlider.Value - 0.1;
                    break;
                case Key.OemPeriod:
                    x_SpeedRatioSlider.Value = x_SpeedRatioSlider.Value + 1;
                    break;
                case Key.OemComma:
                    x_SpeedRatioSlider.Value = x_SpeedRatioSlider.Value - 1;
                    break;
                case Key.Multiply:
                    x_BalanceSlider.Value = x_BalanceSlider.Value + 0.1;
                    break;
                case Key.Divide:
                    x_BalanceSlider.Value = x_BalanceSlider.Value - 0.1;
                    break;
                case Key.M:
                    if (_playerState == PlayerState.Playing)
                        Play_Click(null, null);
                    x_TimelineSlider.Value = x_TimelineSlider.Value + 1;
                    break;
                case Key.N:
                    if (_playerState == PlayerState.Playing)
                        Play_Click(null, null);
                    x_TimelineSlider.Value = x_TimelineSlider.Value - 1;
                    break;
                case Key.Escape:
                    this.Close();
                    break;
            }
            Console.WriteLine(e.Key);
        }

        #endregion

        #region Large Media Player Actions

        private double seekToTime = 0;
        private void PlayMovie(MovieFile movieFile)
        {
            lock (_sb)
            {
                //x_MoviesExpender.IsExpanded = false;
                _sb.Stop(this);
                _mt.Source = movieFile.Uri;
                _sb.Begin(this, true);
                x_MediaElement.Volume = (double)x_VolumeSlider.Value;
                _sb.SetSpeedRatio(this, (double)x_SpeedRatioSlider.Value);
            }

            _playerState = PlayerState.Playing;
            this.Play.Content = ";";
            this.x_FileNameTextBlock.Text = "Playing: " + movieFile.Name;
        }

        private void PlayMovie(UserControl_MediaElement_Ext userControl)
        {
            if (x_syncCheckbox.IsChecked.Value)
            {
                if (!userControl.IsDPositionSet)
                    userControl.SetDPosition();
                seekToTime = userControl.TimePosition / 1000;
            }

            lock (_sb)
            {
                x_MoviesExpender.IsExpanded = false;
                _sb.Stop(this);
                _mt.Source = userControl.SourceUri;
                _sb.Begin(this, true);
                x_MediaElement.Volume = (double)x_VolumeSlider.Value;
                _sb.SetSpeedRatio(this, (double)x_SpeedRatioSlider.Value);
            }

            _playerState = PlayerState.Playing;
            this.Play.Content = ";";
            this.x_FileNameTextBlock.Text = "Playing: " + userControl.FileName;
        }

        public void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            if (x_MediaElement.Clock != null)
            {
                x_TimelineSlider.Maximum = x_MediaElement.Clock.NaturalDuration.TimeSpan.TotalSeconds;
            }
            _sliderTimer.Start();
            _sliderSeekTimer.Start();
            x_MediaElement.Volume = (double)x_VolumeSlider.Value;
            _sb.SetSpeedRatio(this, (double)x_SpeedRatioSlider.Value);

            // Jump To seekToTime
            if (seekToTime != 0)
            {
                this.MyProp = seekToTime;
                seekToTime = 0;
                lock (_sb)
                    _sb.Resume(this);
            }
        }

        private void x_MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Next_Click(null, null);
        }

        #endregion

        #region MyProp: TimelineSlider Binded Property

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        DispatcherTimer _sliderTimer = new DispatcherTimer();
        void SliderTimer_Tick(object sender, EventArgs e)
        {
            OnPropertyChanged("MyProp");
        }

        private double _sliderSeekTime = -1;
        DispatcherTimer _sliderSeekTimer = new DispatcherTimer();
        void SliderSeekTimer_Tick(object sender, EventArgs e)
        {
            lock (_sb)
                if (_sliderSeekTime >= 0)
                {
                    if (_playerState == PlayerState.Stopped)
                        _playerState = PlayerState.Paused;
                    _sb.Pause(this);
                    _sb.Seek(this, new TimeSpan((long)Math.Floor(_sliderSeekTime * TimeSpan.TicksPerSecond)), TimeSeekOrigin.BeginTime);
                    //_sb.SeekAlignedToLastTick(this, new TimeSpan((long)Math.Floor(_sliderSeekTime * TimeSpan.TicksPerSecond)), TimeSeekOrigin.BeginTime);
                    _sliderSeekTime = -1;
                    OnPropertyChanged("MyProp");
                }
        }

        DispatcherTimer _sliderStoppedTimer = new DispatcherTimer();
        void SliderStoppedTimer_Tick(object sender, EventArgs e)
        {
            Next_Click(null, null);
        }

        private double currentTime;
        public double MyProp
        {
            get
            {
                try
                {
                    /*
                    Console.WriteLine(!x_TimelineSlider.IsMouseCaptureWithin + " " + _playerState + " " + currentTime + " " + _sb.GetCurrentTime(this).Value.TotalSeconds);
                    if (!x_TimelineSlider.IsMouseCaptureWithin &&
                        _playerState == PlayerState.Playing &&
                        currentTime == _sb.GetCurrentTime(this).Value.TotalSeconds)
                        _sliderStoppedTimer.Start();
                    else
                        _sliderStoppedTimer.Stop();
                    //*/

                    if (x_TimelineSlider.Maximum < _sb.GetCurrentTime(this).Value.TotalSeconds)
                        Next_Click(null, null);

                    currentTime = _sb.GetCurrentTime(this).Value.TotalSeconds;
                    return _sb.GetCurrentTime(this).Value.TotalSeconds;
                }
                catch (Exception ex) { ex.ToString(); return 0; }
            }

            set
            {
                if (!x_TimelineSlider.IsMouseCaptureWithin && _playerState == PlayerState.Playing)
                    Play_Click(null, null);

                if (_sliderSeekTime < 0)
                {
                    _sliderSeekTime = value;
                    SliderSeekTimer_Tick(null, null);
                }
                else
                    _sliderSeekTime = value;
            }
        }

        #endregion

        #region Buttons and Sliders

        private enum PlayerState
        {
            Playing, Paused, Stopped
        }

        PlayerState _playerState = PlayerState.Stopped;

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            _sliderStoppedTimer.Stop();
            lock (_sb)
            {
                if (_mt.Source == null)
                {
                    Next_Click(null, null);
                    return;
                }

                switch (_playerState)
                {
                    case PlayerState.Playing:
                        _sb.Pause(this);
                        _playerState = PlayerState.Paused;
                        this.Play.Content = "4";
                        break;
                    case PlayerState.Paused:
                        _sb.Resume(this);
                        _playerState = PlayerState.Playing;
                        this.Play.Content = ";";
                        break;
                    case PlayerState.Stopped:
                        _sb.Begin(this, true);
                        _playerState = PlayerState.Playing;
                        this.Play.Content = ";";
                        break;
                }
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            Uri thisUri = _mt.Source;
            MovieFile mf = ((MovieCollection)this.FindResource("DisplayMovies")).PreviousMovieFile(thisUri);
            PlayMovie(mf);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Uri thisUri = _mt.Source;
            MovieFile mf = ((MovieCollection)this.FindResource("DisplayMovies")).NextMovieFile(thisUri);
            PlayMovie(mf);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _sliderStoppedTimer.Stop();
            lock (_sb)
            {
                _sb.Stop(this);
                _playerState = PlayerState.Stopped;
                this.Play.Content = "4";
            }
        }

        // Resume Movie Playback after release on TimelineSlider
        private void TimelineSlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            lock (_sb)
                if (_playerState == PlayerState.Playing)
                    _sb.Resume(this);
        }

        private void TimelineSlider_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            lock (_sb)
                if (_playerState == PlayerState.Playing)
                    _sb.Resume(this);
        }

        // Change the volume of the media.
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            x_MediaElement.Volume = (double)x_VolumeSlider.Value;
        }

        // Change the speed of the media.
        private void ChangeMediaSpeedRatio(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            lock (_sb)
                _sb.SetSpeedRatio(this, (double)x_SpeedRatioSlider.Value);
        }

        private void ChangeMediaBalance(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            x_MediaElement.Balance = (double)x_BalanceSlider.Value;
        }

        #endregion

        #region Expender Buttons

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button result = (Button)sender;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = result.Content.ToString();
                proc.Start();
            }
            catch (Exception ex) { ex.ToString(); }
        }

        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StackPanel result = (StackPanel)sender;
            UserControl_MediaElement_Ext userControl = (UserControl_MediaElement_Ext)result.Children[1];
            PlayMovie(userControl);
            x_MoviesExpender.IsExpanded = false;
        }

        private void UserControl_MediaElement_Ext_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PlayMovie((UserControl_MediaElement_Ext)sender);
            x_MoviesExpender.IsExpanded = false;
        }

        #endregion

    }
}
