using System;
using System.IO;

namespace JMC_Photo_Gallery
{
    // Manage Thumb Database.
    // Will never alter the original files.
    public class ThumbPathManager : ICanGetThumbPath
    {
        private string _MiniDBPath = "";
        private string _ThumbDBPath = "";

        public ThumbPathManager(string MiniDBPath, string ThumbDBPath)
        {
            _MiniDBPath = MiniDBPath;
            _ThumbDBPath = ThumbDBPath;
        }

        public string GetMiniThumbPath(string path, bool checkExist)
        {
            return GetThumbPath(_MiniDBPath, path, checkExist);
        }

        public string GetThumbPath(string path, bool checkExist)
        {
            return GetThumbPath(_ThumbDBPath, path, checkExist);
        }

        private string GetThumbPath(string DBPath, string Path, bool checkExist)
        {
            string result = DBPath + @"\" + Path.Replace(":", "") + ".jpg";
            try
            {
                if (new FileInfo(result).Exists)
                    return result;
                else
                {
                    if (checkExist)
                        return null;
                    else
                        return result;
                }
            }
            catch { return null; }
        }

        public string GetMiniSourcePath(string thumbPath, bool checkExist)
        {
            return ReversePath(_MiniDBPath, thumbPath, checkExist);
        }

        public string GetSourcePath(string thumbPath, bool checkExist)
        {
            return ReversePath(_ThumbDBPath, thumbPath, checkExist);
        }

        /// <summary>
        /// returns source path from thumbPath
        /// returns null if thumbPath is invalid
        /// returns null if thumbPath does not start with DB path
        /// returns null if sourcePath is invalid
        /// returns null if sourcePath does not exist
        /// </summary>
        private string ReversePath(string DBPath, string thumbPath, bool checkExist)
        {
            FileInfo tempFI;
            try { tempFI = new FileInfo(thumbPath); }
            catch { return null; }
            if (!tempFI.FullName.ToUpper().StartsWith(DBPath.ToUpper()))
                return null;

            string sourcePath = tempFI.FullName.ToUpper().Replace(DBPath.ToUpper(), "");
            if (sourcePath.StartsWith(@"\"))
                sourcePath = sourcePath.Substring(1);
            sourcePath = sourcePath[0] + ":" + sourcePath.Substring(1);
            if (sourcePath.EndsWith(".JPG"))
            {
                sourcePath = sourcePath.Substring(0, sourcePath.Length - 4);
                try
                {
                    if (new FileInfo(sourcePath).Exists)
                        return sourcePath;
                    else
                    {
                        if (checkExist)
                            return null;
                        else
                            return sourcePath;
                    }
                }
                catch { return null; }
            }
            else
            {
                try
                {
                    if (new DirectoryInfo(sourcePath).Exists)
                        return sourcePath;
                    else
                    {
                        if (checkExist)
                            return null;
                        else
                            return sourcePath;
                    }
                }
                catch { return null; }
            }
        }

    }
}
