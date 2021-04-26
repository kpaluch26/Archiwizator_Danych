using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Server
{
    class ZipFileCreate
    {
        private static string path;
        private static string zip_error;

        public static bool CreateZip(ObservableCollection<FileInformation> _files, string _filename, string _password, string _path)
        {            
            List<FileInformation> selected_files = new List<FileInformation>();

            if (_filename.Trim() == "")
            {
                zip_error = "UWAGA! Nie utworzono archiwum. Podaj nazwę archiwum, które chcesz utworzyć.";
                return false;
            }
            else 
            {
                if (_path.Trim() != "")
                {
                    path = _path + "\\" + _filename + ".zip";
                }
                else
                {
                    Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
                    fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

                    if (fbd.ShowDialog() == true)
                    {
                        path = fbd.SelectedPath + "\\" + _filename + ".zip";
                    }
                    else
                    {
                        zip_error = "UWAGA! Nie wybrano miejsca zapisu nowego archiwum.";
                        return false;
                    }
                }
            }

            try 
            { 
                if (_files.Count >= 1)
                {
                    for (int i = 0; i < _files.Count; i++)
                    {
                        if (_files[i].is_checked == true)
                        {
                            selected_files.Add(_files[i]);
                        }
                    }

                    if (selected_files.Count >= 1)
                    {
                        using (ZipFile _zip = new ZipFile()) //utworzenie archiwum
                        {
                            foreach (var _file in selected_files)
                            {
                                if (_password.Trim() != "")
                                {
                                    _zip.Password = _password; //dodanie hasła
                                }
                                _zip.AddFile((_file.filepath + "\\" + _file.filename + _file.filetype), ""); //dodanie pliku do archiwum
                            }
                            _zip.Save(path); //zapis archiwum
                            return true;                            
                        }
                    }
                    else
                    {
                        zip_error = "UWAGA! Na liście brak zaznaczonych plików do skompresowania.";
                        return false;
                    }
                }
                else
                {
                    zip_error = "UWAGA! Na liście brak plików do skompresowania.";
                    return false;
                }                
            }
            catch (FileNotFoundException)
            {
                zip_error = "UWAGA! Błąd tworzenia. Przynajmniej jeden wybrany plik zmienił ścieżke dostępu.";
                return false;
            }
            catch (ArgumentException)
            {
                zip_error = "UWAGA! Błąd tworzenia. Nazwa archiwum zawiera niedozwolone znaki.";
                return false;
            }
        }

        public static string GetPath()
        {
            return path;
        }

        public static string GetError()
        {
            return zip_error;
        }
    }
}
