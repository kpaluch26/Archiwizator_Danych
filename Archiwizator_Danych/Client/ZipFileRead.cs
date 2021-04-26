using Ionic.Zip;
using System.Collections.Generic;
using System.IO;

namespace Client
{
    class ZipFileRead
    {
        private static List<FileInformation> zip_files;
        private static string password;
        private static long size;
        private static string zip_error;

        public static List<FileInformation> ReadFileFromZip(string _zippath)
        {
            string[] fileinfo;
            string[] folderinfo;
            bool check = true;

            size = 0;

            if (zip_files != null)
            {
                zip_files.Clear();
            }
            
            zip_files = new List<FileInformation>();

            try
            {
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
            }
            catch (Ionic.Zip.ZipException)
            {
                zip_error = "UWAGA! Wybrane archiwum jest uszkodzone.";
            }

            return zip_files;
        }

        public static bool ExportZip(string _zippath, string _password)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
            fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

            if (fbd.ShowDialog() == true)
            {
                if (File.Exists(_zippath))
                {
                    ZipFile zip = new ZipFile(_zippath);

                    if (password == "Tak")
                    {
                        zip.Password = _password;
                    }
                    try
                    {
                        zip.ExtractAll(fbd.SelectedPath, ExtractExistingFileAction.OverwriteSilently);
                        return true;
                    }
                    catch (Ionic.Zip.BadPasswordException)
                    {
                        zip_error = "UWAGA! Podano nieprawidłowe hasło do wybranego archiwum.";
                        return false;
                    }
                    catch (Ionic.Zip.ZipException)
                    {
                        zip_error = "UWAGA! Wybrane archiwum jest uszkodzone.";
                        return false;
                    }
                }
                else
                {
                    zip_error = "Uwaga! Wybrany plik zmienił ścieżkę zapisu.";
                    return false;
                }               
            }
            else
            {
                zip_error = "Uwaga! Nie wybrano miejsca do wypakowania wybranego archiwum.";
                return false;
            }
        }

        public static string GetError()
        {
            return zip_error;
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
