using System;
using System.IO;

namespace Client
{
    class FileToSend
    {
        private static FileInformation file_to_send;
        public static FileInformation FileToSendSet(string _path)
        {
            FileInfo _file_to_send = new FileInfo(_path);

            if (file_to_send != null)
            {
                file_to_send.filename = Path.GetFileNameWithoutExtension(_file_to_send.Name);
                file_to_send.filepath = _path;
                file_to_send.filetype = _file_to_send.Extension;
                file_to_send.filesize = _file_to_send.Length;
                file_to_send.is_checked = true;
            }
            else
            {
                file_to_send = new FileInformation()
                {
                    filename = Path.GetFileNameWithoutExtension(_file_to_send.Name),
                    filepath = _path,
                    filetype = _file_to_send.Extension,
                    filesize = _file_to_send.Length,
                    is_checked = true
                };
            }
            return file_to_send;
        }

        public static string FormatSize(Int64 bytes)
        {
            string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }
    }
}
