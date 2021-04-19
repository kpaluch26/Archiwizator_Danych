using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class BackgroundThread
    {
        private static BackgroundWorker m_oBackgroundWorker = null;
        private static ServerConfiguration config;
        public static void ConnectionListen(ServerConfiguration _config)
        {
            if (m_oBackgroundWorker == null) //sprawdzanie czy obiekt istnieje
            {
                m_oBackgroundWorker = new BackgroundWorker(); //utworzenie obiektu
                m_oBackgroundWorker.WorkerSupportsCancellation = true; //włączenie możliwości przerwania pracy wątka roboczego
                m_oBackgroundWorker.DoWork += new DoWorkEventHandler(m_oBackgroundWorker_DoWork); //utworzenie uchwyta dla obiektu
            }
            if (_config != null)
            {
                config = _config;
                m_oBackgroundWorker.RunWorkerAsync(config.GetPort()); //start wątka roboczego w tle
            }
        }
        private static void m_oBackgroundWorker_DoWork(object sender, DoWorkEventArgs e) //funkcja odpowiadająca za pracę wątka roboczego w tle
        {
            TcpListener listener = new TcpListener(IPAddress.Any, config.GetPort()); //ustawienie nasłuchiwania na porcie z konfiguracji i dla dowolnego adresu IP
            TcpClient client = null; //utworzenie pustego klienta
            listener.Start(); //rozpoczęcie nasłuchiwania     
            bool do_work = true; //zmienna określające prace wątka w tle

            while (do_work)
            {
                if (ServerOptions.server_option == ServerOptions.Options.server_listen) //działa jesli tryb pracy serwera jest ustawiony na oczekiwanie
                {
                    if (listener.Pending()) //jeśli jakieś zapytanie przychodzi
                    {
                        client = listener.AcceptTcpClient(); //zaakceptowanie przychodzącego połączenia                           
                        ThreadPool.QueueUserWorkItem(TransferThread.ConnectionManager,client); //Dodanie do kolejki klienta
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

        public static void BackgroundWorkerClose() //funkcja do przerywania wątka w tle
        {
            if (m_oBackgroundWorker != null) //jeśli obiekt istnieje
            {
                if (m_oBackgroundWorker.IsBusy) //sprawdzanie czy taki wątek istnieje
                {
                    m_oBackgroundWorker.CancelAsync();//przerwanie wątka roboczego
                }
            }
        }
    }
}
