using System;
using My.Config;
using System.Text;

namespace kzip
{
    class ZipEnvir : ConfigEnvir
    {
        static string MakeSyntax(string exeName)
        {
            return "Syntax:\n"
                + $"\t{exeName} -cf new.ZIP  [opt ..] [file ..]\n"
                + $"\t{exeName} -vf file.ZIP [opt ..]\n"
                + $"\t{exeName} -xf file.ZIP [opt ..] [file ..]\n"
                + "Help:\n"
                + $"\t{exeName} -?\n"
                + $"\t{exeName} [-c|-v|-x] [-?|-h|--help]\n"
                + "Version:\n"
                + $"\t{exeName} --version\n"
                + "\n"+ "Sample:\n"
                + $"\t{exeName} -cf backup.zip readme.txt\n"
                + $"\t{exeName} -vf backup.zip\n"
                + $"\t{exeName} -x --file=backup.zip\n"
                ;
        }

        static public bool Debug { get; private set; } = false;

        public override string ToString()
        {
            return $"ZipFilename='{ZipFilename}'";
        }

        public static bool Quiet { get; private set; } = false;

        public static string ZipFilename { get; private set; }
            = String.Empty;

        public static string HashMethod { get; private set; }
            = String.Empty;

        public ZipEnvir(string exeName): base(
            MakeSyntax(exeName)
            , new CommandSetup[] {
                new CommandSetup('c', "create", () => {
                    return new ZipCreate("Create command:\n" +
                        $"\t{exeName} -cf new.ZIP [opt ..] [file ..]\n");
                }),
                new CommandSetup('v', "view", () => {
                    return new ZipView("View command:\n" +
                        $"\t{exeName} -vf file.zip [opt ..]\n");
                }),
                new CommandSetup('x', "extract", () => {
                    return new ZipExtract("Extract command:\n" +
                        $"\t{exeName} -xf file.ZIP [opt ..] [file ..]\n");
                }),
            }
            , new SwitchSetup[] {
                new SwitchSetup('q',"quiet",String.Empty,
                    () => { Quiet=true; }),
                new SwitchSetup("md5", "Unique by MD5",
                    () => {
                        if (!String.IsNullOrEmpty(HashMethod)
                        && (HashMethod!="md5"))
                        {
                            throw new ArgumentException(
                                $"Cannot set md5 option after {HashMethod} option");
                        }
                        HashMethod = "md5";
                    }),
                new SwitchSetup("xhash", "Unique by xxHash",
                    () => {
                        if (!String.IsNullOrEmpty(HashMethod)
                        && (HashMethod!="xhash"))
                        {
                            throw new ArgumentException(
                                $"Cannot set xhash option after {HashMethod} option");
                        }
                        HashMethod = "xhash";
                    }),
                new SwitchSetup("debug", String.Empty,
                    () => { Debug=true; })
            }
            , new ITypeSetup[] {
                new ValueFactory<string>(
                    arg => arg,
                    new ValueSetup<string>(
                        'f',"file","=zip-filename",
                        val => { ZipFilename = val; }))
            })
        {
        }

        public static Encoding Encoding { get { return Encoding.UTF8; } }
    }
}
