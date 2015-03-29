using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JMC_Photo_Gallery
{
    /// <summary>
    /// Interaction logic for Window_Config.xaml
    /// </summary>
    public partial class Window_Config : Window
    {
        private MediaGallery_Config _MG;
        private SlideShowProperties _SP;

        public Window_Config()
        {
            _MG = AR.GalleryConfig;
            _SP = AR.SlideShowProperties;

            InitializeComponent();
            Refresh_GeneralTab();
            Refresh_SlideshowTab();
        }

        void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Documents.Hyperlink tempLink = (System.Windows.Documents.Hyperlink)sender;
                System.Diagnostics.Process.Start(tempLink.NavigateUri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link that was clicked.\r\n" + ex.ToString());
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Save_GeneralTab();
            Save_SlideshowTab();

            AR.ImportConfigs.StartExportAll();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #region GeneralTab
        // ======================================================================
        // ======================================================================
        // ======================================================================

        private void Refresh_GeneralTab()
        {
            this._galleryList.Items.Clear();
            foreach (string item in this._MG.Folders)
                this._galleryList.Items.Add(item);
        }

        private void Save_GeneralTab()
        {
            if (GalleryChanged())
            {
                _MG.Clear();
                foreach (string path in this._galleryList.Items)
                    _MG.AddFolder(path);
            }
        }

        private bool GalleryChanged()
        {
            if (this._galleryList.Items.Count != _MG.Folders.Length)
                return true;
            for (int i=0; i<this._galleryList.Items.Count; i++)
            {
                if ((string)this._galleryList.Items[i] != _MG.Folders[i])
                    return true;
            }
            return false;
        }

        private void GalleryAdd_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog temp = new System.Windows.Forms.FolderBrowserDialog();
            temp.Description = "Select your gallery folder. Subfolders are included automatically.";
            temp.ShowDialog();
            if (temp.SelectedPath != "" && new System.IO.DirectoryInfo(temp.SelectedPath).Exists)
                this._galleryList.Items.Add(temp.SelectedPath);
        }

        private void GalleryRemove_Click(object sender, RoutedEventArgs e)
        {
            if (this._galleryList.SelectedIndex >= 0)
                this._galleryList.Items.RemoveAt(this._galleryList.SelectedIndex);
        }

        private void GalleryMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (this._galleryList.SelectedIndex > 0)
            {
                int tempInt = this._galleryList.SelectedIndex;
                String tempStr = this._galleryList.SelectedItem.ToString();
                this._galleryList.Items.RemoveAt(tempInt);
                tempInt--;
                this._galleryList.Items.Insert(tempInt, tempStr);
                this._galleryList.SelectedIndex = tempInt;
            }
        }

        private void GalleryMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (this._galleryList.SelectedIndex + 1 < this._galleryList.Items.Count)
            {
                int tempInt = this._galleryList.SelectedIndex;
                String tempStr = this._galleryList.SelectedItem.ToString();
                this._galleryList.Items.RemoveAt(tempInt);
                tempInt++;
                this._galleryList.Items.Insert(tempInt, tempStr);
                this._galleryList.SelectedIndex = tempInt;
            }
        }

        #endregion

        #region SlideshowTab
        // ======================================================================
        // ======================================================================
        // ======================================================================

        private void Refresh_SlideshowTab()
        {
            if (this._SP._slide_defaultMode > 4)
                this._SP._slide_defaultMode = 4;
            if (this._SP._slide_defaultMode < 1)
                this._SP._slide_defaultMode = 1;
            if (this._SP._screen_defaultMode > 4)
                this._SP._screen_defaultMode = 4;
            if (this._SP._screen_defaultMode < 1)
                this._SP._screen_defaultMode = 1;
            this._slideDefaultMode.SelectedIndex = this._SP._slide_defaultMode - 1;
            this._saverDefaultMode.SelectedIndex = this._SP._screen_defaultMode - 1;

            if (this._SP._screen_quitAtMouseMove)
                this._saverQuitWhenMouseMove.SelectedIndex = 0;
            else
                this._saverQuitWhenMouseMove.SelectedIndex = 1;

            this._slideImgCount1.Text = this._SP._slide_Mode1._imageCount.ToString();
            this._slideImgCount2.Text = this._SP._slide_Mode2._imageCount.ToString();
            this._slideImgCount3.Text = this._SP._slide_Mode3._imageCount.ToString();
            this._slideImgCount4.Text = this._SP._slide_Mode4._imageCount.ToString();
            this._slideDuration1.Text = this._SP._slide_Mode1._duration.ToString();
            this._slideDuration2.Text = this._SP._slide_Mode2._duration.ToString();
            this._slideDuration3.Text = this._SP._slide_Mode3._duration.ToString();
            this._slideDuration4.Text = this._SP._slide_Mode4._duration.ToString();
            this._slideFadeA1.Text = this._SP._slide_Mode1._fadeAuto.ToString();
            this._slideFadeA2.Text = this._SP._slide_Mode2._fadeAuto.ToString();
            this._slideFadeA3.Text = this._SP._slide_Mode3._fadeAuto.ToString();
            this._slideFadeA4.Text = this._SP._slide_Mode4._fadeAuto.ToString();
            this._slideFadeM1.Text = this._SP._slide_Mode1._fadeManual.ToString();
            this._slideFadeM2.Text = this._SP._slide_Mode2._fadeManual.ToString();
            this._slideFadeM3.Text = this._SP._slide_Mode3._fadeManual.ToString();
            this._slideFadeM4.Text = this._SP._slide_Mode4._fadeManual.ToString();

            this._saverImgCount1.Text = this._SP._screen_Mode1._imageCount.ToString();
            this._saverImgCount2.Text = this._SP._screen_Mode2._imageCount.ToString();
            this._saverImgCount3.Text = this._SP._screen_Mode3._imageCount.ToString();
            this._saverImgCount4.Text = this._SP._screen_Mode4._imageCount.ToString();
            this._saverDuration1.Text = this._SP._screen_Mode1._duration.ToString();
            this._saverDuration2.Text = this._SP._screen_Mode2._duration.ToString();
            this._saverDuration3.Text = this._SP._screen_Mode3._duration.ToString();
            this._saverDuration4.Text = this._SP._screen_Mode4._duration.ToString();
            this._saverFadeA1.Text = this._SP._screen_Mode1._fadeAuto.ToString();
            this._saverFadeA2.Text = this._SP._screen_Mode2._fadeAuto.ToString();
            this._saverFadeA3.Text = this._SP._screen_Mode3._fadeAuto.ToString();
            this._saverFadeA4.Text = this._SP._screen_Mode4._fadeAuto.ToString();
            this._saverFadeM1.Text = this._SP._screen_Mode1._fadeManual.ToString();
            this._saverFadeM2.Text = this._SP._screen_Mode2._fadeManual.ToString();
            this._saverFadeM3.Text = this._SP._screen_Mode3._fadeManual.ToString();
            this._saverFadeM4.Text = this._SP._screen_Mode4._fadeManual.ToString();
        }

        private void Save_SlideshowTab()
        {
            AR.SlideShowProperties._slide_defaultMode = this._slideDefaultMode.SelectedIndex + 1;
            AR.SlideShowProperties._screen_defaultMode = this._saverDefaultMode.SelectedIndex + 1;
            if (this._saverQuitWhenMouseMove.SelectedIndex == 0)
                AR.SlideShowProperties._screen_quitAtMouseMove = true;
            else
                AR.SlideShowProperties._screen_quitAtMouseMove = false;

            AR.SlideShowProperties._slide_Mode1._imageCount = Int32.Parse(this._slideImgCount1.Text);
            AR.SlideShowProperties._slide_Mode2._imageCount = Int32.Parse(this._slideImgCount2.Text);
            AR.SlideShowProperties._slide_Mode3._imageCount = Int32.Parse(this._slideImgCount3.Text);
            AR.SlideShowProperties._slide_Mode4._imageCount = Int32.Parse(this._slideImgCount4.Text);
            AR.SlideShowProperties._slide_Mode1._duration = Int32.Parse(this._slideDuration1.Text);
            AR.SlideShowProperties._slide_Mode2._duration = Int32.Parse(this._slideDuration2.Text);
            AR.SlideShowProperties._slide_Mode3._duration = Int32.Parse(this._slideDuration3.Text);
            AR.SlideShowProperties._slide_Mode4._duration = Int32.Parse(this._slideDuration4.Text);
            AR.SlideShowProperties._slide_Mode1._fadeAuto = Int32.Parse(this._slideFadeA1.Text);
            AR.SlideShowProperties._slide_Mode2._fadeAuto = Int32.Parse(this._slideFadeA2.Text);
            AR.SlideShowProperties._slide_Mode3._fadeAuto = Int32.Parse(this._slideFadeA3.Text);
            AR.SlideShowProperties._slide_Mode4._fadeAuto = Int32.Parse(this._slideFadeA4.Text);
            AR.SlideShowProperties._slide_Mode1._fadeManual = Int32.Parse(this._slideFadeM1.Text);
            AR.SlideShowProperties._slide_Mode2._fadeManual = Int32.Parse(this._slideFadeM2.Text);
            AR.SlideShowProperties._slide_Mode3._fadeManual = Int32.Parse(this._slideFadeM3.Text);
            AR.SlideShowProperties._slide_Mode4._fadeManual = Int32.Parse(this._slideFadeM4.Text);

            AR.SlideShowProperties._screen_Mode1._imageCount = Int32.Parse(this._saverImgCount1.Text);
            AR.SlideShowProperties._screen_Mode2._imageCount = Int32.Parse(this._saverImgCount2.Text);
            AR.SlideShowProperties._screen_Mode3._imageCount = Int32.Parse(this._saverImgCount3.Text);
            AR.SlideShowProperties._screen_Mode4._imageCount = Int32.Parse(this._saverImgCount4.Text);
            AR.SlideShowProperties._screen_Mode1._duration = Int32.Parse(this._saverDuration1.Text);
            AR.SlideShowProperties._screen_Mode2._duration = Int32.Parse(this._saverDuration2.Text);
            AR.SlideShowProperties._screen_Mode3._duration = Int32.Parse(this._saverDuration3.Text);
            AR.SlideShowProperties._screen_Mode4._duration = Int32.Parse(this._saverDuration4.Text);
            AR.SlideShowProperties._screen_Mode1._fadeAuto = Int32.Parse(this._saverFadeA1.Text);
            AR.SlideShowProperties._screen_Mode2._fadeAuto = Int32.Parse(this._saverFadeA2.Text);
            AR.SlideShowProperties._screen_Mode3._fadeAuto = Int32.Parse(this._saverFadeA3.Text);
            AR.SlideShowProperties._screen_Mode4._fadeAuto = Int32.Parse(this._saverFadeA4.Text);
            AR.SlideShowProperties._screen_Mode1._fadeManual = Int32.Parse(this._saverFadeM1.Text);
            AR.SlideShowProperties._screen_Mode2._fadeManual = Int32.Parse(this._saverFadeM2.Text);
            AR.SlideShowProperties._screen_Mode3._fadeManual = Int32.Parse(this._saverFadeM3.Text);
            AR.SlideShowProperties._screen_Mode4._fadeManual = Int32.Parse(this._saverFadeM4.Text);
        }

        #endregion

    }
}
