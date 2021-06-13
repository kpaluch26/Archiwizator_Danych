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
                if (!m_oBackgroundWorker.IsBusy)
                {
                    m_oBackgroundWorker.RunWorkerAsync(config.GetPort()); //start wątka roboczego w tle
                }
            }
        }
        private static void m_oBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, config.GetPort());  
            TcpClient client = null; 
            listener.Start();     
            bool do_work = true; 

            while (do_work)
            {
                if (ServerOptions.server_option == ServerOptions.Options.server_listen) 
                {
                    if (listener.Pending()) 
                    {
                        client = listener.AcceptTcpClient();                         
                        ThreadPool.QueueUserWorkItem(TransferThread.ConnectionManager,client); 
                    }
                }
                if (m_oBackgroundWorker.CancellationPending) 
                {
                    listener.Stop(); 
                    e.Cancel = true; 
                    do_work = false; 
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
