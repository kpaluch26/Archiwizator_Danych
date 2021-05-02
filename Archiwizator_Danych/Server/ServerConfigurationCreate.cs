using System;
using System.Linq;
using System.Windows;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace Server
{
    class ServerConfigurationCreate
    {
        private static string create_error;

        public static ServerConfiguration CreateConfiguration(string _port, string _buffer, bool _iscreated)
        {
            ServerConfiguration config = null;
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog(); //utworzenie okna dialogowego do wybrania ścieżki zapisu otrzymanych plików
            fbd.Description = "Wybierz ścieżkę dostępu."; //tytuł utworzonego okna
            fbd.ShowNewFolderButton = true; //włączenie mozliwości tworzenia nowych folderów

            if (fbd.ShowDialog() == true) //jeśli wybrano ścieżkę
            {
                int port, buffer;
                string username, hostname, ip_address;
                IPHostEntry ip_entry;
                IPAddress[] all_address;

                username = Environment.UserName; //odczyt nazwy konta użytkownika
                hostname = Dns.GetHostName(); //odczyt hostname
                ip_entry = System.Net.Dns.GetHostEntry(hostname);
                all_address = ip_entry.AddressList;
                ip_address = all_address.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault().ToString();

                try
                {
                    port = Convert.ToInt32(_port);
                    buffer = Convert.ToInt32(_buffer);
                    config = new ServerConfiguration(username, hostname, ip_address, fbd.SelectedPath, port, buffer);//utworzenie configa                    
                }
                catch
                {
                    if (_iscreated)
                    {
                        create_error = "UWAGA! Nie wprowadzono danych niezbędnych do utworzenia konfiguracji lub dane są niepoprawne, powrót do istniejącej konfiguracji.";                        
                    }
                    else
                    {
                        create_error = "UWAGA! Nie wprowadzono danych niezbędnych do utworzenia konfiguracji lub dane są niepoprawne.";
                    }
                }
            }
            else
            {
                if (_iscreated)
                {
                    create_error = "UWAGA! Nie podano miejsca zapisu dla przychodzących plików. Powrót do istniejącej konfiguracji.";
                }
                else
                {
                    create_error = "UWAGA! Nie podano miejsca zapisu dla przychodzących plików.";
                }
            }

            return config;
        }

        public static string GetError()
        {
            return create_error;
        }
    }
}
