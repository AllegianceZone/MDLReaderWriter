using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MDLFileReaderWriter.MDLFile;
using System.IO;

namespace MDLd
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 
                || args.Any(x => x.ToLower().Contains("help"))
                || args.Any(x => x.ToLower().Contains("-h"))
                || args.Any(x => x.ToLower().Contains("/h"))
                || args.Any(x => x.ToLower().Contains("-?"))
                || args.Any(x => x.ToLower().Contains("/?")))
            {
                WriteUsage();
                return;
            }
            // check parameters
            Stream input = null;
            Stream output = null;
            
            // assume stdin and stdout
            input = Console.OpenStandardInput();
            output = Console.OpenStandardOutput();
            
            foreach (var item in args)
            {
                var idx = item.IndexOfAny(new char[] { '=', ':' });
                var split = new string[] { item.Substring(0,idx),item.Substring(idx+1)}  ;
                switch (split[0].ToLower().Substring(1))
                {
                    case "file": input = File.Open(split[1], FileMode.Open, FileAccess.Read);
                        break;
                    case "out": output = File.Create(split[1]);
                        break;
                    case "stdin": input = Console.OpenStandardInput();
                        break;
                    case "stdout": output = Console.OpenStandardOutput();
                        break;
                    default:
                        break;
                }
            }

            
            MDLFile mdl = new MDLFile();
            var readToEnd = mdl.Load(input);
            var text = mdl.ToString();

            using(var sw = new StreamWriter(output))
            {
                sw.Write(text);
            }
        }

        private static void WriteUsage()
        {
            Console.WriteLine("Usage: (/file=PathToFile | /stdin) (/out=PathToOutput | /stdout)");
        }
    }
}
