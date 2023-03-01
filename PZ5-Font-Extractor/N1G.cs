using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PZ5_Font_Extractor
{
    public static class N1G
    {
        private struct Header
        {
            public Int64 Magic;
            public int FileSize;
            public int HeaderSize;
            public int AtlasOffset;
            public int PalleteCount;
            public int TableCount;
        }
        private struct CharID
        {
            public int CharCode;
            public char Character;
        }
        private struct GlyphInfo
        {
            public int CharCode;
            public string Character;
            public byte Width;
            public byte Height;
            public int TexOffset;
            public int TexSize;
            public byte[] PixelData;
        }
        private static Header ReadHeader(ref BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            Header header = new Header();
            header.Magic = reader.ReadInt64();
            header.FileSize = reader.ReadInt32();
            header.HeaderSize = reader.ReadInt32();
            reader.BaseStream.Position += 4;
            header.AtlasOffset = reader.ReadInt32();
            header.PalleteCount = reader.ReadInt32();
            header.TableCount= reader.ReadInt32();
            return header;
        }

        private static GlyphInfo[] ReadGlyphs(ref BinaryReader reader, Header header)
        {
            reader.BaseStream.Position = header.HeaderSize;
            int charCount = 0;
            CharID[] charIDs = new CharID[0xFFFF];
            for (int i = 0; i < 0xFFFF; i++)
            {
                ushort ordinal = reader.ReadUInt16();
                if ((ordinal == 0 && charCount <= ordinal + 1) || ordinal > 0)
                {
                    charCount = ordinal + 1;
                    charIDs[ordinal].CharCode = i;
                    charIDs[ordinal].Character = (char)i;
                }
            }
            reader.BaseStream.Position += 2;
            GlyphInfo[] glyphs = new GlyphInfo[charCount];
            for (int i = 0; i < charCount; i++)
            {
                glyphs[i].CharCode = charIDs[i].CharCode;
                glyphs[i].Character = charIDs[i].Character.ToString();
                glyphs[i].Width = reader.ReadByte();
                glyphs[i].Height = reader.ReadByte();
                reader.BaseStream.Position += 6;
                glyphs[i].TexOffset = reader.ReadInt32();
                long temp = reader.BaseStream.Position;

                if (i >= charCount - 1)
                {
                    glyphs[i].TexSize = (int)reader.BaseStream.Length - (glyphs[i].TexOffset + header.AtlasOffset);
                }
                else
                {
                    reader.BaseStream.Position += 8;
                    glyphs[i].TexSize = reader.ReadInt32() - glyphs[i].TexOffset;
                }
                reader.BaseStream.Position = header.AtlasOffset + glyphs[i].TexOffset;

                //glyphs[i].PixelData = reader.ReadBytes(0x0992); //4 bpp, Width * Height * 0.5 = image size, the rest can be shadow or mipmap
                glyphs[i].PixelData = reader.ReadBytes(glyphs[i].TexSize);
                reader.BaseStream.Position = temp;
            }
            return glyphs;
        }
        public static void Extract(string input, string output)
        {
            using (FileStream stream = File.OpenRead(input))
            {
                BinaryReader reader = new BinaryReader(stream);
                Header header = ReadHeader(ref reader);
                GlyphInfo[] glyphs = ReadGlyphs(ref reader, header);
                string pixelPath = Path.Combine(output, "IndexedPixelData");
                string importPath = Path.Combine(pixelPath, "Import");
                if (!Directory.Exists(pixelPath)) Directory.CreateDirectory(pixelPath);
                if (!Directory.Exists(importPath)) Directory.CreateDirectory(importPath);
                List<string> fontData = new List<string>();
                foreach (GlyphInfo glyph in glyphs)
                {
                    int imgWidth = glyph.Width % 2 == 0 ? glyph.Width : glyph.Width + 1;
                    int pixelSize = (imgWidth * glyph.Height * 4) / 8;
                    byte[] realPixelData = new byte[pixelSize];
                    for (int i = 0; i < pixelSize; i++) realPixelData[i] = glyph.PixelData[i];

                    byte[] convertedData = IndexedPixel.Convert4BppTo8Bpp(realPixelData);
                    byte[] ddsHeader = CreateDDSHeader(imgWidth, glyph.Height);
                    byte[] ddsData = new byte[convertedData.Length + ddsHeader.Length];
                    ddsHeader.CopyTo(ddsData, 0);
                    convertedData.CopyTo(ddsData, ddsHeader.Length);
                    File.WriteAllBytes(Path.Combine(pixelPath, $"{glyph.CharCode}.dds"), ddsData);
                    File.WriteAllBytes(Path.Combine(pixelPath, $"{glyph.CharCode}"), glyph.PixelData); 
                    fontData.Add($"Char={glyph.Character}\tCode={glyph.CharCode}\tWidth={glyph.Width}\tHeight={glyph.Height}");
                }
                File.WriteAllLines(Path.Combine(output, "glyphs.txt"), fontData.ToArray());
                Console.WriteLine($"Unpacked: {glyphs.Length} glyphs");
                reader.Close();
            }
        }
        private static byte[] CreateDDSHeader(int width, int height)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Properties.Resources.DDSHeader);
            writer.BaseStream.Position = 0xC;
            writer.Write(height);
            writer.Write(width);
            writer.Write(width);
            writer.Close();
            return stream.ToArray();
        }
        public static void Import(string original, string input, string output)
        {
            MemoryStream result = new MemoryStream();
            List<GlyphInfo> glyphs = new List<GlyphInfo>();
            using (BinaryWriter writer = new BinaryWriter(result))
            {
                using (FileStream stream = File.OpenRead(original))
                {
                    string[] fontData = File.ReadAllLines(Path.Combine(input, "glyphs.txt")).Where(e => e.Length > 0).OrderBy(e => 
                        int.Parse(e.Split(new string[] { "\t" }, StringSplitOptions.None)[1].Split('=')[1]))
                        .ToArray();
                    BinaryReader reader = new BinaryReader(stream);
                    Header header = ReadHeader(ref reader);
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    writer.Write(reader.ReadBytes(header.HeaderSize));
                    ushort[] charIDs = new ushort[0xFFFF];
                    int textOffset = 0;
                    int ordinal = 0;
                    for (int i = 0; i < fontData.Length; i++)
                    {
                        string[] data = fontData[i].Split(new string[] { "\t" }, StringSplitOptions.None);
                        if (data.Length == 0 || data.Length < 7) continue;
                        try
                        {
                            GlyphInfo glyph = new GlyphInfo();
                            glyph.Character = data[0].Split(new string[] { "Char=" }, StringSplitOptions.None)[1];
                            glyph.CharCode = int.Parse(data[1].Split('=')[1]);
                            string tex = Path.Combine(input, "IndexedPixelData", "Import", $"{glyph.CharCode}.dds");
                            glyph.Width = (byte)int.Parse(data[2].Split('=')[1]);
                            glyph.Height = (byte)int.Parse(data[3].Split('=')[1]);
                            glyph.PixelData = new byte[glyph.TexSize];
                            if (!File.Exists(tex))
                            {
                                tex = Path.Combine(input, "IndexedPixelData", $"{glyph.CharCode}");
                                if (!File.Exists(tex)) continue;
                                glyph.PixelData = File.ReadAllBytes(tex);
                            }
                            else
                            {
                                byte[] pixelData = IndexedPixel.Convert8BppTo4Bpp(File.ReadAllBytes(tex));
                                if (pixelData.Length > glyph.TexSize)
                                {
                                    Console.WriteLine($"{Path.GetFileName(tex)}: Import pixel data file size does not match the original size!");
                                    continue;
                                }
                                pixelData.CopyTo(glyph.PixelData, 0);
                            }    
                            glyph.TexOffset = textOffset;
                            glyphs.Add(glyph);
                            textOffset += glyph.PixelData.Length;
                            charIDs[glyph.CharCode] = (ushort)ordinal++;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            continue;
                        }
                    }
                    for (int i = 0; i < charIDs.Length; i++)
                    {
                        writer.Write(charIDs[i]);
                    }
                    writer.Write(new byte[2]);
                    //glyphs = glyphs.OrderBy(g => g.CharCode).ToList();
                    long glyphOffset = writer.BaseStream.Position;
                    writer.Write(new byte[0xC * glyphs.Count]);
                    long atlasOffset = writer.BaseStream.Position;
                    writer.BaseStream.Position = glyphOffset;
                    for (int i = 0; i < glyphs.Count; i++)
                    {
                        writer.Write(glyphs[i].Width);
                        writer.Write(glyphs[i].Height);
                        writer.BaseStream.Position++;
                        writer.Write(glyphs[i].Width);
                        writer.BaseStream.Position += 2;
                        writer.Write(glyphs[i].Height);
                        writer.Write(glyphs[i].TexOffset);
                        long temp = writer.BaseStream.Position;
                        writer.BaseStream.Position = atlasOffset + glyphs[i].TexOffset;
                        writer.Write(glyphs[i].PixelData);
                        writer.BaseStream.Position = temp;
                    }
                    writer.BaseStream.Position = 8;
                    writer.Write((int)writer.BaseStream.Length);
                    writer.BaseStream.Position = 20;
                    writer.Write((int)atlasOffset);
                    reader.Close();
                }
            }
            File.WriteAllBytes(output, result.ToArray());
            Console.WriteLine($"Repacked: {glyphs.Count} glyphs");
            result.Close();
        }
    }
}
