using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
                byte high = rd.ReadByte();
                byte low = rd.ReadByte();

                var b = (high << 4) | low;

                wt.Write((byte)b);
                /*string value = string.Empty;
                byte bit_a = rd.ReadByte();
                byte bit_b = rd.ReadByte();
                value += $"{Compare(bit_b)}{Compare(bit_a)}";
                wt.Write(Convert.ToByte(value, 2));*/
            }
            wt.Close();
            rd.Close();
            return result.ToArray();
        }
        private static string Compare(byte index)
        {
            if (index > 225) index = 225;
            int square = (int)Math.Round(Math.Sqrt(index));
            string value = Convert.ToString(square, 2).PadLeft(4, '0');
            return value;
        }
        public static byte[] Convert4BppTo8Bpp(byte[] input)
        {
            MemoryStream stream = new MemoryStream(input);
            MemoryStream result = new MemoryStream();
            BinaryReader rd = new BinaryReader(stream);
            BinaryWriter wt = new BinaryWriter(result);
            while (rd.BaseStream.Length > rd.BaseStream.Position)
            {
                byte bit = rd.ReadByte();
                /*int low = bit & 0xF;
                int high = bit >> 4;
                wt.Write((byte)high);
                wt.Write((byte)low);*/
                char[] chars = Convert.ToString(bit, 2).PadLeft(8, '0').ToCharArray();
                int firstHalfByte = (int)Math.Pow(int.Parse(new string(chars.Take(4).ToArray()).PadLeft(8, '0')), 2);
                int lastHalfByte = (int)Math.Pow(int.Parse(new string(chars.Skip(4).Take(4).ToArray()).PadLeft(8, '0')), 2);
                wt.Write(Convert.ToByte(firstHalfByte > 255 ? 255 : firstHalfByte));
                wt.Write(Convert.ToByte(lastHalfByte > 255 ? 255 : lastHalfByte));
            }
            wt.Close();
            rd.Close();
            return result.ToArray();
        }
    }
}
