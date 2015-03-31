using System;
using System.Text;
using System.IO;
// Disabled because XML based Gallery Database is not reliable. Limited Character Count.
// Please Search Above Comment for code lines. 3 Lines Total

namespace JMC_Photo_Gallery
{
    public class AR
    { 
        #region Import Export Setting Classes

        public class ImportNode
        {
            public String TaskName, FilePath;
            public ICanXMLExportImport Obj;
            public ImportNode(String TaskName, String FilePath, ICanXMLExportImport Obj)
            {
                this.TaskName = TaskName;
                this.FilePath = FilePath;
                this.Obj = Obj;
            }
        }

        public class ImportList : System.Collections.ObjectModel.Collection<ImportNode>
        {
            public void StartImport(String TaskName)
            {
                foreach (ImportNode tempNode in this)
                {
                    if (tempNode.TaskName.Trim().ToLower() == TaskName.Trim().ToLower())
                    {
                        ParseConfigCodeSnippet(tempNode.TaskName, tempNode.FilePath, tempNode.Obj);
                        return;
                    }
                }
                System.Windows.MessageBox.Show("Resource Task: " + TaskName + " is not available!", "System Error!");
            }

            public void StartImportAll()
            {
                foreach (ImportNode tempNode in this)
                    ParseConfigCodeSnippet(tempNode.TaskName, tempNode.FilePath, tempNode.Obj);
            }

            // Parse Configuration Files. Standarized Procedure.
            private void ParseConfigCodeSnippet(String purpose, String path, ICanXMLExportImport obj)
            {
                System.Xml.XmlTextReader reader = null;
                if (new FileInfo(path).Exists)
                {
                    try
                    {
                        reader = new System.Xml.XmlTextReader(path);
                        obj.ImportXML(reader);
                    }
                    catch (Exception e)
                    {
                        FileInfo tempFileInfo = new FileInfo(path);
                        DateTime tempDateTime = System.DateTime.Now;
                        FileInfo moveToPath = new FileInfo(_ErrorConfigFolderPath + "\\" + tempFileInfo.Name + " " +
                            tempDateTime.Year + "-" + tempDateTime.Month + "-" + tempDateTime.Day + " " +
                            tempDateTime.Hour + "-" + tempDateTime.Minute + "-" + tempDateTime.Second +
                            tempFileInfo.Extension);
                        System.Windows.MessageBox.Show("Unable to setup " + purpose + ".\r\n" +
                            "Default Applied.\r\n" +
                            "File: " + tempFileInfo.FullName + "\r\n" +
                            "Moved To: " + moveToPath.FullName + "\r\n" +
                            "Error Message:\r\n" + e.ToString(), "Parse Error");
                        if (!moveToPath.Directory.Exists)
                            moveToPath.Directory.Create();
                        tempFileInfo.CopyTo(moveToPath.FullName, true);
                    }
                    try { reader.Close(); }
                    catch (Exception e) { e.ToString(); }
                }
                else
                {
                    System.Windows.MessageBox.Show(new FileInfo(path).FullName + " is missing.\r\n" +
                        "Default Applied.\r\n", "File Missing");
                }
            }

            public void StartExport(String TaskName)
            {
                foreach (ImportNode tempNode in this)
                {
                    if (tempNode.TaskName.Trim().ToLower() == TaskName.Trim().ToLower())
                    {
                        WriteToFile(tempNode.Obj.ExportXML(), tempNode.FilePath);
                        return;
                    }
                }
                System.Windows.MessageBox.Show("Resource Task: " + TaskName + " is not available!", "System Error!");
            }

            public void StartExportAll()
            {
                foreach (ImportNode tempNode in this)
                    WriteToFile(tempNode.Obj.ExportXML(), tempNode.FilePath);
            }

            private void WriteToFile(String text, String outPath)
            {
                FileInfo _tempFileInfo = new FileInfo(outPath);
                if (!_tempFileInfo.Directory.Exists)
                    _tempFileInfo.Directory.Create();

                TextWriter tw = new StreamWriter(outPath);
                tw.WriteLine(text);
                tw.Close();
            }

        }

        #endregion

        // System Paths
        public static String _MyGalleryDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\JMC PG";
        public static String _MiniThumbDBPath = _MyGalleryDataPath + @"\DB1";
        public static String _ThumbDBPath = _MyGalleryDataPath + @"\DB2";
        public static String _MovieThumbDBPath = _MyGalleryDataPath + @"\DBM";
        private static String _ErrorConfigFolderPath = _MyGalleryDataPath + @"\Failed Config Files";

        // Objects
        private static Window_PhotoGallery _Window_PhotoGallery;
        public static Window_PhotoGallery Window_PhotoGallery { get { return _Window_PhotoGallery; } }
        private static MediaGallery_Config _GalleryConfig;
        public static MediaGallery_Config GalleryConfig { get { return _GalleryConfig; } }
        private static ViewConfig _ViewConfig;
        public static ViewConfig ViewConfig { get { return _ViewConfig; } }

        private static ICanGetThumbPath _GrandThumbPathMaker;
        public static ICanGetThumbPath GrandThumbPathMaker { get { return _GrandThumbPathMaker; } }
        private static ThumbManagerAsyncRunner _GrandThumbMaker;
        public static ThumbManagerAsyncRunner GrandThumbMaker { get { return _GrandThumbMaker; } }

        // Objects of Extras
        private static SlideShowProperties _SlideShowProperties;
        public static SlideShowProperties SlideShowProperties { get { return _SlideShowProperties; } }

        // Resource Stack
        private static ImportList _ImportConfigs;
        public static ImportList ImportConfigs { get { return _ImportConfigs; } }

        public static void Init()
        {
            Window_StartupScreen _Window_StartupScreen = new Window_StartupScreen(); // Has build in Skin Manager
            _Window_StartupScreen.Show();
            try
            {
                // Create Obj and Run Threads
                _Window_PhotoGallery = new Window_PhotoGallery();
                _GalleryConfig = new MediaGallery_Config();
                _ViewConfig = new ViewConfig();
                _GrandThumbPathMaker = new ThumbPathManager(AR._MiniThumbDBPath, AR._ThumbDBPath);
                _GrandThumbMaker = new ThumbManagerAsyncRunner(_GrandThumbPathMaker);
                UserControl_ManagedImage._thumbPathMaker = _GrandThumbPathMaker;
                UserControl_ManagedImage.s_ActivateThreads();

                // Create Obj For Extras
                _SlideShowProperties = new SlideShowProperties();

                // Setup preferences and searching photos
                InitImportList();
                SetupConfig();
                _GrandThumbMaker.StartThread();
                foreach (string rootFolder in _GalleryConfig.Folders)
                    _GrandThumbMaker.AddRoot(rootFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Unexpected AR.Init() Error!\nThis program will not run properly!\n\n"
                    + ex.ToString(), "Internal Error");
            }
            _Window_StartupScreen.Close();
        }

        private static void InitImportList()
        {
            _ImportConfigs = new ImportList();
            _ImportConfigs.Add(new ImportNode("Gallery Configuration", _MyGalleryDataPath + @"\Config\Gallery Config.xml", _GalleryConfig));
            _ImportConfigs.Add(new ImportNode("View Configuration", _MyGalleryDataPath + @"\Config\View Config.xml", _ViewConfig));
            _ImportConfigs.Add(new ImportNode("Slideshow Configuration", _MyGalleryDataPath + @"\Config\Slideshow Config.xml", _SlideShowProperties));
        }

        // Setup Conffigurations from the Config files.
        private static void SetupConfig()
        {
            if (new DirectoryInfo(_MyGalleryDataPath).Exists == false)
                _GalleryConfig.AddFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            else
                _ImportConfigs.StartImportAll();

            // Export
            _ImportConfigs.StartExportAll();
        }

        public static void End()
        {
            AR._GrandThumbMaker.EndThread();
            UserControl_ManagedImage.s_DeactivateThreads();
        }
    }
}
