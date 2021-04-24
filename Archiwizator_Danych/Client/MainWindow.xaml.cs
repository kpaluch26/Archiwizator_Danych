﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ionic.Zip;
using System.ComponentModel;
using System.Net.Sockets;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //zmienne globalne
        private UserConfiguration user_configuration;
        private static Thread resources_thread;
        private CancellationTokenSource cts;
        private ObservableCollection<FileInformation> files_list = new ObservableCollection<FileInformation>();
        private long total_ram;
        //konstruktor
        public MainWindow()
        {
            InitializeComponent();
            SetUp();
        }

        //funkcje
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        private void SetUp()
        {
            user_configuration = new UserConfiguration();
            user_configuration.state = false;

            GetPhysicallyInstalledSystemMemory(out total_ram);
            total_ram = total_ram / 1024;

            files_list.Clear();
            dgr_ArchivePanelFiles.ItemsSource = files_list;
        }   
        
        private void CleanClient()
        {
            grd_ArchivePanelCreate.Visibility = Visibility.Collapsed;
            grd_Configuration.Visibility = Visibility.Collapsed;
            grd_ResourcesMonitor.Visibility = Visibility.Collapsed;
            grd_ArchivePanelRead.Visibility = Visibility.Collapsed;

            if (resources_thread != null && resources_thread.IsAlive && cts.Token.CanBeCanceled)
            {
                cts.Cancel();
                cts.Token.WaitHandle.WaitOne();
                cts.Dispose();
            }
        }

        private void grd_MenuToolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            CleanClient();
            Application.Current.Shutdown();
        }

        private void btn_MenuClose_Click(object sender, RoutedEventArgs e)
        {
            btn_MenuOpen.Visibility = Visibility.Visible;
            btn_MenuClose.Visibility = Visibility.Collapsed;
        }

        private void btn_MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            btn_MenuOpen.Visibility = Visibility.Collapsed;
            btn_MenuClose.Visibility = Visibility.Visible;
        }

        private void btn_ConfigurationSave_Click(object sender, RoutedEventArgs e)
        {
            string firstname, lastname, group, section, version;

            try
            {
                firstname = txt_ConfigurationName.Text.Trim();
                if (firstname != "")
                {
                    user_configuration.firstname = firstname;
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano imienia użytkownika.";
                    throw new Exception();
                }

                lastname = txt_ConfigurationSurname.Text.Trim();
                if (lastname != "")
                {
                    user_configuration.lastname = lastname;
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano nazwiska użytkownika.";
                    throw new Exception();
                }

                group = txt_ConfigurationGroup.Text.Trim();
                if (group != "")
                {
                    user_configuration.group = group;
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano grupy studenckiej użytkownika.";
                    throw new Exception();
                }

                if (cbx_ConfigurationSection.IsChecked == true)
                {
                    section = txt_ConfigurationSection.Text.Trim();
                    if (section != "")
                    {
                        user_configuration.section = section;
                    }
                    else
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Nie podano sekcji użytkownika.";
                        throw new Exception();
                    }
                }
                else
                {
                    user_configuration.section = null;
                }

                if (cbx_ConfigurationVersion.IsChecked == true)
                {
                    version = txt_ConfigurationVersion.Text.Trim();
                    if (version != "")
                    {
                        user_configuration.version = version;
                    }
                    else
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Nie podano wersji pracy użytkownika.";
                        throw new Exception();
                    }
                }
                else
                {
                    user_configuration.version = null;
                }

                user_configuration.state = true;
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
            }
            catch
            {
                user_configuration.state = false;
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
            }
        }

        private void btn_ConfigurationConnect_Click(object sender, RoutedEventArgs e)
        {
            bool result = ConnectionStart.TryConnect(user_configuration, this);

            if (result)
            {
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;

                btn_ConfigurationSave.IsEnabled = false;

                btn_ConfigurationConnect.Visibility = Visibility.Collapsed;
                btn_ConfigurationConnect.IsEnabled = false;

                btn_ConfigurationDisconnect.IsEnabled = true;
                btn_ConfigurationDisconnect.Visibility = Visibility.Visible;
            }
            else
            {
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
            }
        }

        private void btn_ConfigurationSetSavePath_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
            fbd.Description = "Wybierz ścieżkę dostępu."; //tytuł utworzonego okna
            fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

            if (fbd.ShowDialog() == true) //jeśli wybrano ścieżkę
            {
                user_configuration.folderpath = fbd.SelectedPath;
                tbl_ConfigurationSavePath.Text = fbd.SelectedPath;
                tbl_ConfigurationAllert.Text = "";
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
            }
            else
            {
                if (user_configuration.folderpath != null)
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie wybrano nowego miejsca zapisu. Powrót do poprzedniego zapisu.";
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie wybrano nowego miejsca zapisu.";
                }
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
            }
        }

        private void btn_ConfigurationDisconnect_Click(object sender, RoutedEventArgs e)
        {
            ConnectionEnd.TryDisconnect(this);
        }

        private void btn_ArchivePanelCreateOptions_Click(object sender, RoutedEventArgs e)
        {
            Storyboard s;
            if (btn_ArchivePanelCreateZIP.IsEnabled == false)
            {
                s = (Storyboard)this.FindResource("ArchivePanelOptionsShow");
                s.Begin();
                btn_ArchivePanelCreateZIP.IsEnabled = true;
            }
            else
            {
                s = (Storyboard)this.FindResource("ArchivePanelOptionsHide");
                s.Begin();
                btn_ArchivePanelCreateZIP.IsEnabled = false;
            }
        }

        private void btn_ArchivePanelCreateZIP_Click(object sender, RoutedEventArgs e)
        {
            bool result = false;
            string path = "";

            if (cbx_ArchivePanelSavePathFromFile.IsChecked == true)
            {
                if (tbl_ConfigurationSavePath.Text != "")
                {
                    path = tbl_ConfigurationSavePath.Text;
                }
                else
                {
                    tbl_ArchivePanelAllert.Text = "UWAGA! Domyślne miejsce zapisu nieustawione.";
                    tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                    pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
                }
            }

            result = ZipFileCreate.CreateZip(files_list, tbx_ArchivePanelFilename.Text, pbx_ArchivePanelPasswordBox.Password, path);

            if (result)
            {
                if (cbx_ArchivePanelSetToSend.IsChecked == true)
                {
                    //file_to_send = FileToSend.FileToSendSet(ZipFileCreate.GetPath());

                    //TransferThread.SetUp(file_to_send);

                    //tbl_ControlPanelFileName.Text = file_to_send.filename;
                    //tbl_ControlPanelFileLocation.Text = file_to_send.filepath;
                    //tbl_ControlPanelFileSize.Text = FileToSend.FormatSize(file_to_send.filesize);
                }
                tbl_ArchivePanelAllert.Text = "";
                tbl_ArchivePanelAllert.Visibility = Visibility.Collapsed;
                pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check;
            }
            else
            {
                tbl_ArchivePanelAllert.Text = ZipFileCreate.GetError();
                tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
            }
        }

        private void btn_ConfigurationReturn_Click(object sender, RoutedEventArgs e)
        {
            tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
        }

        private void pic_ArchivePanelPasswordBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            tbx_ArchivePanelPasswordUnmasked.Text = pbx_ArchivePanelPasswordBox.Password;
            pbx_ArchivePanelPasswordBox.Visibility = Visibility.Collapsed;
            tbx_ArchivePanelPasswordUnmasked.Visibility = Visibility.Visible;
        }

        private void pic_ArchivePanelPasswordBox_MouseLeave(object sender, MouseEventArgs e)
        {
            tbx_ArchivePanelPasswordUnmasked.Visibility = Visibility.Collapsed;
            pbx_ArchivePanelPasswordBox.Visibility = Visibility.Visible;
            tbx_ArchivePanelPasswordUnmasked.Text = "";
        }

        private void pic_ArchivePanelPasswordBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            tbx_ArchivePanelPasswordUnmasked.Visibility = Visibility.Collapsed;
            pbx_ArchivePanelPasswordBox.Visibility = Visibility.Visible;
            tbx_ArchivePanelPasswordUnmasked.Text = "";
        }

        private void cbx_ArchivePanelSelectAllFiles_Click(object sender, RoutedEventArgs e)
        {
            if (cbx_ArchivePanelSelectAllFiles.IsChecked == true)
            {
                for (int i = 0; i < files_list.Count; i++)
                {
                    files_list[i].is_checked = true;
                }
            }
            else
            {
                for (int i = 0; i < files_list.Count; i++)
                {
                    files_list[i].is_checked = false;
                }
            }

            dgr_ArchivePanelFiles.Items.Refresh();
        }

        private void dgr_ArchivePanelFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void dgr_ArchivePanelFiles_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length >= 1)
            {
                List<string> _files_list = new List<string>(files);

                foreach (string _file in _files_list)
                {
                    FileInfo fi = new FileInfo(_file);
                    if (fi.Exists)
                    {
                        string _filename = Path.GetFileNameWithoutExtension(fi.Name);
                        string _filepath = fi.DirectoryName;
                        string _filetype = fi.Extension;
                        long _filesize = fi.Length;
                        FileInformation file = new FileInformation()
                        {
                            filename = _filename,
                            filepath = _filepath,
                            filetype = _filetype,
                            filesize = _filesize,
                            is_checked = false
                        };
                        files_list.Add(file);
                    }
                }
                _files_list.Clear();
                btn_ArchivePanelDataGridClear.IsEnabled = true;
            }
        }

        private void cbx_ArchviePanelFileSelectedChangeValue(object sender, RoutedEventArgs e) //event do od/zaznaczania pojedynczego pliku z listy 
        {
            if (cbx_ArchivePanelSelectAllFiles.IsChecked == true)
            {
                for (int i = 0; i < files_list.Count; i++)
                {
                    if (files_list[i].is_checked == false)
                    {
                        cbx_ArchivePanelSelectAllFiles.IsChecked = false;
                        break;
                    }
                }
            }
            else
            {
                int selected_counter = 0;
                for (int i = 0; i < files_list.Count; i++)
                {
                    if (files_list[i].is_checked == true)
                    {
                        selected_counter++;
                    }
                }

                if (selected_counter == files_list.Count)
                {
                    cbx_ArchivePanelSelectAllFiles.IsChecked = true;
                }
            }
        }

        private void btn_ArchivePanelDataGridClear_Click(object sender, RoutedEventArgs e)
        {
            if (cbx_ArchivePanelSelectAllFiles.IsChecked == true)
            {
                files_list.Clear();
                btn_ArchivePanelDataGridClear.IsEnabled = false;
                cbx_ArchivePanelSelectAllFiles.IsChecked = false;
            }
            else
            {
                for (int i = 0; i < files_list.Count; i++)
                {
                    if (files_list[i].is_checked == true)
                    {
                        files_list.Remove(files_list[i]);
                        i -= 1;
                    }
                }
            }
        }

        private void btn_ArchivePanelDataGridFileAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog(); //utworzenie okna do przeglądania plików
            ofd.Filter = "all files (*.*)|*.*"; //ustawienie filtrów okna na dowolne pliki
            ofd.FilterIndex = 1; //ustawienie domyślnego filtru
            ofd.RestoreDirectory = true; //przywracanie wcześniej zamkniętego katalogu
            ofd.Multiselect = true; //ustawienie możliwości wyboru wielu plików z poziomu okna

            if (ofd.ShowDialog() == true)
            {
                FileInfo[] _files = ofd.FileNames.Select(_file => new FileInfo(_file)).ToArray();

                foreach (var _file in _files)
                {
                    string _filename = Path.GetFileNameWithoutExtension(_file.Name);
                    string _filepath = _file.DirectoryName;
                    string _filetype = _file.Extension;
                    long _filesize = _file.Length;
                    FileInformation file = new FileInformation()
                    {
                        filename = _filename,
                        filepath = _filepath,
                        filetype = _filetype,
                        filesize = _filesize,
                        is_checked = false
                    };
                    files_list.Add(file);
                }
                if (btn_ArchivePanelDataGridClear.IsEnabled == false)
                {
                    btn_ArchivePanelDataGridClear.IsEnabled = true;
                }
            }
        }

        private void btn_ArchivePanelCreate_Click(object sender, RoutedEventArgs e)
        {
            CleanClient();
            grd_ArchivePanelCreate.Visibility = Visibility.Visible;
            tbl_ArchivePanelAllert.Visibility = Visibility.Hidden;
        }

        private void btn_ConfigurationPanel_Click(object sender, RoutedEventArgs e)
        {
            CleanClient();
            grd_Configuration.Visibility = Visibility.Visible;
            tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
        }

        private void btn_ResourcesMonitor_Click(object sender, RoutedEventArgs e)
        {
            CleanClient();
            grd_ResourcesMonitor.Visibility = Visibility.Visible;
            cts = new CancellationTokenSource();
            resources_thread = new Thread(() => ResourcesMonitor.ResourcesMonitorWork(cts.Token, total_ram, this));
            resources_thread.Start();
        }

        private void btn_ArchivePanelReadSetZIP_Click(object sender, RoutedEventArgs e)
        {            
            OpenFileDialog ofd = new OpenFileDialog(); //utworzenie okna do przeglądania plików
            ofd.Filter = "zip file(*.zip)|*.zip"; //ustawienie filtrów okna na dowolne pliki
            ofd.FilterIndex = 1; //ustawienie domyślnego filtru
            ofd.RestoreDirectory = true; //przywracanie wcześniej zamkniętego katalogu
            ofd.Multiselect = false; //ustawienie możliwości wyboru wielu plików z poziomu okna 

            if (ofd.ShowDialog() == true)
            {
                FileInfo fn = new FileInfo(ofd.FileName);

                tbl_ArchivePanelReadFileName.Text = ofd.SafeFileName;
                tbl_ArchivePanelReadFileLocation.Text = ofd.FileName;
                tbl_ArchivePanelReadFileSize.Text = FileToSend.FormatSize(fn.Length);

                dgr_ArchivePanelReadFiles.ItemsSource = ZipFileRead.ReadFileFromZip(ofd.FileName);

                tbl_ArchivePanelReadPassword.Text = ZipFileRead.GetZipPassword();
                tbl_ArchivePanelReadQuantity.Text = ZipFileRead.GetCount();
                tbl_ArchivePanelReadTotalSize.Text = ZipFileRead.GetSize();
            }
            else
            {
                tbl_ArchivePanelReadAllert.Text = "UWAGA! Nie wybrano nowego archiwum.";
                tbl_ArchivePanelReadAllert.Visibility = Visibility.Visible;
            }
        }

        private void btn_ArchivePanelReadExportZip_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_ArchivePanelRead_Click(object sender, RoutedEventArgs e)
        {
            CleanClient();
            grd_ArchivePanelRead.Visibility = Visibility.Visible;
        }
    }
}
