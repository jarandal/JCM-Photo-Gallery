using System;
using System.Collections.Generic;
using System.Text;

namespace JMC_Photo_Gallery
{
    public class SlideShowPropertyNode : ICanXMLExportImport
    {
        public String _name;
        public int _imageCount, _duration, _fadeAuto, _fadeManual;

        public SlideShowPropertyNode()
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            _name = "slideshow_standard";
            _imageCount = 1;
            _duration = 10;
            _fadeAuto = 2000;
            _fadeManual = 200;
        }

        public String ExportXML()
        {
            String result = "<SlideShowPropertyNode>\r\n";
            result += "<_name>" + _name + "</_name>\r\n";
            result += "<_imageCount>" + _imageCount.ToString() + "</_imageCount>\r\n";
            result += "<_duration>" + _duration.ToString() + "</_duration>\r\n";
            result += "<_fadeAuto>" + _fadeAuto.ToString() + "</_fadeAuto>\r\n";
            result += "<_fadeManual>" + _fadeManual.ToString() + "</_fadeManual>\r\n";
            result += "</SlideShowPropertyNode>\r\n";
            return result;
        }

        public void ImportXML(System.Xml.XmlTextReader reader)
        {
            String TempName = "slideshow_standard";
            int TempImageCount = 1, TempDuration = 10, TempFadeAuto = 20, TempFadeManual = 20;

            try
            {
                reader.ReadStartElement("SlideShowPropertyNode");
                reader.ReadStartElement("_name"); TempName = reader.ReadString(); reader.ReadEndElement(); //_name
                reader.ReadStartElement("_imageCount"); TempImageCount = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_imageCount
                reader.ReadStartElement("_duration"); TempDuration = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_duration
                reader.ReadStartElement("_fadeAuto"); TempFadeAuto = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_fadeAuto
                reader.ReadStartElement("_fadeManual"); TempFadeManual = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_fadeManual
                reader.ReadEndElement(); //SlideShowPropertyNode
            }
            finally
            {
                _name = TempName;
                _imageCount = TempImageCount;
                _duration = TempDuration;
                _fadeAuto = TempFadeAuto;
                _fadeManual = TempFadeManual;
            }
        }
    }

    public class SlideShowProperties : ICanXMLExportImport
    {
        public int _slide_defaultMode;
        public SlideShowPropertyNode _slide_Mode1, _slide_Mode2, _slide_Mode3, _slide_Mode4;
        public int _screen_defaultMode;
        public SlideShowPropertyNode _screen_Mode1, _screen_Mode2, _screen_Mode3, _screen_Mode4;
        public bool _screen_quitAtMouseMove;

        public SlideShowProperties()
        {
            ResetToDefault();
        }

        public void ResetToDefault()
        {
            _slide_defaultMode = 3;
            _slide_Mode1 = new SlideShowPropertyNode();
            _slide_Mode2 = new SlideShowPropertyNode();
            _slide_Mode3 = new SlideShowPropertyNode();
            _slide_Mode4 = new SlideShowPropertyNode();
            _slide_Mode1._name = "slideshow_standard1";
            _slide_Mode1._imageCount = 1;
            _slide_Mode2._name = "slideshow_standard2";
            _slide_Mode2._imageCount = 1;
            _slide_Mode3._name = "slideshow_scatter1";
            _slide_Mode3._imageCount = 10;
            _slide_Mode4._name = "slideshow_scatter2";
            _slide_Mode4._imageCount = 30;

            _screen_defaultMode = 3;
            _screen_Mode1 = new SlideShowPropertyNode();
            _screen_Mode2 = new SlideShowPropertyNode();
            _screen_Mode3 = new SlideShowPropertyNode();
            _screen_Mode4 = new SlideShowPropertyNode();
            _screen_Mode1._name = "screensaver_standard1";
            _screen_Mode1._imageCount = 1;
            _screen_Mode2._name = "screensaver_standard2";
            _screen_Mode2._imageCount = 1;
            _screen_Mode3._name = "screensaver_scatter1";
            _screen_Mode3._imageCount = 10;
            _screen_Mode4._name = "screensaver_scatter2";
            _screen_Mode4._imageCount = 30;
            _screen_quitAtMouseMove = true;
        }

        public String ExportXML()
        {
            String result = "<SlideShowProperties>\r\n";
            result += "<_slide_defaultMode>" + _slide_defaultMode + "</_slide_defaultMode>\r\n";
            result += _slide_Mode1.ExportXML();
            result += _slide_Mode2.ExportXML();
            result += _slide_Mode3.ExportXML();
            result += _slide_Mode4.ExportXML();
            result += "<_screen_defaultMode>" + _screen_defaultMode + "</_screen_defaultMode>\r\n";
            result += _screen_Mode1.ExportXML();
            result += _screen_Mode2.ExportXML();
            result += _screen_Mode3.ExportXML();
            result += _screen_Mode4.ExportXML();
            result += "<_screen_quitAtMouseMove>" + _screen_quitAtMouseMove.ToString() + "</_screen_quitAtMouseMove>\r\n";
            result += "</SlideShowProperties>\r\n";
            return result;
        }

        public void ImportXML(System.Xml.XmlTextReader reader)
        {
            reader.ReadStartElement("SlideShowProperties");
            reader.ReadStartElement("_slide_defaultMode"); _slide_defaultMode = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_slide_defaultMode
            _slide_Mode1.ImportXML(reader);
            _slide_Mode2.ImportXML(reader);
            _slide_Mode3.ImportXML(reader);
            _slide_Mode4.ImportXML(reader);
            reader.ReadStartElement("_screen_defaultMode"); _screen_defaultMode = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_screen_defaultMode
            _screen_Mode1.ImportXML(reader);
            _screen_Mode2.ImportXML(reader);
            _screen_Mode3.ImportXML(reader);
            _screen_Mode4.ImportXML(reader);
            reader.ReadStartElement("_screen_quitAtMouseMove");
            String tempStr = reader.ReadString().ToLower().Trim();
            _screen_quitAtMouseMove = (tempStr != "false");
            reader.ReadEndElement(); //_screen_quitAtMouseMove
            reader.ReadEndElement(); //SlideShowProperties
        }
    }

}
