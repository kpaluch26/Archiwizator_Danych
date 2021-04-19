using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class ServerOptions
    {
        public static Options server_option { get; set; }
        public enum Options
        {
            server_stop,
            server_listen,
            server_receive,
            server_send
        }       
    }
}
