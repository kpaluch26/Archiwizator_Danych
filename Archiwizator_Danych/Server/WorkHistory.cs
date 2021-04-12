using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class WorkHistory
    {
        private string history_time;
        private string activity_name;

        public string History
        {
            get
            {
                return history_time;
            }
        }

        public string Activity
        {
            get
            {
                return activity_name;
            }
        }

        public void ClientConnectionStart(string _client)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "Użytkownik " + _client + " połączył się z serwerem.";
        }

        public void ClientConnectionEnd(string _client)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "Użytkownik " + _client + " rozłączył się z serwerem.";
        }

        public void ClientReceiveStart(string _client, string _file)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "Użytkownik " + _client + " rozpoczął transfer pliku o nazwie: " + _file + ".";
        }

        public void ClientReceiveEnd(string _client, string _file)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "Użytkownik " + _client + " zakończył transfer pliku o nazwie: " + _file + ".";
        }

        public void ClientReceiveError(string _client, string _file)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "BŁĄD! Podczas transferu pliku: " + _file + " wystąpił błąd. Plik użytkownika " + _client + " nie jest kompletny.";
        }

        public void ClientSendStart(string _client, string _file)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "Rozpoczęto transfer pliku: " + _file + " do użytkownika " + _client + ".";
        }

        public void ClientSendEnd(string _client, string _file)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "Zakończono transfer pliku: " + _file + " do użytkownika " + _client + ".";
        }

        public void ClientSendError(string _client, string _file)
        {
            history_time = DateTime.Now.ToString();
            activity_name = "BŁĄD! Podczas transferus pliku: " + _file + " wystąpił błąd. Użytkownik " + _client + " nie otrzymał kompletu danych.";
        }
    }
}
