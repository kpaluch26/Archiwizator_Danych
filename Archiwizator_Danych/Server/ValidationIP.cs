using System;
using System.Linq;

namespace Server
{
    class ValidationIP
    {
        public static bool ValidateIPAddress(string new_ip)
        {
            if (String.IsNullOrWhiteSpace(new_ip))
            {
                return false;
            }

            string[] splitValues = new_ip.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;
            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }
    }
}
