using System;

namespace JMC_Photo_Gallery
{

    public interface ICanXMLExportImport
    {
        String ExportXML();
        void ImportXML(System.Xml.XmlTextReader reader);
    }

    public interface ICanGetThumbPath
    {
        string GetMiniThumbPath(string path, bool checkExist);
        string GetThumbPath(string path, bool checkExist);
        string GetMiniSourcePath(string thumbPath, bool checkExist);
        string GetSourcePath(string thumbPath, bool checkExist);
    }

    public interface ICanGetThumbBitmapImage
    {
        System.Windows.Media.Imaging.BitmapImage GetMiniThumb_BitmapImage(string path, int decodeWidth);
        System.Windows.Media.Imaging.BitmapImage GetThumb_BitmapImage(string path, int decodeWidth);
    }

}
