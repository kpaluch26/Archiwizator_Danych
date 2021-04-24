using System;

namespace Server
{
    public class FileInformation 
    {
        public string filename { get; set; }
        public string filepath { get; set; }
        public string filetype { get; set; }
        public long filesize  { get; set; }
        public bool is_checked { get; set; }

        public string GetFullFileName()
        {
            return filename + filetype;
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
