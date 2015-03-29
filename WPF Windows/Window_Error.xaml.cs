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
using System.Windows.Shapes;

namespace JMC_Photo_Gallery
{
    /// <summary>
    /// Interaction logic for Window_Error.xaml
    /// </summary>
    public partial class Window_Error : Window
    {
        public Window_Error(String detail)
        {
            InitializeComponent();
            x_ErrorDetail.Text = detail;
        }

        void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Button_Click_Copy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(DataFormats.Text, (Object)x_ErrorDetail.Text);
        }

        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
