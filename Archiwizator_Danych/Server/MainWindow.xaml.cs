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
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServerConfiguration config;
        private static Thread resources_thread;
        private long total_ram;
        ObservableCollection<FileInformation> files_list = new ObservableCollection<FileInformation>();


        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        public MainWindow()
        {
            GetPhysicallyInstalledSystemMemory(out total_ram);
            total_ram = total_ram / 1024;
            InitializeComponent();
            files_list.Clear();            
            dgr_ArchivePanelFiles.ItemsSource = files_list;            
        }

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
            tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
        }

        private void btn_ControlPanel_Click(object sender, RoutedEventArgs e) //otwieranie głównego panelu sterowania
        {
            CleanServer();
        }
        private void btn_ArchivePanel_Click(object sender, RoutedEventArgs e) //otwieranie panelu do zarządzania zip
        {
            CleanServer();
            grd_ArchivePanel.Visibility = Visibility.Visible;
        }
        private void btn_HistoryPanel_Click(object sender, RoutedEventArgs e) //otwieranie historii pracy serwera i aktywności klientów
        {
            CleanServer();
        }

        private void btn_ResourcesMonitor_Click(object sender, RoutedEventArgs e) //śledzenie wydajności zasobów komputera
        {
            CleanServer();
            grd_ResourcesMonitor.Visibility = Visibility.Visible;
            resources_thread = new Thread(ResourcesMonitor);
            resources_thread.Start();
        }

        private void btn_ConfigurationLoad_Click(object sender, RoutedEventArgs e) //funkcja do załadowania pliku konfiguracyjnego
        {
            StreamReader file; //zmienna do odczytu pliku
            string[] result = new string[2]; //tablica zmiennych do odczytu konfiguracji
            string filePath, line, archive_address = ""; //zmienne pomocnicze do konfiguracji serwera
            int port = 0, buffer_size = 0, counterp = 0, counterb = 0, countera = 0; //zmienne pomocnicze sprawdzające poprawność importowanych danych
            string username = Environment.UserName; //odczyt nazwy konta użytkownika
            string hostName = Dns.GetHostName(); //odczyt hostname
            string ip_address = Dns.GetHostByName(hostName).AddressList[0].ToString(); // odczyt adresu IPv4
            bool is_config_correct = false; //flaga do sterowania możliwością eksportu configa

            OpenFileDialog ofd = new OpenFileDialog(); //utworzenie okna do przeglądania plików konfiguracji
            ofd.Filter = "txt files (*.txt)|*.txt|xml files (*.xml)|*.xml"; //ustawienie filtrów okna na pliki txt i xml
            ofd.FilterIndex = 1; //ustawienie domyślnego filtru na plik txt
            ofd.RestoreDirectory = true; //przywracanie wcześniej zamkniętego katalogu

            if (ofd.ShowDialog() == true) //wyświetlenie okna ze sprawdzeniem, czy plik został wybrany
            {
                filePath = ofd.FileName; //przypisanie ścieżki wybranego pliku do zmiennej

                if (ofd.FilterIndex == 1)//odczyt dla pliku txt
                {
                    try
                    {
                        file = new StreamReader(filePath); //utworzenie odczytu pliku
                        while ((line = file.ReadLine()) != null) //dopóki są linie w pliku
                        {
                            line = String.Concat(line.Where(x => !Char.IsWhiteSpace(x))); //usunięcie wszelkich znaków białych z linii
                            result = line.Split('='); //podzielenie odczytanej linii wykorzystując separator
                            switch (result[0].ToLower()) //zmiana liter na małe w poleceniu
                            {
                                case "port_tcp": //polecenie
                                    port = Convert.ToInt32(result[1].Substring(1, result[1].Length - 2)); //przypisanie numeru portu odczytanego z pliku txt
                                    counterp = 1; //poprawny format
                                    break;
                                case "buffer_size": //polecenie
                                    buffer_size = Convert.ToInt32(result[1].Substring(1, result[1].Length - 2)); //przypisanie rozmiaru buffera odczytanego z pliku txt
                                    counterb = 1; //poprawny format
                                    break;
                                case "archive_address": //polecenie
                                    archive_address = Convert.ToString(result[1].Substring(1, result[1].Length - 2)); //przypisanie ścieżki zapisu otrzymanych plików
                                    countera = 1; //poprawny format
                                    break;
                            }
                            if (counterp + counterb + countera == 3) //jeśli wczytano wszystkie niezbędne dane
                            {
                                break; //przerwij dalsze wczytywanie
                            }
                        }
                        file.Close(); //zamknięcie pliku
                        if (counterp + counterb + countera != 3) //jeśli nie wczytano wszystkich niezbędnych danych
                        {
                            file.Close(); //zamknięcie pliku
                            throw new FileLoadException(); //wyrzucenie wyjątku
                        }
                        else
                        {
                            config = new ServerConfiguration(username, hostName, ip_address, archive_address, port, buffer_size);//utworzenie configa   
                            pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check; //zmiana ikony na powodzenie operacji
                            is_config_correct = true;
                        }
                    }
                    catch (FileLoadException)
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Wczytanie konfiguracji nie powiodło się. Plik konfiguracyjny jest uszkodzony.";
                        tbl_ConfigurationAllert.FontSize = 18;
                        tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                        pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
                    }
                }
                else if (ofd.FilterIndex == 2)//odczyt dla pliku xml
                {
                    try
                    {
                        bool serwer = false; //zmienna pomocnicza do odczytu konfiguracji dla serwera
                        bool configure = false; //zmienna pomocnicza sprawdzająca poprawność importowanych danych
                        file = new StreamReader(filePath); //utworzenie odczytu pliku
                        while ((line = file.ReadLine()) != null) //dopóki są linie w pliku
                        {
                            line = String.Concat(line.Where(x => !Char.IsWhiteSpace(x))); //usunięcie wszelkich znaków białych z linii
                            if (line.ToLower() == "<serwer>") //początek konfiguracji serwera
                            {
                                serwer = true; //ustawienie odczytu danych dla serwera
                            }
                            else if (line.ToLower() == "</serwer>") //koniec konfiguracji serwera
                            {
                                serwer = false; //przerwanie odczytu danych
                                break;
                            }
                            else if (line.ToLower() == "<configure>" && serwer) //początek konfiguracji
                            {
                                configure = true; //ustawienie odczytu konfiguracji
                            }
                            else if (line.ToLower() == "</configure>" && serwer) //koniec konfiguracji
                            {
                                configure = false; //przerwanie konfiguracji
                            }
                            else if (serwer && configure) //jeśli konfiguracja obowiązuje dla serwera
                            {
                                result = line.Split('='); //podzielenie odczytanej linii wykorzystując separator
                                switch (result[0].ToLower()) //ustawienie małych liter poleceń
                                {
                                    case "port_tcp": //polecenie
                                        port = Convert.ToInt32(result[1].Substring(1, result[1].Length - 2)); //przypisanie numeru portu odczytanego z pliku xml
                                        counterp = 1; //poprawny format
                                        break;
                                    case "buffer_size": //polecenie
                                        buffer_size = Convert.ToInt32(result[1].Substring(1, result[1].Length - 2)); //przypisanie numeru portu odczytanego z pliku xml
                                        counterb = 1; //poprawny format
                                        break;
                                    case "archive_address": //polecenie 
                                        archive_address = Convert.ToString(result[1].Substring(1, result[1].Length - 2)); //przypisanie ścieżki zapisu otrzymanych plików
                                        countera = 1; //poprawny format
                                        break;
                                }
                            }
                        }
                        file.Close(); //zamknięcie pliku
                        if (counterp + counterb + countera != 3) //jeśli niepoprawny format konfiguracji
                        {
                            file.Close(); //zamknięcie pliku
                            throw new FileLoadException(); //wyrzuca wyjątek
                        }
                        else
                        {
                            config = new ServerConfiguration(username, hostName, ip_address, archive_address, port, buffer_size);//utworzenie configa                             
                            pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check; //zmiana ikony na powodzenie operacji
                            is_config_correct = true;
                        }
                    }
                    catch (FileLoadException)
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Wczytanie konfiguracji nie powiodło się. Plik konfiguracyjny jest uszkodzony.";
                        tbl_ConfigurationAllert.FontSize = 18;
                        tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                        pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
                    }
                }
            }
            else
            {
                tbl_ConfigurationAllert.Text = "UWAGA! Nie wybrano pliku z konfiguracją serwera.";
                tbl_ConfigurationAllert.FontSize = 18;
                tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
            }

            if (is_config_correct || config != null)
            {
                flp_ConfigurationSave.IsEnabled = true; //można eksportować do pliku
            }
        }

        private void btn_ConfigurationSave_Click(object sender, RoutedEventArgs e) //funkcja do zapisu konfiguracji do pliku
        {
            SaveFileDialog sfg = new SaveFileDialog(); //utworzenie okna do przeglądania plików
            sfg.Filter = "txt files (*.txt)|*.txt|xml files (*.xml)|*.xml"; //ustawienie filtrów okna na pliki txt i xml
            sfg.FilterIndex = 1; //ustawienie domyślnego filtru na plik txt
            sfg.RestoreDirectory = true; //przywracanie wcześniej zamkniętego katalogu

            if (sfg.ShowDialog() == true)//wyświetlenie okna ze sprawdzeniem, czy plik został zapisany
            {
                if (sfg.FilterIndex == 1) //zapis dla pliku txt
                {
                    File.WriteAllText(sfg.FileName, "port_tcp=" + '"' + config.GetPort() + '"' +
                    Environment.NewLine + "buffer_size=" + '"' + config.GetBufferSize() + '"' +
                    Environment.NewLine + "archive_address=" + '"' + config.GetArchiveAddress() + '"'); //stworzenie lub nadpisanie pliku        
                }
                else if (sfg.FilterIndex == 2) //zapis dla pliku xml
                {
                    File.WriteAllText(sfg.FileName, "<serwer>" +
                        Environment.NewLine + "    <configure>" +
                        Environment.NewLine + "        port_tcp=" + '"' + config.GetPort() + '"' +
                        Environment.NewLine + "        buffer_size=" + '"' + config.GetBufferSize() + '"' +
                        Environment.NewLine + "        archive_address=" + '"' + config.GetArchiveAddress() + '"' +
                        Environment.NewLine + "    </configure>" +
                        Environment.NewLine + "</serwer>"); //stworzenie lub nadpisanie pliku 
                }
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
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
            fbd.Description = "Wybierz ścieżkę dostępu."; //tytuł utworzonego okna
            fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

            if (fbd.ShowDialog() == true) //jeśli wybrano ścieżkę
            {
                int port, buffer;
                string username = Environment.UserName; //odczyt nazwy konta użytkownika
                string hostName = Dns.GetHostName(); //odczyt hostname
                string ip_address = Dns.GetHostByName(hostName).AddressList[0].ToString(); // odczyt adresu IPv4

                try
                {
                    port = Convert.ToInt32(txt_ConfigurationPort.Text);
                    buffer = Convert.ToInt32(cmb_ConfigurationBuffer.Text);
                    config = new ServerConfiguration(username, hostName, ip_address, fbd.SelectedPath, port, buffer);//utworzenie configa
                    btn_ConfigurationCreate.Content = "Edytuj konfigurację";
                    flp_ConfigurationSave.IsEnabled = true;
                    tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
                }
                catch
                {
                    if (config != null)
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Nie wprowadzono wszystkich danych niezbędnych do utworzenia konfiguracji lub wprowadzone dane są niepoprawne, powrót do istniejącej konfiguracji.";
                        tbl_ConfigurationAllert.FontSize = 12;
                        tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                        txt_ConfigurationPort.Text = config.GetPort().ToString();
                        cmb_ConfigurationBuffer.Text = config.GetBufferSize().ToString();
                    }
                    else
                    {
                        tbl_ConfigurationAllert.Text = "UWAGA! Nie wprowadzono wszystkich danych niezbędnych do utworzenia konfiguracji lub wprowadzone dane są niepoprawne.";
                        tbl_ConfigurationAllert.FontSize = 16;
                        tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                        btn_ConfigurationCreateSave.Command.Execute(null);
                    }
                }
            }
            else
            {
                if (config != null)
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano miejsca zapisu dla przychodzących plików. Powrót do istniejącej konfiguracji.";
                    tbl_ConfigurationAllert.FontSize = 18;
                    tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                }
                else
                {
                    tbl_ConfigurationAllert.Text = "UWAGA! Nie podano miejsca zapisu dla przychodzących plików.";
                    tbl_ConfigurationAllert.FontSize = 18;
                    tbl_ConfigurationAllert.Visibility = Visibility.Visible;
                    btn_ConfigurationCreateSave.Command.Execute(null);
                }
            }
        }

        private void ResourcesMonitor() //metoda do odczytu uźycia podzespołów
        {
            PerformanceCounter cpu_usage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter ram_usage = new PerformanceCounter("Memory", "Available MBytes");
            PerformanceCounter disk_usage = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            var firstCall = cpu_usage.NextValue();
            Thread.Sleep(100);

            while (grd_ResourcesMonitor.Visibility == Visibility.Visible)
            {
                double cpu = Math.Round(cpu_usage.NextValue(), 2);
                double ram = Math.Round(((total_ram - ram_usage.NextValue()) * 100 / total_ram), 2);
                double disk = Math.Round(disk_usage.NextValue(), 2);
                Dispatcher.Invoke(delegate { ResourcesMonitorUpdate(cpu, ram, disk); });
                Thread.Sleep(1000);
            }
        }

        private void CleanServer() //funkcja do czyszczenia pozostałości po wcześniej otwartym oknie
        {
            grd_ResourcesMonitor.Visibility = Visibility.Collapsed;
            grd_Configuration.Visibility = Visibility.Collapsed;
            grd_ArchivePanel.Visibility = Visibility.Collapsed;

            if (resources_thread != null && resources_thread.IsAlive)
            {
                resources_thread.Join();
            }
        }

        private void ResourcesMonitorUpdate(double cpu, double ram, double disk) //funkcja do aktualizacji użycia podzespołów
        {
            double safe_usage = 75;
            tbl_ResourcesMonitorAllert.Text = "UWAGA! Duże wykorzystanie podzespołów: ";

            if (cpu > safe_usage)
            {
                tbl_ResourcesMonitorAllert.Text += "CPU ";
            }
            if (ram > safe_usage)
            {
                tbl_ResourcesMonitorAllert.Text += "RAM ";
            }
            if (disk > safe_usage)
            {
                tbl_ResourcesMonitorAllert.Text += "DISK ";
            }
            if (cpu > safe_usage || ram > safe_usage || disk > safe_usage)
            {
                tbl_ResourcesMonitorAllert.Visibility = Visibility.Visible;
            }
            else
            {
                tbl_ResourcesMonitorAllert.Visibility = Visibility.Hidden;
            }

            rpb_CPU.Value = cpu;
            rpb_RAM.Value = ram;
            rpb_DISK.Value = disk;
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

        private string FileSizeCalculate(Int64 _filesize) // funkcja do obliczania rozmiaru pliku
        {             
            string[] suffixes = { " B", " KB", " MB", " GB" };

            int counter = 0;
            decimal number = (decimal)_filesize;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
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

        private void cbx_ArchviePanelFileSelectedChangeValue(object sender, RoutedEventArgs e)
        {
            if (cbx_ArchivePanelSelectAllFiles.IsChecked == true)
            {
                cbx_ArchivePanelSelectAllFiles.IsChecked = false;
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

        private void btn_ArchivePanelCreateZIP_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_ArchivePanelDataGridClear_Click(object sender, RoutedEventArgs e)
        {
            files_list.Clear();
            btn_ArchivePanelDataGridClear.IsEnabled = false;
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
    }    
}
