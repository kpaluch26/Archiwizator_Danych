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

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        public MainWindow()
        {
            GetPhysicallyInstalledSystemMemory(out total_ram);
            total_ram = total_ram / 1024;
            InitializeComponent();            
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

        private void btn_ConfigurationPanel_Click(object sender, RoutedEventArgs e)
        {
            CleanServer();
            grd_Configuration.Visibility = Visibility.Visible;
        }

        private void btn_ControlPanel_Click(object sender, RoutedEventArgs e)
        {
            CleanServer();
        }
        private void btn_ArchivePanel_Click(object sender, RoutedEventArgs e)
        {
            CleanServer();
        }
        private void btn_HistoryPanel_Click(object sender, RoutedEventArgs e)
        {
            CleanServer();
        }

        private void btn_ResourcesMonitor_Click(object sender, RoutedEventArgs e)
        {
            CleanServer();
            grd_ResourcesMonitor.Visibility = Visibility.Visible;
            resources_thread = new Thread(ResorcesMonitor);
            resources_thread.Start();
        }

        private void btn_ConfigurationLoad_Click(object sender, RoutedEventArgs e)
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
                        pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
                    }
                }
            }
            else
            {
                pic_ConfigurationLoad.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
            }

            if (is_config_correct || config != null)
            {
                flp_ConfigurationSave.IsEnabled = true; //można eksportować do pliku
            }
        }

        private void btn_ConfigurationSave_Click(object sender, RoutedEventArgs e)
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
            }
            else
            {
                Console.WriteLine("Nie zapisano pliku. Powrót do menu głównego."); //komunikat
                pic_ConfigurationSave.Kind = MaterialDesignThemes.Wpf.PackIconKind.Close; //zmiana ikony na niepowodzenie operacji
            }
        }

        private void btn_ConfigurationCreate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_ConfigurationCreateSave_Click(object sender, RoutedEventArgs e)
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
                }
                catch
                {                   
                    if(config != null) 
                    {
                        MessageBox.Show("Wprowadzono błędne dane. Powrót do istniejącej konfiguracji.", "Błąd.");
                        txt_ConfigurationPort.Text = config.GetPort().ToString();
                        cmb_ConfigurationBuffer.Text = config.GetBufferSize().ToString();
                    }
                    else
                    {
                        MessageBox.Show("Wprowadzono błędne dane.", "Błąd.");
                        btn_ConfigurationCreateSave.Command.Execute(null);
                    }
                }                  
            }
            else
            {
                if (config != null)
                {
                    MessageBox.Show("Nie podano miejsca zapisu dla przychodzących plików. Powrót do istniejącej konfiguracji. ", "Błąd.");
                }
                else
                {
                    MessageBox.Show("Nie podano miejsca zapisu dla przychodzących plików.", "Błąd.");
                    btn_ConfigurationCreateSave.Command.Execute(null);
                }
            }
        }

        private void ResorcesMonitor()
        {
            PerformanceCounter cpu_usage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter ram_usage = new PerformanceCounter("Memory", "Available MBytes");
            PerformanceCounter disk_usage = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            var firstCall = cpu_usage.NextValue();

            while (grd_ResourcesMonitor.Visibility == Visibility.Visible)
            {
                cpb_CPU.Dispatcher.Invoke(delegate { cpb_CPU.Progress = Math.Round(cpu_usage.NextValue(),2); });
                cpb_RAM.Dispatcher.Invoke(delegate { cpb_RAM.Progress = Math.Round(((total_ram - ram_usage.NextValue()) * 100 / total_ram),2); });
                cpb_DISK.Dispatcher.Invoke(delegate { cpb_DISK.Progress = Math.Round(disk_usage.NextValue(),2); });
                Thread.Sleep(1000);
            }
        }

        private void CleanServer()
        {
            grd_ResourcesMonitor.Visibility = Visibility.Collapsed;
            grd_Configuration.Visibility = Visibility.Collapsed;

            if (resources_thread != null && resources_thread.IsAlive)
            {
                resources_thread.Join();
            }
        }
    }
}
