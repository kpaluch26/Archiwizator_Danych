using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class TransferThread
    {
        private static ServerConfiguration config;
        private static MainWindow MW;
        private static FileInformation file_to_send;
        private static object client_list_locker = new Object();
        private static int active_clients = 0;
        private static int sending_end = 0;
        private static ObservableCollection<ConnectionThread> client_list = new ObservableCollection<ConnectionThread>();
        private static ObservableCollection<WorkHistory> history_list = new ObservableCollection<WorkHistory>();

        public static void SetLists(MainWindow _mw)
        {
            history_list.Clear();
            client_list.Clear();
            _mw.dgr_UsersPanelHistory.ItemsSource = history_list;
            _mw.dgr_UsersPanelConnectedUsers.ItemsSource = client_list;
        }

        public static void SetUp(ServerConfiguration _config, MainWindow _mw)
        {
            if (client_list.Count == 0)
            {
                config = _config;
                MW = _mw;
            }            
        }

        public static void SetUp(FileInformation _fi)
        {
            file_to_send = _fi;
        }

        public static void ConnectionManager(object obj)
        {            
            CancellationTokenSource canceltoken = new CancellationTokenSource();
            TcpClient client = (TcpClient)obj; //przejęcie kontroli nad klientem
            NetworkStream stream = null; //utworzenie kanału do odbioru            
            string client_hostname = "", client_ip = "", client_filename = ""; //zmienne do identyfikacji połączenia
            int buffer_size = config.GetBufferSize(); //wczytanie rozmiaru buffera z konfiguracji
            int receive_bytes; //zmienna do odbierania plików
            byte[] data = new byte[buffer_size]; //ustawienie rozmiaru bufera
            bool help = true; //zmienna omocnicza określająca czy nowy klient się podłączył 
            System.Text.Decoder decoder = System.Text.Encoding.UTF8.GetDecoder(); //zmienna pomocnicza do odkodowania nazwy pliku
            
            while (!canceltoken.IsCancellationRequested)
            {
                try
                {
                    while (client.Connected)
                    {
                        if (ServerOptions.server_option != ServerOptions.Options.server_listen && help)
                        {
                            client.Close();
                        }
                        else if (ServerOptions.server_option == ServerOptions.Options.server_listen && help)
                        {
                            Monitor.Enter(client_list_locker);                           
                            stream = client.GetStream(); //określenie rodzaju połączenia na odbiór danych
                            int dec_data = stream.Read(data, 0, data.Length);//oczekiwanie na nazwę klienta       
                            char[] chars = new char[dec_data]; //zmienna pomocnicza do odkodowania nazwy klienta
                            decoder.GetChars(data, 0, dec_data, chars, 0); //dekodowanie otrzymanej nazwy klienta
                            client_hostname = new System.String(chars); //przypisanie odkodowanej nazwy do nowej zmiennej
                            client_ip = client.Client.RemoteEndPoint.ToString();

                            WorkHistory worker = new WorkHistory();
                            worker.ClientConnectionStart(client_hostname);
                            MW.Dispatcher.Invoke(delegate { history_list.Add(worker); });                                        
                            
                            ConnectionThread _ct = new ConnectionThread(client_hostname, client_ip, canceltoken);
                            MW.Dispatcher.Invoke(delegate { client_list.Add(_ct); });

                            MW.Dispatcher.Invoke(delegate { updateCounterOfActiveUsers(true); });                            
                            Monitor.Exit(client_list_locker);
                            help = false; //wyłączenie właściwości nowego klienta   
                        }
                        else if (ServerOptions.server_option == ServerOptions.Options.server_receive && !help)
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
                                    stream.Flush(); //zwolnienie strumienia
                                    stream.Write(data, 0, data.Length);
                                    data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                    receive_bytes = stream.Read(data, 0, data.Length);
                                    client_filename = System.Text.Encoding.UTF8.GetString(data, 0, receive_bytes);

                                    if (client_filename != null && client_filename != "")
                                    {
                                        data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                        data = System.Text.Encoding.ASCII.GetBytes("ready");
                                        stream.Write(data, 0, data.Length);
                                        stream.Flush(); //zwolnienie strumienia
                                        stream.Write(data, 0, data.Length);

                                        Monitor.Enter(client_list_locker);
                                        WorkHistory worker = new WorkHistory();
                                        worker.ClientReceiveStart(client_hostname, client_filename);
                                        MW.Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                        Monitor.Exit(client_list_locker);

                                        FileStream filestream = new FileStream(config.GetArchiveAddress() + @"\" + client_filename, FileMode.OpenOrCreate, FileAccess.Write); //utworzenie pliku do zapisu archiwum                              
                                        data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                                        stream.ReadTimeout=3000;

                                        while (!end_stream)
                                        {                                           
                                            try
                                            {
                                                receive_bytes = stream.Read(data, 0, data.Length);
                                                string end_transfer = System.Text.Encoding.ASCII.GetString(data, 0, receive_bytes);

                                                if (receive_bytes >= 10 && end_transfer.Remove(0, (receive_bytes - 10)) == "endsending")
                                                {
                                                    if (end_transfer.Length <= 10)
                                                    {
                                                        end_stream = true;
                                                    }
                                                    else
                                                    {
                                                        filestream.Write(data, 0, (receive_bytes - 10)); //kopiowanie danych do pliku
                                                        end_stream = true;
                                                    }
                                                }
                                                else
                                                {
                                                    filestream.Write(data, 0, receive_bytes); //kopiowanie danych do pliku
                                                }
                                            }
                                            catch
                                            {
                                                if (client.Client.Receive(buff, SocketFlags.Peek) == 0) //jeśli nagle przestał odpowiadać
                                                {
                                                    WorkHistory worker1 = new WorkHistory();
                                                    worker1.ClientReceiveError(client_hostname, client_filename);
                                                    MW.Dispatcher.Invoke(delegate { history_list.Add(worker1); });
                                                    throw new SocketException();
                                                    end_stream = true;
                                                }
                                            }
                                        }
                                        filestream.Close(); //zamknięcie strumienia pliku 
                                        stream.ReadTimeout = Timeout.Infinite;

                                        if (end_stream)
                                        {
                                            Monitor.Enter(client_list_locker);
                                            WorkHistory worker1 = new WorkHistory();
                                            worker1.ClientReceiveEnd(client_hostname, client_filename);
                                            MW.Dispatcher.Invoke(delegate { history_list.Add(worker1); });
                                            Monitor.Exit(client_list_locker);
                                        }
                                        else
                                        {
                                            Monitor.Enter(client_list_locker);
                                            WorkHistory worker1 = new WorkHistory();
                                            worker1.ClientReceiveError(client_hostname, client_filename);
                                            MW.Dispatcher.Invoke(delegate { history_list.Add(worker1); });
                                            Monitor.Exit(client_list_locker);
                                            throw new SocketException();
                                        }
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
                            {
                                if (canceltoken.IsCancellationRequested)
                                {
                                    client.Client.Disconnect(true); //rozłącz klienta                            
                                    help = true;
                                }
                            }
                        }
                        else if (ServerOptions.server_option == ServerOptions.Options.server_send && !help)
                        {
                            if (file_to_send != null) //jeśli wybrany plik istnieje
                            {
                                string filename = file_to_send.GetFullFileName();
                                int sending_start = active_clients;
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
                                        data = System.Text.Encoding.UTF8.GetBytes(filename); //zakodowanie nazwy pliku
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
                                                Monitor.Enter(client_list_locker);
                                                WorkHistory worker = new WorkHistory();
                                                worker.ClientSendStart(client_hostname, filename);
                                                MW.Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                                Monitor.Exit(client_list_locker);

                                                data = new byte[buffer_size]; //ustawienie rozmiaru bufera
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

                                                Monitor.Enter(client_list_locker);
                                                WorkHistory worker1 = new WorkHistory();
                                                worker1.ClientSendEnd(client_hostname, filename);
                                                MW.Dispatcher.Invoke(delegate { history_list.Add(worker1); });
                                                Monitor.Exit(client_list_locker);
                                            }
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    stream.Flush();

                                    Monitor.Enter(client_list_locker);
                                    WorkHistory worker = new WorkHistory();
                                    worker.ClientSendError(client_hostname, client_filename);
                                    MW.Dispatcher.Invoke(delegate { history_list.Add(worker); });
                                    Monitor.Exit(client_list_locker);

                                    throw new SocketException();
                                }
                                finally
                                {
                                    Monitor.Enter(client_list_locker);
                                    sending_end++;
                                    if (sending_start == sending_end)
                                    {
                                        ServerOptions.server_option = ServerOptions.Options.server_stop;
                                        MW.Dispatcher.Invoke(delegate { MW.tbl_ControlPanelServer.Text = "Wysłano pliki do wszystkich użytkowników."; }); //aktualizacja aktywnych użytkowników
                                        sending_end = 0;
                                    }
                                    else
                                    {
                                        ServerOptions.server_option = ServerOptions.Options.server_stop;
                                        MW.Dispatcher.Invoke(delegate { MW.tbl_ControlPanelServer.Text = "Wysłano pliki do " + sending_end.ToString() + "/" + sending_start.ToString() + " użytkowników."; }); //aktualizacja aktywnych użytkowników
                                    }
                                    Monitor.Exit(client_list_locker);
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
                    MW.Dispatcher.Invoke(delegate { updateCounterOfActiveUsers(false); }); //aktualizacja aktywnych użytkowników
                    if (!help)
                    {
                        canceltoken.Cancel();
                        canceltoken.Dispose();
                    }
                    MW.Dispatcher.Invoke(delegate { deleteUserFromList(client_ip); });

                    Monitor.Enter(client_list_locker);
                    WorkHistory worker_end = new WorkHistory();
                    worker_end.ClientConnectionEnd(client_hostname);
                    MW.Dispatcher.Invoke(delegate { history_list.Add(worker_end); });
                    Monitor.Exit(client_list_locker);
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
                        MW.Dispatcher.Invoke(delegate { updateCounterOfActiveUsers(false); }); //aktualizacja aktywnych użytkowników

                        if (!help)
                        {
                            canceltoken.Cancel();
                            canceltoken.Dispose();
                        }

                        MW.Dispatcher.Invoke(delegate { deleteUserFromList(client_ip); });

                        Monitor.Enter(client_list_locker);
                        WorkHistory worker = new WorkHistory();
                        worker.ClientConnectionEnd(client_hostname);
                        MW.Dispatcher.Invoke(delegate { history_list.Add(worker); });
                        Monitor.Exit(client_list_locker);
                    }
                }
                catch (IOException)
                {
                    client.Close(); //zamknięcie klienta
                    MW.Dispatcher.Invoke(delegate { updateCounterOfActiveUsers(false); }); //aktualizacja aktywnych użytkowników

                    if (!help)
                    {
                        canceltoken.Cancel();
                        canceltoken.Dispose();
                    }

                    MW.Dispatcher.Invoke(delegate { deleteUserFromList(client_ip); });
                }
            }
        }

        private static void deleteUserFromList(string _ip)
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

        private static void updateCounterOfActiveUsers(bool x) //funkcja do aktualizowania aktywnych połączeń
        {
            if (x) //jeśli true
            {
                active_clients++; //zwiększenie listy aktywnych klientów
                MW.tbl_ControlPanelUsercCounters.Text = active_clients.ToString() + " / 20";
                MW.rpb_ControlPanelUsersCounter.Value = (active_clients * 5);
            }
            else
            {
                active_clients--; //zmniejszenie liczby aktywnych klientów
                MW.tbl_ControlPanelUsercCounters.Text = active_clients.ToString() + " / 20";
                if (active_clients != 0)
                {
                    MW.rpb_ControlPanelUsersCounter.Value = (active_clients * 5);
                }
                else
                {
                    MW.rpb_ControlPanelUsersCounter.Value = 0;
                }
            }
        }
    }
}
