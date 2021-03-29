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
        private int users_counter = 0;
        private FileInformation file_to_send = new FileInformation();


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
            tbl_ControlPanelUsercCounters.Text = users_counter.ToString() + " / 20";
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
            string ip_address = Dns.GetHostByName(hostName).AddressList[1].ToString(); // odczyt adresu IPv4
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
                            ServerConfigurationUpdate();
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
                            ServerConfigurationUpdate();
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

        private void ServerConfigurationUpdate()
        {
            tbl_ControlPanelUserName.Text = config.GetUserName();
            tbl_ControlPanelIP.Text = config.GetIPAddress();
            tbl_ControlPanelHostName.Text = config.GetHostName();
            tbl_ControlPanelSavePath.Text = config.GetArchiveAddress();
            tbl_ControlPanelBufferSize.Text = config.GetBufferSize().ToString();
            tbl_ControlPanelPort.Text = config.GetPort().ToString();
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
                string ip_address = Dns.GetHostByName(hostName).AddressList[1].ToString(); // odczyt adresu IPv4
                try
                {
                    port = Convert.ToInt32(txt_ConfigurationPort.Text);
                    buffer = Convert.ToInt32(cmb_ConfigurationBuffer.Text);
                    config = new ServerConfiguration(username, hostName, ip_address, fbd.SelectedPath, port, buffer);//utworzenie configa
                    btn_ConfigurationCreate.Content = "Edytuj konfigurację";
                    flp_ConfigurationSave.IsEnabled = true;
                    tbl_ConfigurationAllert.Visibility = Visibility.Hidden;
                    ServerConfigurationUpdate();
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
            grd_ControlPanel.Visibility = Visibility.Collapsed;

            if (resources_thread != null && resources_thread.IsAlive)
            {
                resources_thread.Join();
                ResourcesMonitorUpdate(0, 0, 0);
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
            string filename = tbx_ArchivePanelFilename.Text;
            string password = pbx_ArchivePanelPasswordBox.Password;
            string path = null;
            List<FileInformation> _files_list = new List<FileInformation>();

            try
            {
                if(filename != null && filename.Trim() != "")
                {
                    if (cbx_ArchivePanelSavePathFromFile.IsChecked == true)
                    {
                        if (config != null)
                        {
                            path = config.GetArchiveAddress() + "\\" + filename + ".zip";
                        }
                        else
                        {
                            tbl_ArchivePanelAllert.Text = "UWAGA! Domyślne miejsce zapisu nieustawione. Załaduj plik konfiguracyjny lub ręcznie wybierz miejsce zapisu";
                            tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                            pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
                            throw new Exception();
                        }
                    }
                    else
                    {
                        Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
                        fbd.Description = "Wybierz ścieżkę dostępu."; //tytuł utworzonego okna
                        fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

                        if (fbd.ShowDialog() == true)
                        {
                            path = fbd.SelectedPath + "\\" + filename + ".zip";
                        }
                        else
                        {
                            tbl_ArchivePanelAllert.Text = "UWAGA! Nie wybrano miejsca zapisu nowego archiwum.";
                            tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                            pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
                            throw new Exception();
                        }
                    }

                    if (files_list.Count >= 1)
                    {
                        for (int i = 0; i < files_list.Count; i++)
                        {
                            if (files_list[i].is_checked == true)
                            {
                                _files_list.Add(files_list[i]);
                            }
                        }
                        if (_files_list.Count >= 1)
                        {
                            using (ZipFile _zip = new ZipFile()) //utworzenie archiwum
                            {
                                foreach (var _file in _files_list)
                                {
                                    if (password != null && password.Trim() != "")
                                    {
                                        _zip.Password = password; //dodanie hasła
                                    }
                                    _zip.AddFile((_file.filepath + "\\" + _file.filename + _file.filetype), ""); //dodanie pliku do archiwum
                                }
                                _zip.Save(path); //zapis archiwum
                                if (cbx_ArchivePanelSetToSend.IsChecked == true)
                                {
                                    FileToSendSet(path);                                   
                                }
                                tbl_ArchivePanelAllert.Text = "";
                                tbl_ArchivePanelAllert.Visibility = Visibility.Collapsed;
                                pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check;
                            }
                        }
                        else
                        {
                            tbl_ArchivePanelAllert.Text = "UWAGA! Na liście brak zaznaczonych plików do skompresowania.";
                            tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                            pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
                        }
                    }
                    else
                    {
                        tbl_ArchivePanelAllert.Text = "UWAGA! Na liście brak plików do skompresowania.";
                        tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                        pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
                    }
                }
                else
                {
                    tbl_ArchivePanelAllert.Text = "UWAGA! Nie utworzono archiwum. Podaj nazwę archiwum, które chcesz utworzyć.";
                    tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                    pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
                }
            }
            catch (FileNotFoundException)
            {
                tbl_ArchivePanelAllert.Text = "UWAGA! Błąd tworzenia. Przynajmniej jeden wybrany plik zmienił ścieżke dostępu.";
                tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
            }
            catch (ArgumentException)
            {
                tbl_ArchivePanelAllert.Text = "UWAGA! Błąd tworzenia. Nazwa archiwum zawiera niedozwolone znaki.";
                tbl_ArchivePanelAllert.Visibility = Visibility.Visible;
                pic_ArchivePanelCreateZIP.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close;
            }
            catch (Exception)
            {

            }
        }

        private void FileToSendSet(string path)
        {
            FileInfo _file_to_send = new FileInfo(path);

            file_to_send.filename = Path.GetFileNameWithoutExtension(_file_to_send.Name);
            file_to_send.filepath = path;
            file_to_send.filetype = _file_to_send.Extension;
            file_to_send.filesize = _file_to_send.Length;
            file_to_send.is_checked = true;

            tbl_ControlPanelFileName.Text = _file_to_send.Name;
            tbl_ControlPanelFileLocation.Text = path;
            tbl_ControlPanelFileSize.Text = FormatSize(_file_to_send.Length);
        }

        public static string FormatSize(Int64 bytes)
        {
            string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
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
                FileToSendSet(ofd.FileName);
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
    }    
}
