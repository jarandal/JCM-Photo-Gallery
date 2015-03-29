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
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace JMC_Photo_Gallery
{
    /// <summary>
    /// Interaction logic for Window_SlideShow_2D_1.xaml
    /// </summary>
    public partial class Window_SlideShow : Window
    {
        // Resources
        private ImageFolder_SS _DisplayFolderIn;
        private ImageFolder_SS _DisplayFolderOutBack;
        private ImageFolder_SS _DisplayFolderOutFront;

        // For auto play
        private System.Windows.Threading.DispatcherTimer _SlideTicker = new System.Windows.Threading.DispatcherTimer();
        public TimeSpan Interval { get { return _SlideTicker.Interval; } set { _SlideTicker.Interval = value; } }
        public bool IsPlaying { get { return _SlideTicker.IsEnabled; } set { _SlideTicker.IsEnabled = value; } }

        // For control fade in/out
        private Point _mousePoint = new Point(0, 0);
        private bool _panelIsActive = true;
        private System.Windows.Threading.DispatcherTimer _PanelTicker = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _MouseTicker = new System.Windows.Threading.DispatcherTimer();

        // Settings
        public static bool IsRunningScreenSaver = false;
        private int _startMode = 1;
        private SlideShowPropertyNode _modeCurrent;
        private SlideShowPropertyNode[] _modeArray = new SlideShowPropertyNode[4];

        // Actual Collection
        private CollectionRound<String> _contents;
        private int _currentIndex = 0;

        // Constructor
        public Window_SlideShow()
        {
            InitializeComponent();

            // Resources
            _DisplayFolderIn = (ImageFolder_SS)this.FindResource("OnScreenFolder");
            _DisplayFolderOutBack = (ImageFolder_SS)this.FindResource("OnScreenFolderOutBack");
            _DisplayFolderOutFront = (ImageFolder_SS)this.FindResource("OnScreenFolderOutFront");

            // For auto play
            _SlideTicker.Interval = TimeSpan.FromSeconds(10);
            _SlideTicker.IsEnabled = true;
            _SlideTicker.Tick += new EventHandler(dispatcherTimer_SlideTicker);

            // For control fade in/out
            _PanelTicker.Tick += new EventHandler(dispatcherTimer_PanelTicker);
            _PanelTicker.Interval = new TimeSpan(0, 0, 5);
            _MouseTicker.Tick += new EventHandler(dispatcherTimer_MouseTicker);
            _MouseTicker.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _MouseTicker.Start();
            SetMyImagePopStoryBoards();

            // Settings
            if (!IsRunningScreenSaver)
            {
                _startMode = AR.SlideShowProperties._slide_defaultMode - 1;
                _modeArray[0] = AR.SlideShowProperties._slide_Mode1;
                _modeArray[1] = AR.SlideShowProperties._slide_Mode2;
                _modeArray[2] = AR.SlideShowProperties._slide_Mode3;
                _modeArray[3] = AR.SlideShowProperties._slide_Mode4;
            }
            else
            {
                _startMode = AR.SlideShowProperties._screen_defaultMode - 1;
                _modeArray[0] = AR.SlideShowProperties._screen_Mode1;
                _modeArray[1] = AR.SlideShowProperties._screen_Mode2;
                _modeArray[2] = AR.SlideShowProperties._screen_Mode3;
                _modeArray[3] = AR.SlideShowProperties._screen_Mode4;
            }
        }

        #region Image Loaders

        public void Load(Collection<String> contents, bool UseMode1 = false)
        {
            Window_Processing processingWin = new Window_Processing();
            processingWin.Show();

            try
            {
                // Set View Size
                ImageFile_SS.ViewWidth = (int)this.ActualWidth;
                ImageFile_SS.ViewHeight = (int)this.ActualHeight;
                Console.WriteLine("Load ImageFile.ViewWidth = " + ImageFile_SS.ViewWidth);
                Console.WriteLine("Load ImageFile.ViewHeight = " + ImageFile_SS.ViewHeight);

                // Save actual contents
                _contents = new CollectionRound<string>();
                foreach(var item in contents)
                    _contents.Add(item);

                // Apply Style
                // For Normal Window State
                if (UseMode1)
                    Mode.SelectedIndex = 0;
                else
                {
                    if (Mode.SelectedIndex == _startMode)
                        Mode_SelectionChanged(null, null);
                    else
                        Mode.SelectedIndex = _startMode;
                }

                // Play
                this.PanelPlayClick();
            }
            catch { }

            processingWin.Close();
        }

        private void Load(int usingMode)
        {
            // Determin settings
            int imageCount = _modeArray[usingMode]._imageCount;
            imageCount = Math.Min(imageCount, _contents.Count);

            // Add to display
            _DisplayFolderIn._images.Clear();
            for (int i = 0; i < imageCount; i++)
            {
                string path = _contents.GetRound(i + _currentIndex - imageCount + 1);
                string thumbPath = AR.GrandThumbPathMaker.GetThumbPath(path, true);
                ImageFile_SS image = new ImageFile_SS(path, (thumbPath != null) ? thumbPath : path);
                image.RandomProperties();
                _DisplayFolderIn._images.Add(image);
            }
        }

        private void PreviousImage(bool IsFast)
        {
            double timeDouble = (IsFast) ? _modeCurrent._fadeManual : _modeCurrent._fadeAuto;
            TimeSpan time = TimeSpan.FromMilliseconds((double)timeDouble);
            FadeInPanel.FadeTime = time;
            FadeOutPanel.FadeTime = time;

            // This is for Fade In
            _currentIndex--;
            string path = _contents.GetRound(_currentIndex - _DisplayFolderIn._images.Count + 1);
            string thumbPath = AR.GrandThumbPathMaker.GetThumbPath(path, true);
            ImageFile_SS image = new ImageFile_SS(path, (thumbPath != null) ? thumbPath : path);
            image.RandomProperties();
            ImageFile_SS imageOut = _DisplayFolderIn._images[_DisplayFolderIn._images.Count - 1];
            _DisplayFolderIn._images.RemoveAt(_DisplayFolderIn._images.Count - 1);
            _DisplayFolderIn._images.Insert(0, image);

            // This if for Fade Out Front
            while (_DisplayFolderOutFront._images.Count > 10)
                _DisplayFolderOutFront._images.RemoveAt(0);
            _DisplayFolderOutFront._images.Add(imageOut);
        }

        private void NextImage(bool IsFast)
        {
            double timeDouble = (IsFast) ? _modeCurrent._fadeManual : _modeCurrent._fadeAuto;
            TimeSpan time = TimeSpan.FromMilliseconds((double)timeDouble);
            FadeInPanel.FadeTime = time;
            FadeOutPanel.FadeTime = time;

            // This is for Fade In
            _currentIndex++;
            string path = _contents.GetRound(_currentIndex);
            string thumbPath = AR.GrandThumbPathMaker.GetThumbPath(path, true);
            ImageFile_SS image = new ImageFile_SS(path, (thumbPath != null) ? thumbPath : path);
            image.RandomProperties();
            ImageFile_SS imageOut = _DisplayFolderIn._images[0];
            _DisplayFolderIn._images.RemoveAt(0);
            _DisplayFolderIn._images.Add(image);

            // This if for Fade Out Back
            while (_DisplayFolderOutBack._images.Count > 10)
                _DisplayFolderOutBack._images.RemoveAt(0);
            _DisplayFolderOutBack._images.Add(imageOut);
        }

        #endregion

        #region Buttons

        private void ArrowKey_Press(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right || e.Key == Key.Down)
            {
                Next_Click(null, null);
            }
            else if (e.Key == Key.Left || e.Key == Key.Up)
            {
                Previous_Click(null, null);
            }
            else if (e.Key == Key.Space)
            {
                Play_Click(null, null);
            }
            else if (e.Key == Key.M)
            {
                if (this.Mode.SelectedIndex + 1 >= this.Mode.Items.Count)
                    this.Mode.SelectedIndex = 0;
                else
                    this.Mode.SelectedIndex = this.Mode.SelectedIndex + 1;
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void ExitSlideShow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            PanelPauseClick();
            PreviousImage(true);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            PanelPauseClick();
            NextImage(true);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            // 4 == play
            // ; == Pause
            if (this.Play.Content.ToString() == "4")
                PanelPlayClick();
            else
                PanelPauseClick();
        }

        private void PanelPlayClick()
        {
            this.Play.Content = ";";
            this.IsPlaying = true;
        }

        private void PanelPauseClick()
        {
            this.Play.Content = "4";
            this.IsPlaying = false;
        }

        private void Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComboBox result = (ComboBox)sender; // comment out because I will call this mothod with null values.
            switch (Mode.SelectedIndex)
            {
                case 0:
                    PanelStandardMode1Click();
                    break;
                case 1:
                    PanelStandardMode2Click();
                    break;
                case 2:
                    PanelScatterMode1Click();
                    break;
                case 3:
                    PanelScatterMode2Click();
                    break;
                default:
                    PanelStandardMode1Click();
                    break;
            }
        }

        private void PanelStandardMode1Click()
        {
            if (_modeArray[0] == null)
                return;

            // load standard mode
            ImageFile_SS.MinScale = 100;
            ImageFile_SS.MaxScale = 100;
            ImageFile_SS.MinRotation = 0;
            ImageFile_SS.MaxRotation = 0;
            ImageFile_SS.LocationRandom = false;
            ImageFile_SS.ViewExpandThickness = 0;
            ImageFile_SS.BorderThicknessG = 0;

            _modeCurrent = _modeArray[0];
            Load(0);
        }

        private void PanelStandardMode2Click()
        {
            if (_modeArray[1] == null)
                return;

            // load standard mode
            ImageFile_SS.MinScale = 60;
            ImageFile_SS.MaxScale = 80;
            ImageFile_SS.MinRotation = 0;
            ImageFile_SS.MaxRotation = 0;
            ImageFile_SS.LocationRandom = true;
            ImageFile_SS.ViewExpandThickness = 0;
            ImageFile_SS.BorderThicknessG = (int)(this.ActualHeight * 0.02);
            if (ImageFile_SS.BorderThicknessG > 20)
                ImageFile_SS.BorderThicknessG = 20;

            _modeCurrent = _modeArray[1];
            Load(1);
        }

        private void PanelScatterMode1Click()
        {
            if (_modeArray[2] == null)
                return;

            // load scatter mode
            ImageFile_SS.MinScale = 40;
            ImageFile_SS.MaxScale = 60;
            ImageFile_SS.MinRotation = -45;
            ImageFile_SS.MaxRotation = 45;
            ImageFile_SS.LocationRandom = true;
            ImageFile_SS.ViewExpandThickness = (int)(this.ActualHeight * 0.1);
            ImageFile_SS.BorderThicknessG = (int)(this.ActualHeight * 0.02);
            if (ImageFile_SS.BorderThicknessG > 20)
                ImageFile_SS.BorderThicknessG = 20;

            _modeCurrent = _modeArray[2];
            Load(2);
        }

        private void PanelScatterMode2Click()
        {
            if (_modeArray[3] == null)
                return;

            // load scatter mode
            ImageFile_SS.MinScale = 20;
            ImageFile_SS.MaxScale = 40;
            ImageFile_SS.MinRotation = 0;
            ImageFile_SS.MaxRotation = 0;
            ImageFile_SS.LocationRandom = true;
            ImageFile_SS.ViewExpandThickness = (int)(this.ActualHeight * 0.1);
            ImageFile_SS.BorderThicknessG = (int)(this.ActualHeight * 0.02);
            if (ImageFile_SS.BorderThicknessG > 20)
                ImageFile_SS.BorderThicknessG = 20;

            _modeCurrent = _modeArray[3];
            Load(3);
        }

        #endregion

        #region disPatcherTimer

        private void dispatcherTimer_SlideTicker(object sender, EventArgs e)
        {
            NextImage(false);
        }

        private void dispatcherTimer_PanelTicker(object sender, EventArgs e)
        {
            DeactivatePanel();
        }

        private void ActivatePanel()
        {
            _panelIsActive = true;
            this.myFadeInStoryboard.Begin(PlaybackPanel);
        }

        private void DeactivatePanel()
        {
            _PanelTicker.Stop();
            _panelIsActive = false;
            this.myFadeOutStoryboard.Begin(PlaybackPanel);
        }

        private void dispatcherTimer_MouseTicker(object sender, EventArgs e)
        {
            if (IsRunningScreenSaver && AR.SlideShowProperties._screen_quitAtMouseMove &&
                _mousePoint != new Point(0, 0) && _mousePoint != Mouse.GetPosition(this))
                this.Close();

            if (_panelIsActive)
            {
                if (_mousePoint != Mouse.GetPosition(this))
                {
                    _mousePoint.X = Mouse.GetPosition(this).X;
                    _mousePoint.Y = Mouse.GetPosition(this).Y;
                    _PanelTicker.Stop();
                }
                else if (!_PanelTicker.IsEnabled)
                {
                    _PanelTicker.Start();
                }
            }
            else if (_mousePoint != Mouse.GetPosition(this))
            {
                _mousePoint.X = Mouse.GetPosition(this).X;
                _mousePoint.Y = Mouse.GetPosition(this).Y;
                ActivatePanel();
            }
        }

        #endregion

        #region Animations

        private Storyboard myFadeInStoryboard = new Storyboard();
        private Storyboard myFadeOutStoryboard = new Storyboard();
        private void SetMyImagePopStoryBoards()
        {
            DoubleAnimation myDoubleAnimation;

            // Fade In Panel
            myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.To = 1;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            myDoubleAnimation.Completed += delegate(object sender2, EventArgs e2)
            {
            };
            Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(Border.OpacityProperty));
            myFadeInStoryboard.Children.Add(myDoubleAnimation);

            // Fade Out
            myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.To = 0;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));
            myDoubleAnimation.Completed += delegate(object sender2, EventArgs e2)
            {
            };
            Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(Border.OpacityProperty));
            myFadeOutStoryboard.Children.Add(myDoubleAnimation);
        }

        #endregion

    }
}
