using System;
using System.Linq;

namespace Netbattle_Registry.Common {
    public static class XorModule {
        public static byte[] XorEncrypt(byte[] input) {
            if (input.Length % 2 == 1) {
                input = new byte[] { 0 }.Concat(input).ToArray(); // -- Add a null byte to the beginning if this string is an odd length.
            }

            // -- Convert the input string into a byte array.
            int iLen = input.Length; // -- Input length.

            var iBytes = new byte[input.Length];//input.Select(c => (byte)c).ToArray();
            input.CopyTo(iBytes, 0);

            int pLen = iLen + 8; // -- The end result has 8 extra bytes tacked on for flags, storing checksums, and the encryption key. (In NB 9.6, this is only 4 bytes)

            var oByte = new byte[pLen]; // -- Holder for the output.
            Array.Copy(iBytes, oByte, iBytes.Length);

            // -- First, we need to generate a key for the encryption.
            int mainKey = (new Random().Next(0, 255));
            oByte[pLen - 1] = (byte)(mainKey ^ 12);

            // -- Main Encryption cycle.
            int counter = iLen - 1;

            // -- XOR's each byte against the byte in front of it, and the main key.
            // -- Notice only for the iLen though! (original string length is encrypted string length - 8, for encryption flags stored at the end.

            for (var x = 1; x < iLen + 1; x++) {
                oByte[x - 1] = (byte)(oByte[x - 1] ^ oByte[counter] ^ mainKey);
                counter--;
            }

            // -- Generate some checksums to ensure message integrity and decryption correctness.
            var check1 = 0;
            var check2 = 0;
            var check3 = 0;

            // -- The first checksum adds together each unencrypted byte xor'd against the one in front of it (excluding the last byte).
            for (var i = 0; i < iLen - 1; i++) {
                check1 += (iBytes[i] ^ iBytes[i + 1]);
            }
            check1 = check1 % 256;
            oByte[pLen - 4] = (byte)check1;

            // -- The second takes every encrypted byte, and xor's it against (255 - index), where index is where the byte is located.
            for (var i = 0; i < pLen - 3; i++) {
                check2 += (oByte[i] ^ (255 - i));
            }
            check2 = (check2 + mainKey) % 256;

            int v = pLen / 8;

            for (var x = 1; x < v + 1; x++) {
                int working = x;
                for (var y = 0; y < 4; y++) {
                    int z = y * v + x;

                    if (z != pLen - 1) {

                        working = working + ((y % 2 == 1 ? 8 : (6 * oByte[z - 1]) + 8));
                    }
                }
                check3 += working;
            }
            check3 = Math.Abs((check3 + (oByte[(mainKey % iLen)]))) % 256;

            // -- Assign checksums and encryption key..

            oByte[pLen - 3] = (byte)check2;
            oByte[pLen - 2] = (byte)check3;


            // -- Now for extra screwery, the bytes are 'sifted' twice.
            // -- Takes the odd-numbered indices and applies it to the first half of the array
            // -- Takes the even-numbered indices adn applies it to the second half of the array, reversed.
            // -- Runs twice on Vanilla NB.
            for (var i = 0; i < 2; i++) {
                var y = 0;

                var temp = new byte[oByte.Length]; // -- For work as we scramble it.
                oByte.CopyTo(temp, 0);

                for (var x = 1; x < pLen; x += 2) {
                    y++;
                    Array.Copy(temp, y - 1, oByte, x - 1, 1);
                }

                y = pLen + 1;
                for (var x = 2; x < pLen + 1; x += 2) {
                    y--;
                    //oByte[(y - 1)] = iByte[(x - 1)];
                    Array.Copy(temp, y - 1, oByte, x - 1, 1);
                }
            }

            // -- For yet another trick, we do a split and reverse on the bytes..
            oByte = ReverseBytes(oByte);
            // -- Now reform it into a string or keep it as bytes, your choice :)
            return oByte;
        }

        public static byte[] XorDecrypt(byte[] encrypted) {
            var oByte = new byte[encrypted.Length];
            int pLen = encrypted.Length;
            int iLen = pLen - 8;

            byte[] iByte = ReverseBytes(encrypted);

            // -- De-Scramble the string.
            // -- Takes the odd-numbered indecies and applies it to the first half of the array
            // -- Takes the even-numbered indices adn applies it to the second half of the array, reversed.
            // -- Runs twice on Vanilla NB.
            for (var i = 0; i < 2; i++) {
                var y = 0;

                for (var x = 1; x < pLen; x += 2) {
                    y++;
                    Array.Copy(iByte, x - 1, oByte, y - 1, 1);
                }

                y = pLen + 1;
                for (var x = 2; x < pLen + 1; x += 2) {
                    y--;
                    Array.Copy(iByte, x - 1, oByte, y - 1, 1);
                }
                Array.Copy(oByte, iByte, oByte.Length);
            }

            // -- Pull the main XOR Key (stored at the end of the encrypted byte array)
            int mainKey = oByte[pLen - 1] ^ 12;

            // -- Main Decryption..
            var counter = 0;

            // -- XOR's each byte against the byte in front of it, and the main key.
            // -- Notice only for the iLen though! (original string length is encrypted string length - 8, for encryption flags stored at the end.

            for (int x = iLen; x > 0; x--) {
                oByte[x - 1] = (byte)(oByte[x - 1] ^ oByte[counter] ^ mainKey);// (byte) (oByte[potato] ^ oByte[x] ^ mainKey);
                counter++;
            }

            // -- Checksums to verify decryption occured successfully.
            // -- The proper checksum values are stored at the end of the byte array along with the decryption key.

            var check1 = 0;
            var check2 = 0;
            var check3 = 0;

            // -- The first checksum adds together each unencrypted byte xor'd against the one in front of it (excluding the last byte).
            for (var i = 0; i < iLen - 1; i++) {
                check1 += (oByte[i] ^ oByte[i + 1]);
            }
            check1 = check1 % 256;

            // -- The second takes every encrypted byte, and xor's it against (255 - index), where index is where the byte is located.
            for (var i = 0; i < pLen - 3; i++) {
                check2 += (iByte[i] ^ (255 - i));
            }
            check2 = (check2 + mainKey) % 256;
            // -- The third "is too complex to explain", lel.

            int v = pLen / 8;

            for (var x = 1; x < v + 1; x++) {
                int working = x;
                for (var y = 0; y < 4; y++) {
                    int z = y * v + x;

                    if (z != pLen - 1) {

                        working = working + ((y % 2 == 1 ? 8 : (6 * iByte[z - 1]) + 8));
                    }
                }
                check3 += working;
            }
            check3 = Math.Abs((check3 + (iByte[(mainKey % iLen)]))) % 256;

            if (iByte[pLen - 4] != check1) {
                throw new Exception("Decryption checksum 1 failed.");
            }

            if (iByte[pLen - 3] != check2) {
                throw new Exception("Decryption checksum 2 failed.");
            }

            if (iByte[pLen - 2] != check3) {
                throw new Exception("Decryption checksum 3 failed.");
            }

            if (oByte[0] != 0) {
                var tt = new byte[oByte.Length - 8];
                Buffer.BlockCopy(oByte, 0, tt, 0, tt.Length);
                return tt;
            }

            var temp = new byte[oByte.Length - 9]; // -- trim off the encryption flags at the end as well so that doesn't end up part of our message, lel.
            Buffer.BlockCopy(oByte, 1, temp, 0, temp.Length);
            oByte = temp;

            return oByte;
        }

        /// <summary>
        /// Splits a byte array into two halves, reverses them, and puts them back together.
        /// </summary>
        /// <param name="input">Byte array to be swapped.</param>
        /// <returns>Reversed byte array.</returns>
        private static byte[] ReverseBytes(byte[] input) {
            var first = new byte[input.Length / 2];
            var second = new byte[input.Length / 2];
            Buffer.BlockCopy(input, 0, first, 0, input.Length / 2);
            Buffer.BlockCopy(input, input.Length / 2, second, 0, input.Length / 2);
            Array.Reverse(first);
            Array.Reverse(second);

            var final = new byte[input.Length];
            Buffer.BlockCopy(first, 0, final, 0, first.Length);
            Buffer.BlockCopy(second, 0, final, first.Length, second.Length);
            return final;
        }
    }
}
