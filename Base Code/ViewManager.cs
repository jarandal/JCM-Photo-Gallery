using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Net;

namespace JMC_Photo_Gallery
{
    public class ViewConfig : ICanXMLExportImport
    {
        public ViewConfig() {}

        // In XML format for exporting.
        public override string ToString()
        {
            return ExportXML();
        }

        public String ExportXML()
        {
            String result = "<ViewManager>\r\n";
            result += "<_L1_size>" + _L1_size + "</_L1_size>\r\n";
            result += "<_L2_size>" + _L2_size + "</_L2_size>\r\n";
            result += "<_L3_size>" + _L3_size + "</_L3_size>\r\n";
            result += "<_L1_count>" + _L1_count + "</_L1_count>\r\n";
            result += "<_L2_count>" + _L2_count + "</_L2_count>\r\n";
            result += "<_L3_count>" + _L3_count + "</_L3_count>\r\n";
            result += "</ViewManager>\r\n";
            return result;
        }

        // In XML format for exporting.
        public void ImportXML(System.Xml.XmlTextReader reader)
        {
            reader.ReadStartElement("ViewManager");
            reader.ReadStartElement("_L1_size"); _L1_size = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_L1_size
            reader.ReadStartElement("_L2_size"); _L2_size = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_L2_size
            reader.ReadStartElement("_L3_size"); _L3_size = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_L3_size
            reader.ReadStartElement("_L1_count"); _L1_count = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_L1_count
            reader.ReadStartElement("_L2_count"); _L2_count = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_L2_count
            reader.ReadStartElement("_L3_count"); _L3_count = int.Parse(reader.ReadString()); reader.ReadEndElement(); //_L3_count
            reader.ReadEndElement(); //ViewManager
        }

        public int _L1_size = 75;
        public int _L2_size = 200;
        public int _L3_size = 600;
        public int _L1_count = 1;
        public int _L2_count = 5;
        public int _L3_count = 100;

    }
}
