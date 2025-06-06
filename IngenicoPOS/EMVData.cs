using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace IngenicoPOS
{
    public class EMVData
    {
        /// <summary>
        /// Dedicated File (DF) Name (AID)
        /// </summary>
        public string AID { get; set; }

        /// <summary>
        /// Terminal Verification Results
        /// </summary>
        public string TVR { get; set; }

        /// <summary>
        /// Application Preferred Name
        /// </summary>
        public string APN { get; set; }

        /// <summary>
        /// Cardholder Verification Method Results
        /// </summary>
        public string CVMR { get; set; }

        /// <summary>
        /// Transaction Status Information
        /// </summary>
        public string TSI { get; set; } = "0000";

        /// <summary>
        /// Application Cryptogram
        /// </summary>
        public string ARQC { get; set; }

        public EMVData(string rawData)
        {
            byte[] data = HexStringToByteArray(rawData);
            int index = 0;

            while (index < data.Length)
            {
                string tag = ReadTag(data, ref index);
                int length = data[index++];
                byte[] value = new byte[length];
                Array.Copy(data, index, value, 0, length);
                index += length;

                Console.WriteLine($"Tag: {tag}, Length: {length}, Value: {BitConverter.ToString(value).Replace("-", "")}");

                switch (tag)
                {
                    case "84":
                        AID = BitConverter.ToString(value).Replace("-", "");
                        break;
                    case "95":
                        TVR = BitConverter.ToString(value).Replace("-", "");
                        break;
                    case "9B":
                        TSI = BitConverter.ToString(value).Replace("-", "");
                        break;
                    case "9F12":
                        APN = Encoding.ASCII.GetString(value);
                        break;
                    case "9F26":
                        ARQC = BitConverter.ToString(value).Replace("-", "");
                        break;
                    case "9F27":
                        Console.WriteLine("  Cryptogram Info Data: " + value[0].ToString("X2"));
                        break;
                    case "9F34":
                        CVMR = BitConverter.ToString(value).Replace("-", "");
                        break;
                    default:
                        Console.WriteLine("  (Unhandled Tag)");
                        break;
                }
            }

        }


        static string ReadTag(byte[] data, ref int index)
        {
            byte firstByte = data[index++];
            if ((firstByte & 0x1F) == 0x1F) // multi-byte tag
            {
                byte secondByte = data[index++];
                return firstByte.ToString("X2") + secondByte.ToString("X2");
            }
            return firstByte.ToString("X2");
        }

        static byte[] HexStringToByteArray(string hex)
        {
            int len = hex.Length;
            byte[] data = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
            {
                data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return data;
        }

    }
}
