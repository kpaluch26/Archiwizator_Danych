using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class ServerConfiguration
    {
        private string username;
        private string hostname;
        private string ip_address;
        private string archive_address;
        private int port;
        private int buffer_size;

        public ServerConfiguration(string u, string h, string i, string a, int p, int b)
        {
            this.username = u;
            this.ip_address = i;
            this.port = p;
            this.buffer_size = b;
            this.hostname = h;
            this.archive_address = a;
        }

        public int GetPort()
        {
            return this.port;
        }

        public int GetBufferSize()
        {
            return this.buffer_size;
        }

        public string GetUserName()
        {
            return this.username;
        }

        public string GetHostName()
        {
            return this.hostname;
        }

        public string GetIPAddress()
        {
            return this.ip_address;
        }

        public string GetArchiveAddress()
        {
            return this.archive_address;
        }

        public string SetArchiveAddress(string a)
        {
            this.archive_address = a;
            return this.archive_address;
        }

        public void SetIPAddress(string ip)
        {
            this.ip_address = ip;
        }

    }
}
