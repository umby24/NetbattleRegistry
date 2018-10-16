using System;
using System.Net;

namespace Netbattle_Registry.Common {
    public static class NbMethods {
        /// <summary>
        /// Converts a number into a hex string, padding or removing bytes to match a specific length.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string FixedHex(int number, int length) {
            string hexed = Convert.ToString(number, 16); // -- Convert the number to base-16

            if (hexed.Length == length)
                return hexed;

            if (hexed.Length <= length)
                return hexed.PadLeft(length, '0'); // -- length > hexed.length

            int diff = hexed.Length - length;
            hexed = hexed.Substring(diff, hexed.Length - diff);
            return hexed;
        }

        /// <summary>
        /// Converts an IP address into hex.
        /// </summary>
        /// <param name="ip">IPv4 Address in format x.x.x.x</param>
        /// <returns>IP Address converted to a hexadecimal string</returns>
        public static string PackIp(string ip) {
            if (!IPAddress.TryParse(ip, out IPAddress _))
                throw new ArgumentException("Invalid IP Address provided.");
            // -- Break the IP up into it's individual blocks
            string first = ip.Substring(0, ip.IndexOf("."));
            ip = ip.Substring(first.Length + 1);

            string second = ip.Substring(0, ip.IndexOf("."));
            ip = ip.Substring(second.Length + 1);

            string third = ip.Substring(0, ip.IndexOf("."));
            ip = ip.Substring(third.Length + 1);

            // -- Convert each part into hex
            first = FixedHex(int.Parse(first), 2);
            second = FixedHex(int.Parse(second), 2);
            third = FixedHex(int.Parse(third), 2);
            ip = FixedHex(int.Parse(ip), 2);
            
            // -- Put it all together
            return $"{first}{second}{third}{ip}";
        }

        /// <summary>
        /// Reverses the PackIp Method, converting a Hex IP address into it's standard long form.
        /// </summary>
        /// <param name="ip">Hex packed IP Address</param>
        /// <returns>Standard ipv4 address in format x.x.x.x</returns>
        public static string UnpackIp(string ip) {
            int first = Convert.ToInt32(ip.Substring(0, 2), 16);
            int second = Convert.ToInt32(ip.Substring(2, 2), 16);
            int third = Convert.ToInt32(ip.Substring(4, 2), 16);
            int forth = Convert.ToInt32(ip.Substring(6, 2), 16);

            return $"{first}.{second}.{third}.{forth}";
        }

        /// <summary>
        /// Converts a byte array into a string representation of the individual bits in that array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string BytesToBinary(byte[] bytes) {
            var build = "";

            foreach (byte b in bytes) {
                string chr = Convert.ToString(b, 2);
                chr = chr.PadLeft(8, '0');
                build += chr;
            }

            return build;
        }

        /// <summary>
        /// A Recreation of Netbattle's 'Decompress SID' method. Takes 13 bytes, and produces the 21-byte system ID.
        /// </summary>
        /// <param name="sidBytes"></param>
        public static string DecompressSid(byte[] sidBytes) {
            string asBinary = BytesToBinary(sidBytes).Substring(0, 100); // -- Breaks the bytes down to their individual bits, in a full string.
            var result = "";
            var temp = "";
            for (var i = 1; i < 6; i++) { // -- Takes every 20th bit, and places it at the front of the build order..
                temp += asBinary.Substring((i * 20) - 1, 1);
            }
            asBinary = temp + asBinary;
            for (var i = 1; i < 22; i++) { // -- Takes each 5 bit set, treating them as their own values.
                string sub = asBinary.Substring((i * 5) - 5, 5);
                int val = Convert.ToInt32(sub, 2); // -- Converts them to a value..
                val += (val > 8 ? 56 : 49); // -- Adds either 56 or 49 to them.
                result += (char)val; // -- and that's the byte value of the character!
            }

            return result;
        }
    }
}
