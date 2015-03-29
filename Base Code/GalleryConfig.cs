using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;

// Finalized
// Should Not Alter At All
namespace JMC_Photo_Gallery
{
    public class MediaGallery_Config : ICanXMLExportImport
    {
        private string _imageSupport = ".bmp;.gif;.jpg;.jpe;.jpeg;.jfif;.png;.tif;.tiff";
        private string _videoSupport = ".avi;.wmv;.mpeg;.mpg;.mp4;.mov";
        private Collection<string> _folders = new Collection<string>();
        public string[] Folders
        {
            get
            {
                lock (_folders)
                {
                    string[] result = new string[_folders.Count];
                    _folders.CopyTo(result, 0); return result;
                }
            }
        }
        public void AddFolder(string path)
        {
            if (path == null || path == "")
                return;

            // Prevent scanning its own data store.
            if (path.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                return;

            lock (_folders)
                _folders.Add(path);
        }
        public void Clear()
        {
            _folders.Clear();
        }

        public MediaGallery_Config() { }

        private string[] _imageSupports = null;
        public bool IsImage(string ext)
        {
            if (_imageSupports == null)
                _imageSupports = _imageSupport.ToLower().Split(new char[] { ';' });
            foreach (string supported in _imageSupports)
            {
                if (ext.ToLower() == supported)
                    return true;
            }
            return false;
        }

        private string[] _videoSupports = null;
        public bool IsVideo(string ext)
        {
            if (_videoSupports == null)
                _videoSupports = _videoSupport.ToLower().Split(new char[] { ';' });
            foreach (string supported in _videoSupports)
            {
                if (ext.ToLower() == supported)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Export it to a Config file.
        /// </summary>
        public String ExportXML()
        {
            String result = "<MediaGalleryConfig>\r\n";
            result += "<_imageSupport>" + _imageSupport + "</_imageSupport>\r\n";
            result += "<_videoSupport>" + _videoSupport + "</_videoSupport>\r\n";
            result += "<_folders>\r\n";
            foreach (string folder in _folders)
                result += "<Path>" + folder.Replace("&", "&amp;") + "</Path>\r\n";
            result += "</_folders>\r\n";
            result += "</MediaGalleryConfig>\r\n";
            return result;
        }

        /// <summary>
        /// Parse ToString format. Does not validate directories and files.
        /// </summary>
        public void ImportXML(System.Xml.XmlTextReader reader)
        {
            reader.ReadStartElement("MediaGalleryConfig");

            reader.ReadStartElement("_imageSupport");
            _imageSupport = reader.ReadString();
            reader.ReadEndElement(); //_imageSupport
            reader.ReadStartElement("_videoSupport");
            _videoSupport = reader.ReadString();
            reader.ReadEndElement(); //_videoSupport

            _folders.Clear();
            reader.ReadStartElement("_folders");
            while (reader.IsEmptyElement) reader.Read();
            while (reader.IsStartElement("Path"))
            {
                reader.ReadStartElement("Path");
                AddFolder(reader.ReadString());
                reader.ReadEndElement(); //Path
                while (reader.IsEmptyElement) reader.Read();
            }
            reader.ReadEndElement(); //_folders
            reader.ReadEndElement(); //MediaGalleryConfig
        }
    }
}
