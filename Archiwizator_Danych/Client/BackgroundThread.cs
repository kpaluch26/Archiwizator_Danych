using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class BackgroundThread
    {
        private static BackgroundWorker m_oBackgroundWorker = null;
        private static TcpClient client;
        private static NetworkStream ns;
        private static int buffer_size;
        private static MainWindow MW;
        public static void CreateBackgroundThread(MainWindow _mw, TcpClient _client, NetworkStream _ns, int _port , int _buffer)
        {
            MW = _mw;
            client = _client;
            ns = _ns;
            buffer_size = _buffer;

            if (m_oBackgroundWorker == null) //sprawdzanie czy obiekt istnieje
            {
                m_oBackgroundWorker = new BackgroundWorker(); //utworzenie obiektu
                m_oBackgroundWorker.DoWork += new DoWorkEventHandler(m_oBackgroundWorker_DoWork); //utworzenie uchwyta dla obiektu
            }
            m_oBackgroundWorker.RunWorkerAsync(_port); //start wątka roboczego w tle
        }

        private static void m_oBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool do_work = true; //zmienna określające prace wątka w tle

            while (do_work)
            {
                try
                {
                    if (client == null)
                    {
                        ns.Close();
                        throw new Exception();
                    }
                    else
                    {
                        int dec_data;                        
                        byte[] data = new byte[buffer_size]; //ustawienie rozmiaru bufera
                        try
                        {
                            while (!client.Client.Poll(0, SelectMode.SelectRead))
                            {
                                dec_data = ns.Read(data, 0, data.Length);
                                string receive = System.Text.Encoding.ASCII.GetString(data, 0, dec_data);
                                if (receive == "startsending")
                                {
                                    //this.Invoke(new MethodInvoker(delegate { ReceiveFileEnable(stream); }));
                                }
                            }
                            client.Client.ReceiveTimeout = 3000;
                            if (!client.Client.Poll(0, SelectMode.SelectRead))
                            {
                                client.Client.ReceiveTimeout = 0;
                            }
                            else
                            {
                                throw new IOException();
                            }
                        }
                        catch (IOException)
                        {
                            ns.Close();
                            if (client != null)
                            {
                                MW.Dispatcher.Invoke(delegate { ConnectionEnd.TryDisconnect(MW);} );
                                MW = null;
                            }
                            else
                                throw new Exception();
                        }
                    }
                }
                catch
                {
                    do_work = false;
                    return;
                }
            }
        }
    }
}
