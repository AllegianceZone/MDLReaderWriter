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
            MDLFileReaderWriter.MDLFile.MDLFile.OutFormat outFormat = MDLFile.OutFormat.TextMdl;

            // assume stdin and stdout
            input = Console.OpenStandardInput();
            output = Console.OpenStandardOutput();
            string mtlName = "";
            string mtlPath = "";
            string outFile = "";
            string inFile = "";
            foreach (var item in args)
            {
                var idx = item.IndexOfAny(new char[] { '=', ':' });
                var split = new string[] { item.Substring(0,idx),item.Substring(idx+1)}  ;
                switch (split[0].ToLower().Substring(1))
                {
                    case "file": input = File.Open(inFile=split[1], FileMode.Open, FileAccess.Read);
                        if (outFormat == MDLFile.OutFormat.Collada)
                        {
                            mtlName = Path.GetFileNameWithoutExtension(inFile);
                        }
                        break;
                    case "out": output = File.Create(outFile = split[1]);
                        if (outFormat == MDLFile.OutFormat.Collada)
                        {
                            mtlPath = Path.GetDirectoryName(outFile);
                        }
                        break;
                    case "stdin": input = Console.OpenStandardInput();
                        break;
                    case "stdout": output = Console.OpenStandardOutput();
                        break;
                    case "format":
                        if (split[1].Equals("textmdl", StringComparison.CurrentCultureIgnoreCase))
                        {
                            outFormat = MDLFile.OutFormat.TextMdl;
                        }
                        if (split[1].Equals("dae", StringComparison.CurrentCultureIgnoreCase))
                        {
                            outFormat = MDLFile.OutFormat.Collada;
                            if (string.IsNullOrEmpty(mtlPath))
                            {
                                mtlPath = Path.GetDirectoryName(outFile);
                            }
                            if (string.IsNullOrEmpty(mtlName))
                            {
                                mtlName = Path.GetFileNameWithoutExtension(inFile);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            
            MDLFile mdl = new MDLFile();
            var readToEnd = mdl.Load(input);
            mdl.MtlName = mtlName;
            mdl.MtlPath = mtlPath;
            var text = mdl.ToString(outFormat);
            using(var sw = new StreamWriter(output,Encoding.UTF8))
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
