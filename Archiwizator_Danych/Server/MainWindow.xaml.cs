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
        private int active_clients = 0;        
        CancellationTokenSource cts;
        private static FileInformation file_to_send;
        private BackgroundWorker m_oBackgroundWorker = null;
        private enum ServerOptions { server_stop, server_listen, server_receive, server_send };
        private ServerOptions server_option = ServerOptions.server_stop;
        private ObservableCollection<ConnectionThread> client_list = new ObservableCollection<ConnectionThread>();
        private ObservableCollection<WorkHistory> history_list = new ObservableCollection<WorkHistory>();
        private object client_list_locker = new Object();

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
            GetPhysicallyInstalledSystemMemory(out total_ram);
            total_ram = total_ram / 1024;
            files_list.Clear();
            history_list.Clear();
            client_list.Clear();
            dgr_ArchivePanelFiles.ItemsSource = files_list;
            dgr_UsersPanelHistory.ItemsSource = history_list;
            dgr_UsersPanelConnectedUsers.ItemsSource = client_list;
            tbl_ControlPanelUsercCounters.Text = active_clients.ToString() + " / 20";
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
                flp_ConfigurationSave.IsEnabled = true; //można eksportować do pliku
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
                    if (ValidateIPAddress(txt_ControlPanelChangeIP.Text) == true)
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

        public bool ValidateIPAddress(string new_ip)
        {
            if (String.IsNullOrWhiteSpace(new_ip))
            {
                return false;
            }

            string[] splitValues = new_ip.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;
            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }
        
        private void ConnectionListen()
        {
            if (null == m_oBackgroundWorker) //sprawdzanie czy obiekt istnieje
            {
                m_oBackgroundWorker = new BackgroundWorker(); //utworzenie obiektu
                m_oBackgroundWorker.WorkerSupportsCancellation = true; //włączenie możliwości przerwania pracy wątka roboczego
                m_oBackgroundWorker.DoWork += new DoWorkEventHandler(m_oBackgroundWorker_DoWork); //utworzenie uchwyta dla obiektu
            }
            if (config != null)
            {
                m_oBackgroundWorker.RunWorkerAsync(config.GetPort()); //start wątka roboczego w tle
            }
        }

        private void m_oBackgroundWorker_DoWork(object sender, DoWorkEventArgs e) //funkcja odpowiadająca za pracę wątka roboczego w tle
        {
            TcpListener listener = new TcpListener(IPAddress.Any, config.GetPort()); //ustawienie nasłuchiwania na porcie z konfiguracji i dla dowolnego adresu IP
            TcpClient client = null; //utworzenie pustego klienta
            listener.Start(); //rozpoczęcie nasłuchiwania     
            bool do_work = true; //zmienna określające prace wątka w tle

            while (do_work)
            {
                if (server_option == ServerOptions.server_listen) //działa jesli tryb pracy serwera jest ustawiony na oczekiwanie
                {
                    if (listener.Pending()) //jeśli jakieś zapytanie przychodzi
                    {
                        client = listener.AcceptTcpClient(); //zaakceptowanie przychodzącego połączenia                           
                        ThreadPool.QueueUserWorkItem(TransferThread, client ); //Dodanie do kolejki klienta
                    }
                }
                if (m_oBackgroundWorker.CancellationPending) //jeśli przerwano prace wątka
                {
                    listener.Stop(); //stop nasłuchiwania
                    e.Cancel = true; //przerwanie obiektu
                    do_work = false; //koniec pracy
                    return;
                }
            }
        }

        private void TransferThread(object obj)
        {
            TcpClient client = (TcpClient)obj; //przejęcie kontroli nad klientem
            CancellationTokenSource canceltoken = new CancellationTokenSource();
            System.Text.Decoder decoder = System.Text.Encoding.UTF8.GetDecoder(); //zmienna pomocnicza do odkodowania nazwy pliku
            int buffer_size = config.GetBufferSize(); //wczytanie rozmiaru buffera z konfiguracji
            byte[] data = new byte[buffer_size]; //ustawienie rozmiaru bufera
            int receive_bytes; //zmienna do odbierania plików
            NetworkStream stream = null; //utworzenie kanału do odbioru
            string client_hostname = null; //zmienna do identyfikacji połączenia
            string client_ip = null;
            string client_filename = null; //zmienna do identyfikacji nazwy pliku
            bool help = true; //zmienna omocnicza określająca czy nowy klient się podłączył  
            WorkHistory worker = new WorkHistory();           

            while (!canceltoken.IsCancellationRequested)
            {
                try
                {
                    while (client.Connected)
                    {
                        if (server_option != ServerOptions.server_listen && help)
                        {
                            client.Close();
                        }
                        else if (server_option == ServerOptions.server_listen && help)
                        {
                            Monitor.Enter(client_list_locker);
                            Dispatcher.Invoke( delegate { updateCounterOfActiveUsers(true); } );
                            stream = client.GetStream(); //określenie rodzaju połączenia na odbiór danych
                            int dec_data = stream.Read(data, 0, data.Length);//oczekiwanie na nazwę klienta       
                            char[] chars = new char[dec_data]; //zmienna pomocnicza do odkodowania nazwy klienta
                            decoder.GetChars(data, 0, dec_data, chars, 0); //dekodowanie otrzymanej nazwy klienta
                            client_hostname = new System.String(chars); //przypisanie odkodowanej nazwy do nowej zmiennej
                            client_ip = client.Client.RemoteEndPoint.ToString();
                            worker.ClientConnectionStart(client_hostname);
                            Dispatcher.Invoke(delegate { history_list.Add(worker); });
                            help = false; //wyłączenie właściwości nowego klienta                        
                            ConnectionThread _ct = new ConnectionThread(client_hostname, client_ip, canceltoken);
                            Dispatcher.Invoke(delegate { client_list.Add(_ct); });
                            Monitor.Exit(client_list_locker);
                        }
                        else if (server_option == ServerOptions.server_receive && !help)
                        {
                            if (Directory.Exists(config.GetArchiveAddress()) == false) //sprawdzanie czy ścieżka dostępu z pliku konfiguracyjnego istnieje
                            {
                                Directory.CreateDirectory(config.GetArchiveAddress()); //utworzenie ścieżki dostępu z pliku konfiguracyjnego
                            }
                            try
                            {
                                byte[] buff = new byte[1]; //pomocniczy bufer
                                data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                bool end_stream = false; //flaga informująca czy koniec pliku
                                stream = client.GetStream(); //określenie rodzaju połączenia na odbiór danych
                                stream.ReadTimeout = 1000;
                                receive_bytes = stream.Read(data, 0, data.Length);
                                if ("startsending" == System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes))
                                {
                                    stream.ReadTimeout = Timeout.Infinite;
                                    data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                    data = System.Text.Encoding.ASCII.GetBytes("ready");
                                    stream.Write(data, 0, data.Length);
                                    stream.Write(data, 0, data.Length);
                                    stream.Flush(); //zwolnienie strumienia
                                    data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                    receive_bytes = stream.Read(data, 0, data.Length);
                                    client_filename = System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes);
                                    if (client_filename != null && client_filename != "")
                                    {
                                        data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                        data = System.Text.Encoding.ASCII.GetBytes("ready");
                                        stream.Write(data, 0, data.Length);
                                        stream.Flush(); //zwolnienie strumienia
                                        stream.Write(data, 0, data.Length);
                                        worker.ClientReceiveStart(client_hostname, client_filename);
                                        Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                        FileStream filestream = new FileStream(config.GetArchiveAddress() + @"\" + client_filename, FileMode.OpenOrCreate, FileAccess.Write); //utworzenie pliku do zapisu archiwum                              
                                        data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                                                      //while ((receive_bytes = stream.Read(data, 0, data.Length)) > 0 && !end_stream) //dopóki przychodzą dane                           
                                        while (!end_stream)
                                        {
                                            if (client.Client.Receive(buff, SocketFlags.Peek) == 0) //jeśli nagle przestał odpowiadać
                                            {
                                                worker.ClientReceiveError(client_hostname, client_filename);
                                                Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                                throw new SocketException();
                                            }
                                            receive_bytes = stream.Read(data, 0, data.Length);
                                            string end_transfer = System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes);
                                            if (end_transfer == "endsending")
                                            {
                                                end_stream = true;
                                            }
                                            else
                                            {
                                                filestream.Write(data, 0, receive_bytes); //kopiowanie danych do pliku
                                            }
                                        }
                                        filestream.Close(); //zamknięcie strumienia pliku    
                                        worker.ClientReceiveEnd(client_hostname, client_filename);
                                        Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                        client_filename = null; //wyczyszczenie nazwy pliku                                                              
                                    }
                                }
                                else
                                {
                                    if (client.Client.Receive(buff, SocketFlags.Peek) == 0) //jeśli nagle przestał odpowiadać
                                    {
                                        client.Client.Disconnect(true); //rozłącz klienta                                                         
                                    }
                                }
                            }
                            catch
                            { }
                        }
                        else if (server_option == ServerOptions.server_send && !help)
                        {
                            if (file_to_send != null) //jeśli wybrany plik istnieje
                            {
                                try
                                {
                                    stream = client.GetStream(); //aktywacja strumienia
                                    data = System.Text.Encoding.ASCII.GetBytes("startsending"); //zakodowanie chęci wysłania pliku
                                    stream.Write(data, 0, data.Length); //wysłanie chęci
                                    stream.Flush(); //zwolnienie strumienia
                                    data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                    receive_bytes = stream.Read(data, 0, data.Length);
                                    if ("confirmtask" == System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes))
                                    {
                                        data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                        data = System.Text.Encoding.ASCII.GetBytes(file_to_send.GetFullFileName()); //zakodowanie nazwy pliku
                                        stream.Write(data, 0, data.Length); //wysłanie nazwy pliku
                                        stream.Flush(); //zwolnienie strumienia
                                        data = new byte[buffer_size];
                                        receive_bytes = stream.Read(data, 0, data.Length);
                                        if ("ready" == System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes))
                                        {
                                            data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                            data = System.Text.Encoding.ASCII.GetBytes(file_to_send.filesize.ToString()); //zakodowanie rozmiaru pliku
                                            stream.Write(data, 0, data.Length); //wysłanie rozmiaru pliku
                                            stream.Flush(); //zwolnienie strumienia
                                            data = new byte[buffer_size];
                                            receive_bytes = stream.Read(data, 0, data.Length);
                                            if ("ready" == System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes))
                                            {
                                                data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                                worker.ClientSendStart(client_hostname, file_to_send.GetFullFileName());
                                                Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                                using (var s = File.OpenRead(file_to_send.filepath)) //dopoki plik jest otwarty
                                                {
                                                    int actually_read; //zmienna pomocnicza do odczytu rozmiaru
                                                    while ((actually_read = s.Read(data, 0, buffer_size)) > 0) //dopóki w pliku sa dane
                                                    {
                                                        stream.Write(data, 0, actually_read); //wyslanie danych z pliku
                                                    }
                                                }
                                                stream.Flush(); //zwolnienie strumienia   
                                                Thread.Sleep(250); //usypianie w celu oddzielenia następnej wiadomości
                                                data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                                data = System.Text.Encoding.ASCII.GetBytes("endsending"); //zakodowanie nazwy pliku
                                                stream.Write(data, 0, data.Length); //wysłanie nazwy pliku
                                                stream.Flush(); //zwolnienie strumienia
                                                worker.ClientSendEnd(client_hostname, file_to_send.GetFullFileName());
                                                Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    stream.Flush();
                                    worker.ClientSendError(client_hostname, client_filename);
                                    Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                    MessageBox.Show(e.ToString());
                                    throw new SocketException();
                                }
                                finally
                                {
                                    server_option = ServerOptions.server_stop;
                                    Dispatcher.Invoke(delegate { tbl_ControlPanelServer.Text = "Wstrzymanie pracy."; }); //aktualizacja aktywnych użytkowników
                                }
                            }
                        }
                        else if (client.Client.Poll(0, SelectMode.SelectRead)) //jeśli klient odpowiada
                        {
                            byte[] buff = new byte[1]; //pomocniczy bufer
                            if (client.Client.Receive(buff, SocketFlags.Peek) == 0) //jeśli nagle przestał odpowiadać
                            {
                                client.Client.Disconnect(true); //rozłącz klienta                                                         
                            }
                        }
                        else if (canceltoken.IsCancellationRequested)
                        {
                            client.Client.Disconnect(true); //rozłącz klienta
                            help = true;
                        }
                    }
                    client.Close(); //zamknięcie klienta                    
                    Dispatcher.Invoke(delegate { updateCounterOfActiveUsers(false); }); //aktualizacja aktywnych użytkowników
                    if (!help)
                    {
                        canceltoken.Cancel();
                        canceltoken.Dispose();
                    }
                    Dispatcher.Invoke(delegate { deleteUserFromList(client_ip); });
                    worker.ClientConnectionEnd(client_hostname);
                    Dispatcher.Invoke(delegate { history_list.Add(worker); });
                }
                catch (SocketException)
                {
                    client.Client.ReceiveTimeout = 3000;
                    if (!client.Client.Poll(0, SelectMode.SelectRead))
                    {
                        client.Client.ReceiveTimeout = 0;
                    }
                    else
                    {
                        client.Close(); //zamknięcie klienta
                        Dispatcher.Invoke(delegate { updateCounterOfActiveUsers(false); }); //aktualizacja aktywnych użytkowników
                        if (!help)
                        {
                            canceltoken.Cancel();
                            canceltoken.Dispose();
                        }
                        Dispatcher.Invoke(delegate { deleteUserFromList(client_ip); });
                        worker.ClientConnectionEnd(client_hostname);
                        Dispatcher.Invoke(delegate { history_list.Add(worker); });
                    }
                }
                catch (IOException)
                {
                    client.Close(); //zamknięcie klienta
                    Dispatcher.Invoke(delegate { updateCounterOfActiveUsers(false); }); //aktualizacja aktywnych użytkowników
                    if (!help)
                    {
                        canceltoken.Cancel();
                        canceltoken.Dispose();
                    }
                    Dispatcher.Invoke(delegate { deleteUserFromList(client_ip); });
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString()); //komunikat o błędzie
                }
            }
        }

        private void deleteUserFromList(string _ip)
        {
            foreach (var _client in client_list)
            {
                if (_client.IpAddress == _ip)
                {
                    client_list.Remove(_client);
                    break;
                }
            }
        }

        private void updateCounterOfActiveUsers(bool x) //funkcja do aktualizowania aktywnych połączeń
        {
            if (x) //jeśli true
            {
                active_clients++; //zwiększenie listy aktywnych klientów
                tbl_ControlPanelUsercCounters.Text = active_clients.ToString() + " / 20";
                rpb_ControlPanelUsersCounter.Value = (active_clients * 5);
            }
            else
            {
                active_clients--; //zmniejszenie liczby aktywnych klientów
                tbl_ControlPanelUsercCounters.Text = active_clients.ToString() + " / 20";
                if (active_clients != 0)
                {
                    rpb_ControlPanelUsersCounter.Value = (active_clients * 5);
                }
                else
                {
                    rpb_ControlPanelUsersCounter.Value = 0;
                }
            }
        }

        private void BackgroundWorkerClose() //funkcja do przerywania wątka w tle
        {
            if (m_oBackgroundWorker != null) //jeśli obiekt istnieje
            {
                if (m_oBackgroundWorker.IsBusy) //sprawdzanie czy taki wątek istnieje
                {
                    m_oBackgroundWorker.CancelAsync();//przerwanie wątka roboczego
                }
            }
        }

        private void btn_ControlPanelServerListen_Click(object sender, RoutedEventArgs e)
        {
            server_option = ServerOptions.server_listen;
            BackgroundWorkerClose();
            tbl_ControlPanelServer.Text = "Oczekiwanie na podłączenie klientów.";
            ConnectionListen();
        }

        private void btn_ControlPanelServerSend_Click(object sender, RoutedEventArgs e)
        {
            server_option = ServerOptions.server_send;
            BackgroundWorkerClose();
            tbl_ControlPanelServer.Text = "Wysyłanie plików podłączonym klientom.";
        }

        private void btn_ControlPanelServerStop_Click(object sender, RoutedEventArgs e)
        {
            server_option = ServerOptions.server_stop;
            BackgroundWorkerClose();
            tbl_ControlPanelServer.Text = "Wstrzymanie pracy.";
        }

        private void btn_ControlPanelServerReceive_Click(object sender, RoutedEventArgs e)
        {
            server_option = ServerOptions.server_receive;
            BackgroundWorkerClose();
            tbl_ControlPanelServer.Text = "Oczekiwanie na przesłanie plików.";
        }

    }    
}
