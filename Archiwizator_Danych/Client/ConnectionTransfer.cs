using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    class ConnectionTransfer
    {
        private static MainWindow MW;
        private static NetworkStream NS;
        
        public static void SetConnection(MainWindow _mw, NetworkStream _ns)
        {
            MW = _mw;
            NS = _ns;
        }
        
        public static void ReceiveFile()
        {
            int receive_bytes, buffer = Int32.Parse(MW.cmb_ConfigurationBuffer.Text);
            byte[] data = new byte[buffer];
            bool end_stream = false;            
            string filename;
            long size;

            NS.Flush();
            try
            {             
                data = System.Text.Encoding.ASCII.GetBytes("confirmtask"); //zakodowanie startu transmisji
                NS.Write(data, 0, data.Length); //wysłanie startu
                NS.Flush(); //zwolnienie strumienia

                data = new byte[buffer];
                receive_bytes = NS.Read(data, 0, buffer);
                filename = System.Text.Encoding.UTF8.GetString(data, 0, receive_bytes);

                if (filename != null && filename != "")
                {
                    data = new byte[buffer];
                    data = System.Text.Encoding.ASCII.GetBytes("ready");
                    NS.Write(data, 0, data.Length);
                    NS.Flush();

                    data = new byte[buffer];
                    receive_bytes = NS.Read(data, 0, buffer);
                    string file_size = System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes);

                    if (long.TryParse(file_size, out size))
                    {
                        long steps = (size / buffer) + 1;
                        double steps_counter = 1;
                        double step_to_percent = steps/100;
                        int percent;
                        int progress = 0;

                        if (steps < 100) 
                        {
                            percent = 100 / (int)steps;
                        }
                        else
                        {
                            percent = 1;
                        }

                        MW.CleanClient();
                        MW.grd_ControlPanel.Visibility = Visibility.Visible; 
                        MW.rpb_ControlPanelProgressBar.Dispatcher.Invoke(() => MW.rpb_ControlPanelProgressBar.Value = 0, System.Windows.Threading.DispatcherPriority.Background);                        
                        MW.tbl_ControlPanelProgressValue.Text = "0 %";
                        MW.tbl_ControlPanelOperation.Text = "Pobieranie pliku";

                        data = new byte[buffer];
                        data = System.Text.Encoding.ASCII.GetBytes("ready");
                        NS.Write(data, 0, data.Length);
                        NS.Flush();

                        FileStream filestream = new FileStream(MW.tbl_ConfigurationSavePath.Text + @"\" + filename, FileMode.OpenOrCreate, FileAccess.Write); //utworzenie pliku do zapisu archiwum
                        data = new byte[buffer];
                        while (!end_stream)
                        {                            
                            receive_bytes = NS.Read(data, 0, data.Length);
                            string end_transfer = System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes);

                            if (receive_bytes>=10 && end_transfer.Remove(0, (receive_bytes - 10)) == "endsending")
                            {
                                filestream.Write(data, 0, (receive_bytes - 10)); //kopiowanie danych do pliku
                                end_stream = true;
                                MW.rpb_ControlPanelProgressBar.Dispatcher.Invoke(() => MW.rpb_ControlPanelProgressBar.Value = 100, System.Windows.Threading.DispatcherPriority.Background);
                                MW.tbl_ControlPanelProgressValue.Text = "100 %";
                                MW.tbl_ControlPanelOperation.Text = "Otrzymano plik";
                            }
                            else
                            {
                                filestream.Write(data, 0, receive_bytes); //kopiowanie danych do pliku                                                 
                                if (step_to_percent - steps_counter <= 0)
                                {
                                    progress += percent;
                                    MW.rpb_ControlPanelProgressBar.Dispatcher.Invoke(() => MW.rpb_ControlPanelProgressBar.Value = progress, System.Windows.Threading.DispatcherPriority.Background);
                                    MW.tbl_ControlPanelProgressValue.Text = progress.ToString() + " %";
                                    steps_counter -= step_to_percent;
                                }
                                steps_counter++;
                            }
                        }
                        filestream.Close(); //zamknięcie strumienia pliku
                    }
                }
            }
            catch (Exception x)
            {                
                MW.tbl_ControlPanelOperation.Text = "Błąd pobierania";
                MW.Dispatcher.Invoke( delegate { MessageBox.Show(x.ToString()); });
            }            
        }

        public static void SendFile(FileInformation _file)
        {
            int receive_bytes, buffer = Int32.Parse(MW.cmb_ConfigurationBuffer.Text);
            byte[] data = new byte[buffer];
            string filename = _file.GetFullFileName();
            string fileaddress = _file.filepath;
            long size = _file.filesize;
            long steps = (size / buffer) + 1;
            double steps_counter = 1;
            double step_to_percent = steps / 100;
            int percent;
            int progress = 0;

            if (steps < 100)
            {
                percent = 100 / (int)steps;
            }
            else
            {
                percent = 1;
            }

            NS.Flush();
            try
            {
                data = Encoding.ASCII.GetBytes("startsending");
                NS.Write(data, 0, data.Length);
                NS.Flush(); //zwolnienie strumienia

                data = new byte[buffer];
                receive_bytes = NS.Read(data, 0, data.Length);

                if ("ready" == Encoding.ASCII.GetString(data, 0, receive_bytes))
                {
                    data = new byte[buffer]; //ustawienie rozmiaru bufera
                    data = Encoding.UTF8.GetBytes(filename); //zakodowanie nazwy pliku
                    NS.Write(data, 0, data.Length); //wysłanie nazwy pliku 
                    NS.Flush(); //zwolnienie strumienia
                                            
                    data = new byte[buffer];
                    receive_bytes = NS.Read(data, 0, data.Length);

                    if ("ready" == Encoding.ASCII.GetString(data, 0, receive_bytes))
                    {

                        MW.rpb_ControlPanelProgressBar.Dispatcher.Invoke(() => MW.rpb_ControlPanelProgressBar.Value = 0, System.Windows.Threading.DispatcherPriority.Background);
                        MW.tbl_ControlPanelProgressValue.Text = "0 %";
                        MW.tbl_ControlPanelOperation.Text = "Wysyłanie pliku";

                        data = new byte[buffer]; //ustawienie rozmiaru bufera
                        using (var s = File.OpenRead(fileaddress)) //dopoki plik jest otwarty
                        {
                            int actually_read; //zmienna pomocnicza do odczytu rozmiaru
                            while ((actually_read = s.Read(data, 0, buffer)) > 0) //dopóki w pliku sa dane
                            {
                                NS.Write(data, 0, actually_read); //wyslanie danych z pliku  
                                if (step_to_percent - steps_counter <= 0)
                                {
                                    progress += percent;
                                    MW.rpb_ControlPanelProgressBar.Dispatcher.Invoke(() => MW.rpb_ControlPanelProgressBar.Value = progress, System.Windows.Threading.DispatcherPriority.Background);
                                    MW.tbl_ControlPanelProgressValue.Text = progress.ToString() + " %";
                                    steps_counter -= step_to_percent;
                                }
                                steps_counter++;
                            }
                            NS.Flush(); //zwolnienie strumienia                         
                        }
                    }
                    Thread.Sleep(250);

                    data = new byte[buffer]; //ustawienie rozmiaru bufera
                    data = Encoding.ASCII.GetBytes("endsending");
                    NS.Write(data, 0, data.Length); //wysłanie nazwy pliku 
                    NS.Flush(); //zwolnienie strumienia  

                    MW.rpb_ControlPanelProgressBar.Dispatcher.Invoke(() => MW.rpb_ControlPanelProgressBar.Value = 100, System.Windows.Threading.DispatcherPriority.Background);
                    MW.tbl_ControlPanelProgressValue.Text = "100 %";
                    MW.tbl_ControlPanelOperation.Text = "Wysłano plik";
                }
            }
            catch (Exception)
            {
                MW.tbl_ControlPanelOperation.Text = "Błąd wysyłania";
            }
        }
    }
}
