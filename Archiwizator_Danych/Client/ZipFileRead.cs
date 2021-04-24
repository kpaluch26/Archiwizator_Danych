using Ionic.Zip;
using System.Collections.Generic;

namespace Client
{
    class ZipFileRead
    {
        private static List<FileInformation> zip_files;
        private static string password;
        private static long size;

        public static List<FileInformation> ReadFileFromZip(string _zippath)
        {
            string[] fileinfo;
            string[] folderinfo;
            bool check = true;

            if (zip_files != null)
            {
                zip_files.Clear();
            }
            
            zip_files = new List<FileInformation>();           

            using (ZipFile zip = ZipFile.Read(_zippath))
            {
                foreach (ZipEntry e in zip)
                {
                    FileInformation fn = new FileInformation();

                    if (!e.IsDirectory)
                    {
                        folderinfo = e.FileName.Split('/');
                        int x = folderinfo.Length - 1;

                        fileinfo = folderinfo[x].Split('.');

                        fn.filename = fileinfo[0];
                        fn.filepath = _zippath;
                        fn.filesize = e.UncompressedSize;

                        if (fileinfo.Length == 2)
                        {
                            fn.filetype = fileinfo[1];
                        }
                        else
                        {
                            fn.filetype = "Plik";
                        }
                        fn.is_checked = true;

                        if (check)
                        {
                            if (e.Encryption.ToString() != "None")
                            {
                                password = "Tak";
                            }
                            else
                            {
                                password = "Nie";
                            }
                            check = false;
                        }

                        size += e.UncompressedSize;
                        zip_files.Add(fn);
                    }
                }
            }

            return zip_files;
        }

        public static string GetZipPassword()
        {
            return password;
        }

        public static string GetCount()
        {
            return zip_files.Count.ToString();
        }

        public static string GetSize()
        {
            return FileToSend.FormatSize(size);
        }
    }   
}
