using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace kzip
{
    using Hash2Filenames = Dictionary<string, List<string>>;
    using Hash2List = Dictionary<ZipItem, List<string>>;

    public class ZipItem
    {
        public readonly string Name;
        public readonly long Size;
        public readonly long CompressedSize;
        public readonly DateTime LastModified;
        public readonly int Crc;
        public readonly bool Encrypted;
        public readonly EncryptionAlgorithm EncryptionAlgorithm;

        public ZipItem(string name, long size,
            long compressedSize, DateTime lastModified,
            int crc, bool encrypted, 
            EncryptionAlgorithm encryptionAlgorithm)
        {
            Name = name;
            Size = size;
            CompressedSize = compressedSize;
            LastModified = lastModified;
            Crc = crc;
            Encrypted = encrypted;
            EncryptionAlgorithm = encryptionAlgorithm;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        static public IEnumerable<ZipItem> GetNormalItems(ZipFile zFile)
        {
            foreach (var zEntry in zFile
                .Where(entry => !entry.IsDirectory))
            {
                yield return new ZipItem(zEntry.FileName,
                    zEntry.UncompressedSize,
                    zEntry.CompressedSize,
                    zEntry.LastModified,
                    zEntry.Crc,
                    zEntry.UsesEncryption,
                    zEntry.Encryption);
            }
        }

        static public IEnumerable<ZipItem> GetHashItems(ZipFile zFile,
            string hashCode)
        {
            var hashFilenames = ReadHashFileNames(zFile,hashCode);
            _fileSum = new ZipSum(String.Empty);
            foreach (var pairThe in hashFilenames)
            {
                _fileSum.Add(pairThe.Key);
                foreach (var fname in pairThe.Value)
                {
                    var keyThe = pairThe.Key;
                    yield return new ZipItem(fname,
                        keyThe.Size, keyThe.CompressedSize,
                        keyThe.LastModified, keyThe.Crc,
                        keyThe.Encrypted, keyThe.EncryptionAlgorithm);
                }
            }
        }

        static public Func<ZipFile,IEnumerable<ZipItem>> GetItems(
            string hashCode)
        {
            {
                if (String.IsNullOrEmpty(hashCode))
                {
                    return GetNormalItems;
                }
                return new Func<ZipFile,IEnumerable<ZipItem>>(
                    zFile =>GetHashItems(zFile,hashCode));
            }
        }

        static ZipSum _fileSum = null;
        public static ZipSum FileSum() { return _fileSum; }

        public static Hash2List ReadHashFileNames(ZipFile zFile,
            string hashCode)
        {
            var hashFilename = String.Empty;
            var hashLen = 1;
            var fnameOffset = 3;
            switch (hashCode)
            {
                case "md5":
                    hashFilename = kZipHelper.Md5Filename;
                    hashLen = 32;
                    fnameOffset = 2 + hashLen;
                    break;
                case "xhash":
                    hashFilename = kZipHelper.HashFilename;
                    hashLen = 8;
                    fnameOffset = 2 + hashLen;
                    break;
            }

            var qryThe = zFile
                .Where(ze => ze.FileName == hashFilename)
                .Take(1);
            if (!qryThe.Any())
            {
                throw new FileNotFoundException($"Hash content '{hashFilename}'"
                    + " is NOT found!");
            }

            var hash2Fnames = new Hash2Filenames();
            var ms = new MemoryStream();
            qryThe.First().Extract(ms);
            ms.Position = 0;
            var buf = ms.ToArray();
            foreach (var line in Encoding.UTF8.GetString(buf).Split(
                new string[] { "\n\r", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries)
                .Where(txt => txt.Length > fnameOffset))
            {
                var hashThe = line.Substring(0, hashLen);
                var fnameThe = line.Substring(fnameOffset);
                if (!hash2Fnames.Keys.Contains(hashThe))
                {
                    hash2Fnames.Add(hashThe, new List<string>());
                }
                hash2Fnames[hashThe].Add(fnameThe);
            }

            var rtn = new Hash2List();
            foreach (var zEntry in zFile)
            {
                if (hash2Fnames.Keys.Contains(zEntry.FileName))
                {
                    var listThe = hash2Fnames[zEntry.FileName];
                    var itemThe = new ZipItem(zEntry.FileName,
                        zEntry.UncompressedSize, zEntry.CompressedSize,
                        zEntry.LastModified, zEntry.Crc,
                        zEntry.UsesEncryption, zEntry.Encryption);
                    rtn.Add(itemThe, listThe);
                }
            }
            return rtn;
        }
    }

    public class ZipSum
    {
        public readonly string Name;
        public int Count { get; private set; }
        public long Size { get; private set; }
        public long CompressedSize { get; private set; }
        public DateTime Earliest { get; private set; }
        public DateTime Lastest { get; private set; }

        public ZipSum(string name)
        {
            Name = name;
            Count = 0;
            Size = 0L;
            CompressedSize = 0L;
            Earliest = DateTime.MaxValue;
            Lastest = DateTime.MinValue;
        }

        public ZipSum Add(ZipItem right)
        {
            Count += 1;
            Size += right.Size;
            CompressedSize += right.CompressedSize;
            if (Earliest > right.LastModified)
                Earliest = right.LastModified;
            if (Lastest < right.LastModified)
                Lastest = right.LastModified;
            return this;
        }

        public ZipSum Add(ZipSum right)
        {
            Count += right.Count;
            Size += right.Size;
            CompressedSize += right.CompressedSize;
            if (Earliest > right.Earliest)
                Earliest = right.Earliest;
            if (Lastest < right.Lastest)
                Lastest = right.Lastest;
            return this;
        }
    }
}
