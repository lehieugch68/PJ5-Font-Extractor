using System;
using System.IO;
using System.Globalization;

namespace PZ5_Font_Extractor
{
    public static class IndexedPixel
    {
        public static byte[] Convert8BppTo4Bpp(byte[] input)
        {
            MemoryStream stream = new MemoryStream(input);
            MemoryStream result = new MemoryStream();
            BinaryReader rd = new BinaryReader(stream);
            BinaryWriter wt = new BinaryWriter(result);
            rd.BaseStream.Seek(128, SeekOrigin.Begin);
            while (rd.BaseStream.Length > rd.BaseStream.Position)
            {
                string color = string.Empty;
                color += JoinBit(rd.ReadByte());
                color += JoinBit(rd.ReadByte());

                wt.Write(byte.Parse(color, NumberStyles.HexNumber));
            }
            wt.Close();
            rd.Close();
            return result.ToArray();
        }
        /*private static string Compare(byte index)
        {
            if (index > 225) index = 225;
            int square = (int)Math.Round(Math.Sqrt(index));
            string value = Convert.ToString(square, 2).PadLeft(4, '0');
            return value;
        }*/
        public static byte[] Convert4BppTo8Bpp(byte[] input)
        {
            MemoryStream stream = new MemoryStream(input);
            MemoryStream result = new MemoryStream();
            BinaryReader rd = new BinaryReader(stream);
            BinaryWriter wt = new BinaryWriter(result);
            while (rd.BaseStream.Length > rd.BaseStream.Position)
            {
                string bit = rd.ReadByte().ToString("X2");
                wt.Write(byte.Parse($"{bit[0]}0", NumberStyles.HexNumber));
                wt.Write(byte.Parse($"{bit[1]}0", NumberStyles.HexNumber));
            }
            wt.Close();
            rd.Close();
            return result.ToArray();
        }
        private static string JoinBit(byte input)
        {
            string res = string.Empty;
            if (input <= 0x0F)
                res = "0";
            if (0x0F < input && input <= 0x20)
                res = "1";
            if (0x20 < input && input <= 0x30)
                res = "2";
            if (0x30 < input && input <= 0x40)
                res = "3";
            if (0x40 < input && input <= 0x50)
                res = "4";
            if (0x50 < input && input <= 0x60)
                res = "5";
            if (0x60 < input && input <= 0x70)
                res = "6";
            if (0x70 < input && input <= 0x80)
                res = "7";
            if (0x80 < input && input <= 0x90)
                res = "8";
            if (0x90 < input && input <= 0xA0)
                res = "9";
            if (0xA0 < input && input <= 0xB0)
                res = "A";
            if (0xB0 < input && input <= 0xC0)
                res = "B";
            if (0xC0 < input && input <= 0xD0)
                res = "C";
            if (0xD0 < input && input <= 0xE0)
                res = "D";
            if (0xE0 < input && input <= 0xF0)
                res = "E";
            if (0xF0 < input && input <= 0xFF)
                res = "F";
            return res;
        }
    }
}
