using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class ConnectionStart
    {
        private static TcpClient client = null;
        private static NetworkStream ns = null;

        public static bool TryConnect(UserConfiguration _user, MainWindow _mw)
        {
            string IP_text, PORT_text;
            IPAddress IP_address;
            int PORT_number = 0;
            int buffer_size = 0;

            if (_user.state == true && _mw.tbl_ConfigurationSavePath.Text!="")
            {
                IP_text = _mw.txt_ConfigurationIP.Text.Trim();
                PORT_text = _mw.txt_ConfigurationPort.Text.Trim();
                buffer_size = Int32.Parse(_mw.cmb_ConfigurationBuffer.Text);

                try
                {
                    bool ValidateIP = IPAddress.TryParse(IP_text, out IP_address);
                    bool ValidatePORT = Int32.TryParse(PORT_text, out PORT_number);

                    if (ValidateIP && ValidatePORT)
                    {
                        client = new TcpClient(IP_address.ToString(), PORT_number);
                    }
                    else if (!ValidateIP && !ValidatePORT)
                    {
                        _mw.tbl_ConfigurationAllert.Text = "UWAGA! Błędny format wprowadzonych danych, spróbuj ponownie.";
                        throw new FormatException();
                    }
                    else if (!ValidateIP && ValidatePORT)
                    {
                        _mw.tbl_ConfigurationAllert.Text = "UWAGA! Wprowadzone zły adres IP, spróbuj ponownie.";
                        throw new FormatException();
                    }
                    else
                    {
                        _mw.tbl_ConfigurationAllert.Text = "UWAGA! Wprowadzone zły numer portu, spróbuj ponownie.";
                        throw new FormatException();
                    }

                    ns = client.GetStream();
                    byte[] bytesToSend = ASCIIEncoding.UTF8.GetBytes(_user.ToString());
                    ns.Write(bytesToSend, 0, bytesToSend.Length);
                    ns.Flush();

                    BackgroundThread.CreateBackgroundThread(_mw, client, ns, PORT_number, buffer_size);
                    ConnectionTransfer.SetConnection(_mw, ns);
                    ConnectionEnd.SetClient(client);

                    return true;
                }
                catch (SocketException)
                {
                    _mw.tbl_ConfigurationAllert.Text = "UWAGA! Serwer odmawia nawiązania połączenia. Wprowadzono błędne dane serwera lub serwer pracuje w trybie uniemożliwiającym nawiązanie połączenia.";
                    return false;
                }
                catch (FormatException)
                {                    
                    return false;
                }
                catch
                {
                    _mw.tbl_ConfigurationAllert.Text = "UWAGA! Przy próbie połączenia z serwerem wystąpił błąd.";
                    return false;
                }
            }
            else
            {
                _mw.tbl_ConfigurationAllert.Text = "UWAGA! Nie wprowadzono wszystkich danych użytkownika lub nie wybrano miejsca zapisu.";
                return false;
            }
        }
    }
}
