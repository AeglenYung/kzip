using Ionic.Zip;
using Ionic.Zlib;
using My.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using xxHashSharp;

namespace kzip
{
    using Hash2Filenames = Dictionary<string, Tuple<long,List<string>>>;
    using ZipSizes = Tuple<int,long, long>;
    
    public class ZipCreate: CommandConfig
    {
        Func<string, string> FixDirectorySep = arg => arg;

        Hash2Filenames hash2Filenames = new Hash2Filenames();

        ZipEntry AddNormalEntry(string filename, ZipFile zFile)
        {
            if (!File.Exists(filename)) return null;
            return zFile.AddEntry(filename, (fname, zs) =>
                {
                    using (var ins = File.OpenRead(fname))
                    {
                        ins.CopyTo(zs);
                    }
                });
        }

        ZipSizes GetNormalipSizes(ZipFile _2)
        {
            return null; // do nothing ..
        }

        ZipEntry AddMd5Entry(string filename, ZipFile zFile)
        {
            if (!File.Exists(filename)) return null;
            string fileMd5 = String.Empty;
            using (var fs = File.OpenRead(filename))
            {
                fileMd5 = Md5.Compute(fs);
            }
            if (hash2Filenames.ContainsKey(fileMd5))
            {
                hash2Filenames[fileMd5].Item2.Add(filename);
                return null;
            }

            var newEntry = zFile.AddEntry(fileMd5, (md5, zs) => 
            {
                using (var ins = File.OpenRead(filename))
                {
                    ins.CopyTo(zs);
                }
            });

            var newTuple = new Tuple<long, List<string>>(
                (new FileInfo(filename)).Length, new List<string>());
            newTuple.Item2.Add(filename);
            hash2Filenames.Add(fileMd5, newTuple);
            return newEntry;
        }

        ZipEntry AddHashEntry(string filename,ZipFile zFile)
        {
            if (!File.Exists(filename)) return null;
            var buf = new byte[64 * 1024];
            int readSize = 0;
            var hasher = new xxHash();
            hasher.Init();
            using (var fs = File.OpenRead(filename))
            {
                while (true)
                {
                    readSize = fs.Read(buf, 0, buf.Length);
                    if (readSize == 0) break;
                    hasher.Update(buf, readSize);
                }
            }
            var fileHash = hasher.Digest().ToString("x8");
            if (hash2Filenames.ContainsKey(fileHash))
            {
                hash2Filenames[fileHash].Item2.Add(filename);
                return null;
            }
            var newEntry = zFile.AddEntry(fileHash, (hashOnly, zs) =>
            {
                using (var ins = File.OpenRead(filename))
                {
                    ins.CopyTo(zs);
                }
            });
            var newTuple = new Tuple<long, List<string>>(
                (new FileInfo(filename)).Length, new List<string>());
            newTuple.Item2.Add(filename);
            hash2Filenames.Add(fileHash, newTuple);
            return newEntry;
        }

        ZipSizes GetHashZipSizes(string hashFilename, ZipFile zFile)
        {
            var oldCnt = 0;
            var oldSize = 0L;
            var redSize = 0L;
            var buf = new StringBuilder();
            foreach (var keyThe in hash2Filenames.Keys)
            {
                var fNames = hash2Filenames[keyThe].Item2;
                var fSize = hash2Filenames[keyThe].Item1;
                oldCnt += fNames.Count;
                oldSize += fSize * fNames.Count;
                redSize += fSize;
                foreach (var fname in fNames)
                {
                    buf.AppendLine($"{keyThe}  {FixDirectorySep(fname)}");
                }
            }
            var contentEntry = zFile.AddEntry(hashFilename,
                ZipEnvir.Encoding.GetBytes(buf.ToString()));
            contentEntry.Password = null;
            return new ZipSizes(oldCnt,oldSize,redSize);
        }

        public override bool Apply(IReadOnlyCollection<string> opts)
        {
            switch (ZipEnvir.HashMethod)
            {
                case "md5":
                    AddEntry = AddMd5Entry;
                    GetZipSizes = arg => GetHashZipSizes(
                        kZipHelper.Md5Filename, arg);
                    break;
                case "xhash":
                    AddEntry = AddHashEntry;
                    GetZipSizes = arg => GetHashZipSizes(
                        kZipHelper.HashFilename, arg);
                    break;
            }

            var zFilename = ZipEnvir.ZipFilename;
            if (File.Exists(zFilename))
            {
                Console.WriteLine($"Output '{zFilename}' ALREDY exists!");
                return false;
            }

            var fnames = ReadFileNameFrom.AddFromFileList(opts);

            if (fnames.Count==0)
            {
                Console.WriteLine("No input file is specified!");
                return false;
            }

            Action<string> Print = arg => Console.Write(arg);
            if (ZipEnvir.Quiet)
            {
                Print = _ => { };
            }

            ZipSizes sizes = null;
            using (var outz = new ZipFile())
            {
                int cntAdded = 0;
                outz.CompressionLevel = Level;
                outz.UseZip64WhenSaving = Zip64Option.AsNecessary;
                outz.AlternateEncodingUsage = ZipOption.AsNecessary;
                outz.AlternateEncoding = ZipEnvir.Encoding;
                if (!String.IsNullOrEmpty(Password))
                {
                    outz.Encryption = Encrypt;
                    outz.Password = Password;
                }

                if (!String.IsNullOrEmpty(TempDir) &&
                    Directory.Exists(TempDir))
                {
                    outz.TempFileFolder = TempDir;
                }

                foreach (var fname in fnames)
                {
                    try
                    {
                        Print(fname);
                        var rslt = AddEntry(fname, outz);
                        if (rslt==null)
                        {
                            Print(" skipped");
                        }
                        else
                        {
                            cntAdded += 1;
                        }
                        Print(Environment.NewLine);
                    }
                    catch (ArgumentException)
                    {   // Do nothing
                        // An item with the same key has already been added
                    }
                    catch (Exception ee)
                    {
                        cntAdded = 0;
                        Console.WriteLine( $"{fname}"+
                            $" [{ee.GetType()}] {ee.Message}");
                        break;
                    }

                }

                if (cntAdded>0)
                {
                    sizes = GetZipSizes(outz);
                    outz.Save(zFilename);
                }
            }

            if (sizes != null)
            {
                if (sizes.Item2 < 1L)
                {
                    Print("Nothing is redcued.");
                }
                else
                {
                    var reduced = 0.0;
                    var oldSize = sizes.Item2;
                    var zFileSize = (new FileInfo(zFilename)).Length;
                    if (zFileSize < oldSize)
                    {
                        reduced = 100.0 * (oldSize - zFileSize);
                        reduced /= oldSize;
                    }
                    Print($"#OriginalFiles:{sizes.Item1}" +
                        $"; ReducedRatio={reduced:N0}%");
                }
                Print(Environment.NewLine);
            }
            return true;
        }

        #region "Properties"
        CompressionLevel Level = CompressionLevel.Level5;
        EncryptionAlgorithm Encrypt = EncryptionAlgorithm.WinZipAes256;
        string Password = String.Empty;
        string TempDir = String.Empty;
        string ReadFileNameFrom = String.Empty;
        Func<string, ZipFile, ZipEntry> AddEntry;
        Func<ZipFile, ZipSizes> GetZipSizes;
        protected WriteDelegate writeDelegateThe;

        public override IReadOnlyCollection<SwitchCfgSetup> SwitchSetups =>
            new SwitchCfgSetup[] {
                new SwitchCfgSetup(
                    "ask-password",String.Empty,
                    () => false,
                    () =>
                    {
                        var tmp = "Input password:".NoEchoInput();
                        if (String.IsNullOrEmpty(tmp))
                        {
                            throw new Exception("No password input");
                        }
                        Password= "Please input  :".NoEchoInput();
                        if (String.IsNullOrEmpty(Password))
                        {
                            throw new Exception("No password input");
                        }
                        if (tmp!=Password)
                        {
                            throw new Exception("Password inputs are NOT matched");
                        }
                        Console.WriteLine(
                            "MD5 of password="+
                            $"{Md5.Compute(Password).InsertDots(4)}");
                    }),
            };

        public override IReadOnlyCollection<ITypeCfgSetup> TypeSetups =>
            new ITypeCfgSetup[] {
                new ValueCfgFactory<string>(
                    arg => arg,
                    arg => arg,
                    new ValueCfgSetup<string>[]
                    {
                        new ValueCfgSetup<string>(
                            'p',"password","=PASSWORD",
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
                            "temp-dir",String.Empty,
                            ()=>Helper.Absent,
                            val => {TempDir=val; }),
                        new ValueCfgSetup<string>(
                            'T',"list","=FILE\t Console if -",
                            () => Helper.Absent,
                            val =>
                            {
                                if (String.IsNullOrEmpty(val)) return;
                                if (val=="-" || File.Exists(val))
                                {
                                    ReadFileNameFrom=val;
                                    return;
                                }
                                throw new ArgumentException(
                                    $"'{val}' is not valid 'read-from' option");
                            })
                    }),
                new ValueCfgFactory<EncryptionAlgorithm>(
                    arg => {
                        if (!String.IsNullOrEmpty(arg))
                        {
                            switch (arg.ToLower())
                            {
                                case "weak":
                                    return EncryptionAlgorithm.PkzipWeak;
                                case "128":
                                    return EncryptionAlgorithm.WinZipAes128;
                            }
                        }
                        return EncryptionAlgorithm.WinZipAes256;
                    },
                    arg => {
                        switch (arg)
                        {
                            case EncryptionAlgorithm.WinZipAes128:
                                return "128";
                            case EncryptionAlgorithm.PkzipWeak:
                                return "weak";
                            default:
                                return Helper.Absent;
                        }
                    },
                    new ValueCfgSetup<EncryptionAlgorithm>(
                        "encrypt", "=[256|128|weak]\t Default:256",
                        () => Encrypt,
                        val => { Encrypt = val; }
                        )
                    ),
                new ValueCfgFactory<CompressionLevel>(
                    arg => {
                        if (String.IsNullOrEmpty(arg))
                        {
                            return CompressionLevel.Level5;
                        }
                        switch (arg)
                        {
                            case "0": return CompressionLevel.Level0;
                            case "2": return CompressionLevel.Level9;
                        };
                        return CompressionLevel.Level5;
                    },
                    arg => {
                        switch (arg)
                        {
                            case CompressionLevel.Level0: return "0";
                            case CompressionLevel.Level9: return "2";
                            default: return Helper.Absent;
                        }
                    },
                    new ValueCfgSetup<CompressionLevel>(
                        'l',"level","=[0|1|2]\t Default:1",
                        () => Level,
                        val => { Level = val; }
                        )
                    ),
            };
        #endregion

        public ZipCreate(string helpHint): 
            base(helpHint, nameof(ZipCreate) + ":")
        {
            if (Path.DirectorySeparatorChar=='\\')
            {
                FixDirectorySep = arg => arg.Replace('\\','/');
            }
            AddEntry = AddNormalEntry;
            GetZipSizes = GetNormalipSizes;
        }
    }

    static class ZipCreateHelper
    {
        static public bool ReadFileToZip(this string fname, Stream ss)
        {
            var buffer = new byte[16 * 1024];
            int readSize = 0;
            using (var fs = File.OpenRead(fname))
            {
                do
                {
                    readSize = fs.Read(buffer, 0, buffer.Length);
                    ss.Write(buffer, 0, readSize);
                } while (readSize > 0);
            }
            return true;
        }
    }
}
