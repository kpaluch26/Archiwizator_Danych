using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    class UserConfiguration
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string group { get; set; }
        public string section { get; set; }
        public string version { get; set; }

        public bool state { get; set; }

        public string folderpath { get; set; }

        public override string ToString()
        {
            return $"{firstname} {lastname} {group}{section}{version}";
        }
    }
}
