using System;
using System.Windows;
using System.Windows.Data;
using System.Security.Permissions;

namespace JMC_Photo_Gallery
{
    public partial class app : Application
    {
        static void ErrorHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Window_Error tempW = new Window_Error(e.ToString());
            tempW.ShowDialog();
            System.Environment.Exit(1);
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        void AppStartup(object sender, StartupEventArgs args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ErrorHandler);
            AR.Init();

            // ScreenSaver Support
            string[] _args = args.Args;
            //_args = new string[1];
            //_args[0] = "/s";
            if (_args.Length > 0)
            {
                // Get the 2 character command line argument
                string arg = _args[0].ToLower().Trim().Substring(0, 2);
                switch (arg)
                {
                    case "/c":
                        // Show the options dialog
                        Window_Config win = new Window_Config();
                        win._tabSlideshow.IsSelected = true;
                        win.Show();
                        break;
                    case "/s":
                        // Show screensaver form
                        ShowScreensaver();
                        break;
                    case "/p":
                        // Don't do anything for preview
                        Application.Current.Shutdown();
                        break;
                    default:
                        Application.Current.Shutdown();
                        break;
                }

                // needed or the application would not end.
                AR.Window_PhotoGallery.Close();
                AR.End();
            }
            else
            {
                AR.Window_PhotoGallery.Show();
            }
        }

        void ShowScreensaver()
        {
            Window_SlideShow.IsRunningScreenSaver = true;
            Window_SlideShow slideShow = new Window_SlideShow();
            slideShow.Show();

            System.Collections.ObjectModel.Collection<string> paths = new System.Collections.ObjectModel.Collection<string>();
            foreach (string collection in AR.GalleryConfig.Folders)
                GetScreenSaverPaths(collection, paths);
            slideShow.Load(paths);
        }

        private void GetScreenSaverPaths(string folderPath, System.Collections.ObjectModel.Collection<string> result)
        {
            try
            {
                // Prevent scanning its own data store.
                if (folderPath.ToLower().Contains(AR._MyGalleryDataPath.ToLower()))
                    return;

                System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(folderPath);

                //JAL 20150329
                if (dirInfo.Name.StartsWith(".")) return;

                int count = 0;
                foreach (System.IO.FileInfo file in new System.IO.DirectoryInfo(folderPath).GetFiles())
                {
                    if (count >= 5)
                        break;
                    if (AR.GalleryConfig.IsImage(file.Extension))
                    {
                        result.Add(file.FullName);
                        count++;
                    }
                }

                foreach (System.IO.DirectoryInfo dir in new System.IO.DirectoryInfo(folderPath).GetDirectories())
                {
                    //JAL 20150329
                    if (dir.Name.StartsWith(".")) return;
                    GetScreenSaverPaths(dir.FullName, result);
                }
                    
            }
            catch { }
        }

        void AppExit(object sender, ExitEventArgs args)
        {
            AR.End();
        }

    }
}