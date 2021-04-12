using System.Threading;
using System.Windows.Input;

namespace Server
{
    public class ConnectionThread
    {
        private string clientname;
        private string ipaddress;
        private CancellationTokenSource canceltoken;
        private ICommand _DisconnectClient;

        public ConnectionThread(string _clientname, string _ipaddress, CancellationTokenSource _cts)
        {
            this.clientname = _clientname;
            this.ipaddress = _ipaddress;
            this.canceltoken = _cts;
        }

        public string ClientName
        {
            get { return this.clientname; }
        }

        public string IpAddress
        {
            get { return this.ipaddress; }
        }

        public ICommand DisconnectClient
        {
            get
            {
               if (_DisconnectClient == null)
                {
                    _DisconnectClient = new RelayCommand(
                    param => this.Execute(),
                    param => this.CanExecute()
                );
                }

                return _DisconnectClient;
            }
        }

        private void Execute()
        {
            this.canceltoken.Cancel();
            this.canceltoken.Token.WaitHandle.WaitOne();
            this.canceltoken.Dispose();
            this.canceltoken = null;
        }

        private bool CanExecute()
        {
            if (canceltoken == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }
}
