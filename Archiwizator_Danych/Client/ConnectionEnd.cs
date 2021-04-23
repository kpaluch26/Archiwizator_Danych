using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace Client
{
    class ConnectionEnd
    {
        private static TcpClient client;

        public static void TryDisconnect(MainWindow _mw)
        {
            client.Close();

            _mw.btn_ConfigurationDisconnect.IsEnabled = false;
            _mw.btn_ConfigurationDisconnect.Visibility = Visibility.Collapsed;

            _mw.btn_ConfigurationSave.IsEnabled = true;

            _mw.btn_ConfigurationConnect.Visibility = Visibility.Visible;
            _mw.btn_ConfigurationConnect.IsEnabled = true;
        }

        public static void SetClient(TcpClient _client)
        {
            client = _client;
        }
    }
}
