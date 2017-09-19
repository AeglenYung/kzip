using Ionic.Zip;
using My.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace kzip
{
    class ZipExtract : CommandConfig
    {
        void ExtraEntry(ZipEntry zEntry, string fname)
        {
            Print(fname);
            var fname2 = Path.Combine(OutDir, NormalFileName(fname));
            if (File.Exists(fname2))
            {
                throw new FieldAccessException(
                    $"'{fname2}' already exists!");
            }
            fname2.CreateParentFolder();
            using (var outs = File.Create(fname2))
            {
                zEntry.Extract(outs);
            }
            File.SetLastWriteTime(fname2, zEntry.LastModified);
        }

        Action<string> Print = msg => Console.WriteLine(msg);

        public override bool Apply(IReadOnlyCollection<string> argFNames)
        {
            var zFilename = ZipEnvir.ZipFilename;
            if (!File.Exists(zFilename))
            {
                throw new FileNotFoundException(zFilename);
            }

            if (ZipEnvir.Quiet)
            {
                Print = _ => { };
            }

            if (IsCreateDir)
            {
                if (!String.IsNullOrEmpty(OutDir))
                {
                    throw new Exception("Option '--new-dir' conflicts to"
                        + " option '--out-dir'.");
                }
                OutDir = Path.GetFileNameWithoutExtension(zFilename);
                if (Directory.Exists(OutDir))
                {
                    throw new Exception($"Directory '{OutDir}'"+
                        " already exists!");
                }
                Directory.CreateDirectory(OutDir);
            }
            else
            {
                if ((!String.IsNullOrEmpty(OutDir)) &&
                    (!Directory.Exists(OutDir)))
                {
                    throw new DirectoryNotFoundException(OutDir);
                }
            }

            using (var inps = File.OpenRead(zFilename))
            using (var inpz = ZipFile.Read(inps))
            {
                if (!String.IsNullOrEmpty(Password))
                {
                    inpz.Password = Password;
                }

                if (String.IsNullOrEmpty(ZipEnvir.HashMethod))
                {
                    foreach (var ze in inpz
                        .Where(ze => !ze.IsDirectory)
                        .Join(argFNames))
                    {
                        ExtraEntry(ze, ze.FileName);
                    }
                    return true;
                }

                var hashFnames = ZipItem.ReadHashFileNames(inpz,
                    ZipEnvir.HashMethod);

                foreach (var zEntry in inpz)
                {
                    var qry1 = hashFnames.Where(
                        pairThe => pairThe.Key.Name.Equals(zEntry.FileName));
                    if (!qry1.Any()) continue;
                    var hashEntry = qry1.First();
                    var qry2 = hashEntry.Value.Join(argFNames);
                    if (!qry2.Any()) continue;
                    var firstFName = qry2.First();
                    ExtraEntry(zEntry, firstFName);
                    foreach (var fname in qry2.Skip(1))
                    {
                        var fname2 = Path.Combine(OutDir,
                            NormalFileName(fname));
                        if (File.Exists(fname2))
                        {
                            throw new Exception($"{fname2} does exists!");
                        }
                        Print(fname);
                        fname2.CreateParentFolder();
                        File.Copy(firstFName, fname2);
                    }
                }
            }
            return true;
        }

        #region "Properties"
        bool IsCreateDir = false;
        string OutDir = String.Empty;
        string Password = String.Empty;
        Func<string, string> NormalFileName = arg => arg;

        public override IReadOnlyCollection<SwitchCfgSetup> SwitchSetups =>
            new SwitchCfgSetup[] {
                new SwitchCfgSetup(
                    "ask-password",String.Empty,
                    () => false,
                    () =>
                    {
                        Password = "Input password:".NoEchoInput();
                        if (String.IsNullOrEmpty(Password))
                        {
                            throw new Exception("No password input");
                        }
                        Console.WriteLine(
                            "MD5 of password="+
                            $"{Md5.Compute(Password).InsertDots(4)}");
                    }),
                new SwitchCfgSetup(
                    "fix-name","Remove invalid path char",
                    () => NormalFileName(":")=="_",
                    () =>
                    {
                        NormalFileName = arg => arg
                            .Replace(':','_').Replace('|','_')
                            .Replace('<','_').Replace('>','_');
                    }),
                new SwitchCfgSetup(
                    'n',"new-dir","Create zip-filename for out-dir ",
                    () => IsCreateDir,
                    () => { IsCreateDir=true; }),
            };

        public override IReadOnlyCollection<ITypeCfgSetup> TypeSetups =>
            new ITypeCfgSetup[] {
                new ValueCfgFactory<string>(
                    arg => arg,
                    arg => arg,
                    new ValueCfgSetup<string>[]
                    {
                        new ValueCfgSetup<string>(
                            'p',"password",String.Empty,
                            ()=>Helper.Absent,
                            val=>{
                                if (!String.IsNullOrEmpty(val))
                                {
                                    Password = val;
                                    Console.WriteLine(
                                        "MD5 of password="+
                                        $"{Md5.Compute(Password).InsertDots(4)}");
                                }
                            }),
                        new ValueCfgSetup<string>(
                            'o',"out-dir","=NewDir",
                            ()=>Helper.Absent,
                            val=>{ OutDir = val; }),
                    })
            };
        #endregion

        public ZipExtract(string helpHint): 
            base(helpHint, nameof(ZipExtract) + ":")
        {
        }
    }

    static class ZipExtractHelper
    {
        public static bool CreateParentFolder(this String arg)
        {
            var theDir = Path.GetDirectoryName(arg);
            if (!String.IsNullOrEmpty(theDir) &&
                !Directory.Exists(theDir))
            {
                Directory.CreateDirectory(theDir);
                return true;
            }
            return true;
        }

        public static IEnumerable<string> Join(this IEnumerable<string> args,
            IReadOnlyCollection<string> right)
        {
            if (right.Any())
            {
                return args.Where(arg => right.Contains(arg));
            }
            return args;
        }

        public static IEnumerable<ZipEntry> Join(this IEnumerable<ZipEntry> args,
            IReadOnlyCollection<string> right)
        {
            if (right.Any())
            {
                return args.Where(arg => right.Contains(arg.FileName));
            }
            return args;
        }
    }
}
