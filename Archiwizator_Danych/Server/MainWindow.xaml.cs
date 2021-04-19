using System;
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

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //zmienne globalne
        private ServerConfiguration config;
        private static Thread resources_thread;
        private long total_ram;
        ObservableCollection<FileInformation> files_list = new ObservableCollection<FileInformation>();               
        CancellationTokenSource cts;
        private FileInformation file_to_send;                                

        //konstruktor 
        public MainWindow()
        {
            InitializeComponent();
            SetUp();
            TransferThread.SetLists(this);
        }

        //funkcje
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        private void SetUp()
        {
            GetPhysicallyInstalledSystemMemory(out total_ram);
            total_ram = total_ram / 1024;
            files_list.Clear();
            dgr_ArchivePanelFiles.ItemsSource = files_list;
            tbl_ControlPanelUsercCounters.Text = "0 / 20";
        }

        private void CleanServer() //funkcja do czyszczenia pozostałości po wcześniej otwartym oknie
        {
            grd_ResourcesMonitor.Visibility = Visibility.Collapsed;
            grd_Configuration.Visibility = Visibility.Collapsed;
            grd_ArchivePanel.Visibility = Visibility.Collapsed;
            grd_ControlPanel.Visibility = Visibility.Collapsed;
            grd_UsersPanel.Visibility = Visibility.Collapsed;

            if (resources_thread != null && resources_thread.IsAlive && cts.Token.CanBeCanceled)
            {
                cts.Cancel();
                cts.Token.WaitHandle.WaitOne();
                cts.Dispose();
            }
        }

        private void ServerConfigurationUpdate()
        {
            tbl_ControlPanelUserName.Text = config.GetUserName();
            tbl_ControlPanelIP.Text = config.GetIPAddress();
            tbl_ControlPanelHostName.Text = config.GetHostName();
            tbl_ControlPanelSavePath.Text = config.GetArchiveAddress();
            tbl_ControlPanelBufferSize.Text = config.GetBufferSize().ToString();
            tbl_ControlPanelPort.Text = config.GetPort().ToString();
        }        

        //wydarzenia
        private void btn_CloseWindow_Click(object sender, RoutedEventArgs e) //zamknięcie okna aplikacji
        {
            CleanServer();
            Application.Current.Shutdown();
        }

        private void btn_MinimizeWindow_Click(object sender, RoutedEventArgs e) //zminimalizowanie okna
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_MenuOpen_Click(object sender, RoutedEventArgs e) //rozwijanie menu głównego
        {
            btn_MenuOpen.Visibility = Visibility.Collapsed;
            btn_MenuClose.Visibility = Visibility.Visible;
        }

        private void btn_MenuClose_Click(object sender, RoutedEventArgs e) //zwijanie menu głównego
        {
            btn_MenuOpen.Visibility = Visibility.Visible;
            btn_MenuClose.Visibility = Visibility.Collapsed;
        }

        private void grd_MenuToolbar_MouseDown(object sender, MouseButtonEventArgs e) //zmiana lokalizacji okna na ekranie
        {
            this.DragMove();
        }

        private void btn_ConfigurationPanel_Click(object sender, RoutedEventArgs e) //otwieranie panelu do konfiguracji
        {
            CleanServer();
            grd_Configuration.Visibility = Visibility.Visible;
            tbl_ConfigurationAllert.Visibility = Visibility.Collapsed;
        }

        private void btn_ControlPanel_Click(object sender, RoutedEventArgs e) //otwieranie głównego panelu sterowania
        {
            CleanServer();
            grd_ControlPanel.Visibility = Visibility.Visible;
            tbl_ControlPanelAllert.Visibility = Visibility.Collapsed;
        }

        private void btn_ArchivePanel_Click(object sender, RoutedEventArgs e) //otwieranie panelu do zarządzania zip
        {
            CleanServer();
            grd_ArchivePanel.Visibility = Visibility.Visible;
            tbl_ArchivePanelAllert.Visibility = Visibility.Collapsed;
        }

        private void btn_HistoryPanel_Click(object sender, RoutedEventArgs e) //otwieranie historii pracy serwera i aktywności klientów
        {
            CleanServer();
            grd_UsersPanel.Visibility = Visibility.Visible;
            tbl_UsersPanelAllert.Visibility = Visibility.Collapsed;
        }

        private void btn_ResourcesMonitor_Click(object sender, RoutedEventArgs e) //śledzenie wydajności zasobów komputera
        {
            CleanServer();
            grd_ResourcesMonitor.Visibility = Visibility.Visible;
            cts = new CancellationTokenSource();            
            resources_thread = new Thread(() => ResourcesMonitor.ResourcesMonitorWork(cts.Token, total_ram, this));
            resources_thread.Start();
        }

        private void btn_ConfigurationLoad_Click(object sender, RoutedEventArgs e) //funkcja do załadowania pliku konfiguracyjnego
        {
            config = ServerConfigurationLoad.LoadFromFile();

            if (config != null)
            {
                pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check; //zmiana ikony na powodzenie operacji
                ServerConfigurationUpdate();
                btn_ConfigurationCreate.Content = "Edytuj konfigurację";
                flp_ConfigurationSave.IsEnabled = true; //można eksportować do pliku
                TransferThread.SetUp(config, this);
            }
            else
            {
                tbl_ConfigurationAllert.Text = ServerConfigurationLoad.GetError();
                tbl_ConfigurationAllert.FontSize = 18;
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
            }
        }

        private void btn_ConfigurationSave_Click(object sender, RoutedEventArgs e) //funkcja do zapisu konfiguracji do pliku
        {
            bool result = ServerConfigurationSave.SaveToFile(config.GetPort(), config.GetBufferSize(), config.GetArchiveAddress());

            if (result)
            {
                pic_ConfigurationSave.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check; //zmiana ikony na powodzenie operacji
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
            }
            else
            {
                tbl_ConfigurationAllert.Text = "UWAGA! Nie zapisano pliku, użytkownik nie wskazał miejsca zapisu.";
                tbl_ConfigurationAllert.FontSize = 18;
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                pic_ConfigurationSave.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
            }
        }

        private void btn_ConfigurationCreateSave_Click(object sender, RoutedEventArgs e) //funkcja do ręcznego tworzenia konfiguracji
        {
            bool iscreated = false;
            ServerConfiguration new_config = null;
            
            if (config != null)
            {
                iscreated = true;
            }

            new_config = ServerConfigurationCreate.CreateConfiguration(txt_ConfigurationPort.Text, tbl_ControlPanelBufferSize.Text, iscreated);

            if (new_config != null)
            {
                config = new_config;
                btn_ConfigurationCreate.Content = "Edytuj konfigurację";
                flp_ConfigurationSave.IsEnabled = true;
                tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
                ServerConfigurationUpdate();
                TransferThread.SetUp(config, this);
            }
            else
            {
                if (iscreated)
                {
                    tbl_ConfigurationAllert.Text = ServerConfigurationCreate.GetError();
                    tbl_ConfigurationAllert.FontSize = 12;
                    tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                    txt_ConfigurationPort.Text = config.GetPort().ToString();
                    cmb_ConfigurationBuffer.Text = config.GetBufferSize().ToString();
                }
                else
                {
                    tbl_ConfigurationAllert.Text = ServerConfigurationCreate.GetError();
                    tbl_ConfigurationAllert.FontSize = 16;
                    tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                    btn_ConfigurationCreateSave.Command.Execute(null);
                }
            }
        }

        private void btn_ConfigurationReturn_Click(object sender, RoutedEventArgs e) //event do kasowania komunikatów
        {
            tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
        }

        private void dgr_ArchivePanelFiles_DragEnter(object sender, DragEventArgs e) //event do sprawdzania czy przyciągany jest plik
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

        private void dgr_ArchivePanelFiles_Drop(object sender, DragEventArgs e) //event do dodawania lików do listy
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

        private void cbx_ArchivePanelSelectAllFiles_Click(object sender, RoutedEventArgs e) //event checkboxa do zaznaczania/odznaczania wszystkich plików na liście
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

        private void btn_ArchivePanelCreateZIP_Click(object sender, RoutedEventArgs e) //event do tworzenia archiwum zip
        {
            bool result = false;
            string path = "";

            if (cbx_ArchivePanelSavePathFromFile.IsChecked == true)
            {
                if (config != null)
                {
                    path = config.GetArchiveAddress();
                }
                else
                {
                    tbl_ArchivePanelAllert.Text = "UWAGA! Domyślne miejsce zapisu nieustawione. Załaduj plik konfiguracyjny lub ręcznie wybierz miejsce zapisu";
                    tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                    pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
                }
            }

            result = ZipFileCreate.CreateZip(files_list, tbx_ArchivePanelFilename.Text, pbx_ArchivePanelPasswordBox.Password, path);


            if (result)
            {
                if (cbx_ArchivePanelSetToSend.IsChecked == true)
                {
                    file_to_send = FileToSend.FileToSendSet(ZipFileCreate.GetPath());

                    TransferThread.SetUp(file_to_send);

                    tbl_ControlPanelFileName.Text = file_to_send.filename;
                    tbl_ControlPanelFileLocation.Text = file_to_send.filepath;
                    tbl_ControlPanelFileSize.Text = FileToSend.FormatSize(file_to_send.filesize);
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

        private void btn_ArchivePanelDataGridClear_Click(object sender, RoutedEventArgs e) //event do czyszczenia plikow
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
                    }
                }              
            }
        }

        private void btn_ArchivePanelDataGridFileAdd_Click(object sender, RoutedEventArgs e) //event do dodawania plików do listy
        {
            OpenFileDialog ofd = new OpenFileDialog(); //utworzenie okna do przeglądania plików
            ofd.Filter = "all files (*.*)|*.*"; //ustawienie filtrów okna na dowolne pliki
            ofd.FilterIndex = 1; //ustawienie domyślnego filtru
            ofd.RestoreDirectory = true; //przywracanie wcześniej zamkniętego katalogu
            ofd.Multiselect = true; //ustawienie możliwości wyboru wielu plików z poziomu okna

            if (ofd.ShowDialog() == true)
            {
                FileInfo[] _files = ofd.FileNames.Select(_file => new FileInfo(_file)).ToArray();

                foreach ( var _file in _files)
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

        private void pic_ArchivePanelPasswordBox_MouseDown(object sender, MouseButtonEventArgs e) //event do pokazywania hasła
        {
            tbx_ArchivePanelPasswordUnmasked.Text = pbx_ArchivePanelPasswordBox.Password;
            pbx_ArchivePanelPasswordBox.Visibility = Visibility.Collapsed;
            tbx_ArchivePanelPasswordUnmasked.Visibility = Visibility.Visible;
        }

        private void pic_ArchivePanelPasswordBox_MouseUp(object sender, MouseButtonEventArgs e) //event do ukrywania hasła
        {
            tbx_ArchivePanelPasswordUnmasked.Visibility = Visibility.Collapsed;
            pbx_ArchivePanelPasswordBox.Visibility = Visibility.Visible;           
            tbx_ArchivePanelPasswordUnmasked.Text = "";
        }

        private void pic_ArchivePanelPasswordBox_MouseLeave(object sender, MouseEventArgs e) //event do ukrywania hasła
        {
            tbx_ArchivePanelPasswordUnmasked.Visibility = Visibility.Collapsed;
            pbx_ArchivePanelPasswordBox.Visibility = Visibility.Visible;
            tbx_ArchivePanelPasswordUnmasked.Text = "";
        }

        private void btn_ArchivePanelCreateOptions_Click(object sender, RoutedEventArgs e) //event do animacji karty z opcjami dla archiwum zip
        {
            Storyboard s;
            if (btn_ArchivePanelCreateZIP.IsEnabled==false)
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

        private void btn_ControlPanelChangeZIP_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog(); //utworzenie okna do przeglądania plików
            ofd.Filter = "zip file(*.zip)|*.zip"; //ustawienie filtrów okna na dowolne pliki
            ofd.FilterIndex = 1; //ustawienie domyślnego filtru
            ofd.RestoreDirectory = true; //przywracanie wcześniej zamkniętego katalogu
            ofd.Multiselect = false; //ustawienie możliwości wyboru wielu plików z poziomu okna 

            if (ofd.ShowDialog() == true)
            {
                file_to_send = FileToSend.FileToSendSet(ofd.FileName);

                TransferThread.SetUp(file_to_send);

                tbl_ControlPanelFileName.Text = file_to_send.filename;
                tbl_ControlPanelFileLocation.Text = file_to_send.filepath;
                tbl_ControlPanelFileSize.Text = FileToSend.FormatSize(file_to_send.filesize);
            }
            else
            {
                tbl_ControlPanelAllert.Text = "UWAGA! Nie wybrano nowego archiwum. Powrót do poprzedniego pliku.";
                tbl_ControlPanelAllert.Visibility = Visibility.Visible;
            }

        }

        private void btn_ControlPanelChangeConfigPath_Click(object sender, RoutedEventArgs e)
        {
            if (config != null)
            {
                Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
                fbd.Description = "Wybierz ścieżkę dostępu."; //tytuł utworzonego okna
                fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

                if (fbd.ShowDialog() == true) //jeśli wybrano ścieżkę
                {
                    config.SetArchiveAddress(fbd.SelectedPath);
                    tbl_ControlPanelSavePath.Text = fbd.SelectedPath;
                    tbl_ControlPanelAllert.Text = "";
                    tbl_ControlPanelAllert.Visibility = Visibility.Collapsed;
                }
                else
                {
                    tbl_ControlPanelAllert.Text = "UWAGA! Nie wybrano nowego miejsca zapisu. Powrót do poprzedniego zapisu.";
                    tbl_ControlPanelAllert.Visibility = Visibility.Visible;
                }
            }
        }

        private void btn_ControlPanelChangeIP_Click(object sender, RoutedEventArgs e)
        {
            if (config != null) {
                if (txt_ControlPanelChangeIP.Visibility == Visibility.Collapsed)
                {
                    txt_ControlPanelChangeIP.Visibility = Visibility.Visible;
                    tbl_ControlPanelIP.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (ValidationIP.ValidateIPAddress(txt_ControlPanelChangeIP.Text) == true)
                    {
                        config.SetIPAddress(txt_ControlPanelChangeIP.Text);
                        tbl_ControlPanelIP.Text = txt_ControlPanelChangeIP.Text;
                        tbl_ControlPanelAllert.Text = "";
                        tbl_ControlPanelAllert.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        tbl_ControlPanelAllert.Text = "UWAGA! Podany adres IP jest nieprawidłowy. Powrót do poprzedniej konfiguracji.";
                        tbl_ControlPanelAllert.Visibility = Visibility.Visible;
                    }
                    txt_ControlPanelChangeIP.Visibility = Visibility.Collapsed;
                    tbl_ControlPanelIP.Visibility = Visibility.Visible;
                }
            }
        }       

        private void btn_ControlPanelServerListen_Click(object sender, RoutedEventArgs e)
        {            
            ServerOptions.server_option = ServerOptions.Options.server_listen;
            BackgroundThread.BackgroundWorkerClose();
            BackgroundThread.ConnectionListen(config);
            tbl_ControlPanelServer.Text = "Oczekiwanie na podłączenie klientów.";
        }

        private void btn_ControlPanelServerSend_Click(object sender, RoutedEventArgs e)
        {
            ServerOptions.server_option = ServerOptions.Options.server_send;
            BackgroundThread.BackgroundWorkerClose();
            tbl_ControlPanelServer.Text = "Wysyłanie plików podłączonym klientom.";
        }

        private void btn_ControlPanelServerStop_Click(object sender, RoutedEventArgs e)
        {
            ServerOptions.server_option = ServerOptions.Options.server_stop;
            BackgroundThread.BackgroundWorkerClose();
            tbl_ControlPanelServer.Text = "Wstrzymanie pracy.";
        }

        private void btn_ControlPanelServerReceive_Click(object sender, RoutedEventArgs e)
        {
            ServerOptions.server_option = ServerOptions.Options.server_receive;
            BackgroundThread.BackgroundWorkerClose();
            tbl_ControlPanelServer.Text = "Oczekiwanie na przesłanie plików.";
        }

    }    
}
