using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PZ5_Font_Extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "PZ5 Font Extractor by LeHieu - VietHoaGame";
            if (args.Length > 0)
            {
                foreach (string file in args)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    FileAttributes attr = File.GetAttributes(file);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        N1G.Import($"{file}.g1n", file, $"{Path.Combine(Path.GetFileNameWithoutExtension(file))}-new.g1n");
                    }
                    else if (ext == ".g1n")
                    {
                        N1G.Extract(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));
                    }
                }
            }
            else
            {
                Console.WriteLine("Please drag and drop files/folder into this tool to unpack/repack.");
            }
            Console.ReadKey();
        }
    }
}
