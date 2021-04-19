using System;
using System.Linq;
using System.Windows;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace Server
{
    class ServerConfigurationLoad 
    {
        private static string load_error;

        public static ServerConfiguration LoadFromFile()
        {
            ServerConfiguration config = null; ;
            StreamReader file; //zmienna do odczytu pliku
            IPHostEntry ip_entry;
            IPAddress[] all_address;
            string[] result = new string[2]; //tablica zmiennych do odczytu konfiguracji
            string filePath, line, ip_address, username, hostname, archive_address = ""; //zmienne pomocnicze do konfiguracji serwera
            int port = 0, buffer_size = 0, counterp = 0, counterb = 0, countera = 0; //zmienne pomocnicze sprawdzające poprawność importowanych danych

            username = Environment.UserName; //odczyt nazwy konta użytkownika
            hostname = Dns.GetHostName(); //odczyt hostname
            ip_entry = System.Net.Dns.GetHostEntry(hostname);
            all_address = ip_entry.AddressList;           
            ip_address = all_address.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault().ToString();                               

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
                            config = new ServerConfiguration(username, hostname, ip_address, archive_address, port, buffer_size);//utworzenie configa                         
                        }
                    }
                    catch (FileLoadException)
                    {                        
                        load_error = "UWAGA! Wczytanie konfiguracji nie powiodło się. Plik konfiguracyjny jest uszkodzony.";
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
                            config = new ServerConfiguration(username, hostname, ip_address, archive_address, port, buffer_size);//utworzenie configa   
                            return config;
                        }
                    }
                    catch (FileLoadException)
                    {                        
                        load_error = "UWAGA! Wczytanie konfiguracji nie powiodło się. Plik konfiguracyjny jest uszkodzony.";
                    }
                }
            }
            else
            {                
                load_error = "UWAGA! Nie wybrano pliku z konfiguracją serwera.";
            }
            return config;
        }

        public static string GetError()
        {
            return load_error;
        }
    }
}
