using System;
using System.Windows;
using System.IO;
using Microsoft.Win32;


namespace Server
{
    class ServerConfigurationSave
    {
        public static bool SaveToFile(int _port, int _buffersize, string _archiveaddress)
        {
            bool result;

            SaveFileDialog sfg = new SaveFileDialog(); //utworzenie okna do przeglądania plików
            sfg.Filter = "txt files (*.txt)|*.txt|xml files (*.xml)|*.xml"; //ustawienie filtrów okna na pliki txt i xml
            sfg.FilterIndex = 1; //ustawienie domyślnego filtru na plik txt
            sfg.RestoreDirectory = true; //przywracanie wcześniej zamkniętego katalogu

            if (sfg.ShowDialog() == true)//wyświetlenie okna ze sprawdzeniem, czy plik został zapisany
            {
                if (sfg.FilterIndex == 1) //zapis dla pliku txt
                {
                    File.WriteAllText(sfg.FileName, "port_tcp=" + '"' + _port + '"' +
                    Environment.NewLine + "buffer_size=" + '"' + _buffersize + '"' +
                    Environment.NewLine + "archive_address=" + '"' + _archiveaddress + '"'); //stworzenie lub nadpisanie pliku        
                }
                else if (sfg.FilterIndex == 2) //zapis dla pliku xml
                {
                    File.WriteAllText(sfg.FileName, "<serwer>" +
                        Environment.NewLine + "    <configure>" +
                        Environment.NewLine + "        port_tcp=" + '"' + _port + '"' +
                        Environment.NewLine + "        buffer_size=" + '"' + _buffersize + '"' +
                        Environment.NewLine + "        archive_address=" + '"' + _archiveaddress + '"' +
                        Environment.NewLine + "    </configure>" +
                        Environment.NewLine + "</serwer>"); //stworzenie lub nadpisanie pliku 
                }
                result = true;                
            }
            else
            {
                result = false;
            }

            return result;
        }
    }
}
