using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Crypto = System.Security.Cryptography;
using Xsd = System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace kzip
{
    static public class kZipHelper
    {
        static public readonly string Md5Filename = ":md5:filename:";

        static public readonly string HashFilename = ":hash:filename:";

        static public string NoEchoInput(this string hint)
        {
            Console.Write(hint);
            var buf = new Stack<char>();
            ConsoleKeyInfo cki;
            Console.TreatControlCAsInput = true;
            do
            {
                cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.Enter) break;
                int inp = (int)cki.KeyChar;

                if (((ConsoleModifiers.Shift & cki.Modifiers) != 0)
                    && (inp >= 'a') && (inp <= 'z'))
                {
                    inp += 'A' - 'a';
                }

                if ((inp >= ' ') && (inp < 127))
                {
                    buf.Push((char)inp);
                    Console.Write('*');
                }
                else if (cki.Key == ConsoleKey.Backspace)
                {
                    if (buf.Count > 0)
                    {
                        buf.Pop();
                        Console.Write('<');
                    }
                }
            } while (true);
            Console.WriteLine();
            var tmp2 = buf.ToArray();
            Array.Reverse(tmp2);
            return new string(tmp2);
        }

        static public string InsertDots(this string arg, int padLength)
        {
            if (String.IsNullOrEmpty(arg)) return String.Empty;
            if (padLength < 2) return arg;
            int loopCnt = (arg.Length - 1) / padLength;
            var rtn = new StringBuilder();
            for (int ii = 0; ii < loopCnt; ii++)
            {
                rtn.Append(arg.Substring(ii * padLength, padLength));
                rtn.Append(".");
            }
            rtn.Append(arg.Substring(loopCnt * padLength));
            return rtn.ToString();
        }

        static public IList<string> AddFromFileList(this string fname,
            IReadOnlyCollection<string> args)
        {
            if (String.IsNullOrEmpty(fname))
            {
                return args.UniqueStrings();
            }

            StreamReader inp;
            if (fname == "-")
            {
                if (!Console.IsInputRedirected)
                {
                    Console.Error.WriteLine(
                        @"Enter filenames (Ctl-Z/Ctrl-D to end)");
                }
                inp = new StreamReader(Console.OpenStandardInput(),
                    Console.InputEncoding);
            }
            else
            {
                inp = new StreamReader(File.OpenRead(fname));
            }

            var rtn = new List<string>(args);
            while (true)
            {
                var line = inp.ReadLine();
                if (line == null) break;
                rtn.Add(line.Trim());
            }
            return rtn.UniqueStrings();
        }

        public static IList<string> UniqueStrings(
            this IReadOnlyCollection<string> args)
        {
            var qry = from arg in args
                      where arg.Trim().Length > 0
                      select arg.Trim();
            return new List<string>(new HashSet<string>(qry));
        }
    }

    public class ConsoleWriter
    {
        public readonly Action<string> WithoutCrlf
            = msg => Console.Write(msg);
        public readonly Action<string> WithCrlf
            = msg => Console.WriteLine(msg);
        public ConsoleWriter(Action<string> withoutCrlf,
            Action<string> withCrlf)
        {
            WithoutCrlf = withoutCrlf;
            WithCrlf = withCrlf;
        }

        static public readonly ConsoleWriter Enable =
            new ConsoleWriter(
                withoutCrlf: msg => Console.Write(msg),
                withCrlf: msg => Console.WriteLine(msg));

        static public readonly ConsoleWriter Disable =
            new ConsoleWriter(_ => { }, _ => { });
    }

    public class Md5
    {
        public static string Compute(Encoding encoding, string content)
        {
            Crypto.HashAlgorithm thisAlgorithm = Crypto.MD5.Create();
            byte[] thisHash = thisAlgorithm.ComputeHash(encoding.GetBytes(content));
            Xsd.SoapHexBinary thisOuput = new Xsd.SoapHexBinary(thisHash);
            return thisOuput.ToString().ToLower();
        }

        public static string Compute(string content)
        {
            return Compute(Encoding.UTF8, content);
        }

        public static string Compute(Stream stream)
        {
            Crypto.HashAlgorithm thisAlgorithm = Crypto.MD5.Create();
            byte[] thisHash = thisAlgorithm.ComputeHash(stream);
            Xsd.SoapHexBinary thisOuput = new Xsd.SoapHexBinary(thisHash);
            return thisOuput.ToString().ToLower();
        }
    }
}
