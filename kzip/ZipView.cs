using Ionic.Zip;
using My.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace kzip
{
    public class ZipView : MyCommandConfig
    {
        public override bool Apply(IReadOnlyCollection<string> opts)
        {
            var zFilename = ZipEnvir.ZipFilename;
            if (!File.Exists(zFilename))
            {
                throw new FileNotFoundException(zFilename);
            }

            if (ZipEnvir.Quiet) // (owner.Quiet)
            {
                Int64ToText = _ => String.Empty;
                Int32ToText = _ => String.Empty;
                DateToText = _ => String.Empty;
                TimeToText = _ => String.Empty;
                RatioToText = (_1, _2) => String.Empty;
                PrintTotal = (arg,_) => arg;
            }

            var oldSize = 0L;
            var zFileSize = (new FileInfo(zFilename)).Length;
            using (var inps = File.OpenRead(zFilename))
            using (var inpz = ZipFile.Read(inps))
            {
                var qryThe = ZipItem.GetItems(ZipEnvir.HashMethod);
                ZipSum infoSum = MyAggregate(qryThe(inpz), zFilename);
                oldSize = infoSum.Size;
                PrintTotal(infoSum,zFileSize);
            }

            var fileSum = ZipItem.FileSum();
            if (fileSum!=null)
            {
                TotalWrite.WithoutCrlf("Size reduced by "+ZipEnvir.HashMethod
                    +" to ");
                TotalWrite.WithoutCrlf(Int64ToText(fileSum.Size).Trim());
                TotalWrite.WithoutCrlf(" ("+
                    oldSize.ReducedRatio(fileSum.Size).Trim() +"%);");
                TotalWrite.WithoutCrlf(" overall to ");                
                TotalWrite.WithoutCrlf(Int64ToText(zFileSize).Trim());
                TotalWrite.WithoutCrlf(" (" + 
                    oldSize.ReducedRatio(zFileSize).Trim() + "%)");
                TotalWrite.WithCrlf(".");
            }

            return true;
        }

        ZipSum PrintExtSum(ZipSum arg, ConsoleWriter write)
        {
            write.WithoutCrlf(Int64ToText(arg.Size));
            write.WithoutCrlf(RatioToText(
                arg.CompressedSize, arg.Size));
            write.WithoutCrlf(DateToText(arg.Earliest));
            write.WithoutCrlf(TimeToText(arg.Earliest));
            write.WithoutCrlf(Int32ToText(arg.Count));
            write.WithoutCrlf(DateToText(arg.Lastest));
            write.WithoutCrlf(TimeToText(arg.Lastest));
            write.WithCrlf(arg.Name);
            return arg;
        }

        ZipSum MyAggregate(IEnumerable<ZipItem> qry, string zFilename)
        {
            if (IsSumExt)
            {
                return qry
                    .GroupBy(entry => Path.GetExtension(entry.Name))
                    .Select(grp => grp.Aggregate(new ZipSum(grp.Key),
                        (acc, curr) => acc.Add(curr)))
                    .OrderEntryBy(this)
                    .Select(sumExt => PrintExtSum(
                        sumExt, ItemWrite))
                    .Aggregate(new ZipSum(zFilename),
                        (acc, curr) => acc.Add(curr));
            }
            return qry
                .OrderEntryBy(this)
                .Select(entry => Print(entry))
                .Aggregate(new ZipSum(zFilename),
                    (acc, curr) => acc.Add(curr));
        }

        ZipItem Print(ZipItem arg)
        {
            ItemWrite.WithoutCrlf(Int64ToText(arg.Size));
            ItemWrite.WithoutCrlf(RatioToText(
                arg.CompressedSize, arg.Size));
            ItemWrite.WithoutCrlf(CrcToText(arg.Crc));
            ItemWrite.WithoutCrlf(DateToText(arg.LastModified));
            ItemWrite.WithoutCrlf(TimeToText(arg.LastModified));
            ItemWrite.WithoutCrlf(EncryptToText(arg.Encrypted));
            ItemWrite.WithCrlf(arg.Name);
            return arg;
        }

        readonly HashSet<String> HiddenFields = new HashSet<string>();
        readonly HashSet<String> ShowFields = new HashSet<string>();

        #region "Properties"
        public ConsoleWriter ItemWrite { get; private set; }
            = ConsoleWriter.Enable;

        public ConsoleWriter TotalWrite { get; private set; }
            = ConsoleWriter.Enable;

        public bool IsSumExt { get; private set; }

        Func<ZipSum, long, ZipSum> PrintTotal { get; set; }

        string sizeFormat = Helper.Absent;
        public Func<Int64, String> Int64ToText { get; private set; }

        string countFormat = Helper.Absent;
        public Func<Int32, String> Int32ToText { get; private set; }

        public Func<DateTime, String> DateToText { get; private set; }
        public Func<DateTime, String> TimeToText { get; private set; }

        public Func<Int64, Int64, String> RatioToText { get; private set; }
        public Func<Int32, String> CrcToText { get; private set; }
        public Func<bool, String> EncryptToText { get; private set; }

        public SortZipEntry SortComparer { get; private set; }
            = SortZipEntry.Nothing;

        public override IReadOnlyCollection<SwitchCfgSetup> SwitchSetups =>
            new SwitchCfgSetup[] {
                new SwitchCfgSetup('t',"total","Total only",
                    getter:() => false,
                    setter: () => {
                        CrcToText = _ => String.Empty;
                        ItemWrite = ConsoleWriter.Disable;
                    }),
                new SwitchCfgSetup('s',"sum","Sum by ext",
                    getter:() => false,
                    setter: () => {
                        CrcToText = _ => String.Empty;
                        EncryptToText = _ => String.Empty;
                        IsSumExt = true;
                    })
            };

        public override IReadOnlyCollection<ITypeCfgSetup> TypeSetups => 
            new ITypeCfgSetup[]{
                new ValueCfgFactory<string>(
                    arg => new Result<string>(arg),
                    arg => arg,
                    new ValueCfgSetup<string>[]
                    {
                        new ValueCfgSetup<string>(
                            "size","=short|comma|kilo",
                            () => sizeFormat, valThe => {
                                var val=valThe.ToLower();
                                switch (val)
                                {
                                    case "comma":
                                        Int64ToText = arg =>
                                            String.Format("{0,25:N0}",arg)+" ";
                                        sizeFormat = val;
                                        break;
                                    case "kilo":
                                        Int64ToText = arg => arg.SizeK()+" ";
                                        sizeFormat = val;
                                        break;
                                    default:
                                        Int64ToText = arg =>
                                            String.Format("{0,9}",arg)+" ";
                                        sizeFormat = Helper.Absent;
                                        break;
                                }
                            }),
                        new ValueCfgSetup<string>(
                            "count","=short|comma|kilo",
                            () => countFormat, valThe => {
                                var val=valThe.ToLower();
                                switch (val)
                                {
                                    case "comma":
                                        Int32ToText = arg =>
                                            String.Format("{0,13:N0}",arg)+" ";
                                        countFormat = val;
                                        break;
                                    case "kilo":
                                        Int32ToText = arg => arg.SizeK()+" ";
                                        countFormat = val;
                                        break;
                                    default:
                                        Int32ToText = arg =>
                                            String.Format("{0,5}",arg)+" ";
                                        countFormat = Helper.Absent;
                                        break;
                                }
                            }),
                        new ValueCfgSetup<string>(
                            "hide","=size,date,time",
                            () => String.Join(",",HiddenFields),
                            valThe => {
                                foreach (var val in valThe.Split(','))
                                {
                                    switch (val)
                                    {
                                        case "size":
                                            Int64ToText = _ => String.Empty;
                                            HiddenFields.Add(val);
                                            break;
                                        case "date":
                                            DateToText = _ => String.Empty;
                                            HiddenFields.Add(val);
                                            break;
                                        case "time":
                                            TimeToText = _ => String.Empty;
                                            HiddenFields.Add(val);
                                            break;
                                        default:
                                            throw new
                                            ArgumentOutOfRangeException(
                                                "'" + val + "' is bad 'hide' opt");
                                    }
                                }
                            }),
                        new ValueCfgSetup<string>
                            ( "show", "=ratio,crc,encrypt,all"
                            , () => String.Join(",",ShowFields)
                            ,  valThe => {
                                foreach (var val in valThe.ToLower().Split(','))
                                {
                                    switch (val)
                                    {
                                        case "ratio":
                                            RatioToText = (num,dem) => dem.ReducedRatio(num);
                                            ShowFields.Add(val);
                                            break;
                                        case "crc":
                                            CrcToText = arg => arg.ToString("X08")+" ";
                                            ShowFields.Add(val);
                                            break;
                                        case "encrypt":
                                            EncryptToText = arg => (arg ? "*" : " ");
                                            ShowFields.Add(val);
                                            break;
                                        case "all":
                                            RatioToText = (num,dem) => dem.ReducedRatio(num);
                                            CrcToText = arg => arg.ToString("X08")+ " ";
                                            EncryptToText = arg => (arg ? "*" : " ");
                                            ShowFields.Add(val);
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException(
                                                "'" + val + "' is bad 'show' opt");
                                    }
                                }
                            })
                            , new ValueCfgSetup<string>
                            ( "sort", "=name|date|size"
                            , () =>
                                (SortComparer==SortZipEntry.Nothing)
                                ? Helper.Absent : SortComparer.ToString()
                            , val => {
                                switch (val.ToLower())
                                {
                                    case "date":
                                        SortComparer = SortZipEntry.Date;
                                        break;
                                    case "datelast":
                                        SortComparer = SortZipEntry.DateLast;
                                        break;
                                    case "name":
                                        SortComparer = SortZipEntry.Name;
                                        break;
                                    case "size":
                                        SortComparer = SortZipEntry.Size;
                                        break;
                                    case "count":
                                        SortComparer = SortZipEntry.Count;
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException(
                                            "'" + val + "' is bad 'sort' opt");
                                }}
                            )
                    })
            };
        #endregion // Properties

        public ZipView(string helpHint): 
            base(helpHint, nameof(ZipView)+":")
        {
            #region "Properties Initialization"
            Int32ToText = arg => String.Format("{0,5} ", arg);
            Int64ToText = arg => String.Format("{0,9} ", arg);
            DateToText = arg => arg.ToString("yyyy-MM-dd ");
            TimeToText = arg => arg.ToString("HH:mm ");

            RatioToText = (_1, _2) => String.Empty;
            CrcToText = _ => String.Empty;
            EncryptToText = _ => String.Empty;

            PrintTotal = (arg,zSize) =>
            {
                TotalWrite.WithoutCrlf(Int64ToText(arg.Size));
                TotalWrite.WithoutCrlf(RatioToText(
                    zSize, arg.Size));
                if (CrcToText(0).Length > 0)
                { // ...................... 12345678+
                    TotalWrite.WithoutCrlf("-------- ");
                }
                TotalWrite.WithoutCrlf(DateToText(arg.Earliest));
                TotalWrite.WithoutCrlf(TimeToText(arg.Earliest));
                TotalWrite.WithoutCrlf(EncryptToText(false));
                TotalWrite.WithoutCrlf(DateToText(arg.Lastest));
                TotalWrite.WithoutCrlf(TimeToText(arg.Lastest));
                var cntText = Int32ToText(arg.Count).Trim();
                if (cntText.Length > 0)
                {
                    TotalWrite.WithoutCrlf("(count:" + cntText + ") ");
                }
                TotalWrite.WithCrlf(arg.Name);
                ItemWrite.WithCrlf("");
                return arg;
            };

            IsSumExt = false;
#endregion
        }
    }

    enum SumByZipEntry { Nothing, Ext, Dir };

    public enum SortZipEntry { Nothing, Name, Size, Date, DateLast, Count };

    static class ListEnvirHelper
    {
        static public string SizeK(this Int32 arg)
        {
            foreach (var Unit in new char[] { ' ', 'K', 'M' })
            {
                if (arg < 10000)
                {
                    return String.Format("{0,4}", arg) + Unit;
                }
                arg = (arg + 512) / 1024;
            }
            return String.Format("{0,4}", arg) + 'G';
        }

        static public string SizeK(this Int64 arg)
        {
            foreach (var Unit in new char[] { ' ', 'K', 'M' })
            {
                if (arg < 10000L)
                {
                    return String.Format("{0,4}", arg) + Unit;
                }
                arg = (arg + 512L) / 1024L;
            }
            return String.Format("{0,4}", arg) + 'G';
        }

        static public string ReducedRatio(this Int64 arg, Int64 num)
        {
            if (arg < 1L) return " 0 ";
            if (arg < num) return "-0 ";
            num = (arg - num);
            num *= 100L;
            num /= arg;
            if (num > 98L) return "99 ";
            if (num < 1L) return " 0 ";
            return String.Format("{0,2}",num)+ " ";
        }

        public static IEnumerable<ZipItem> OrderEntryBy(
            this IEnumerable<ZipItem> source, ZipView cmd)
        {
            switch (cmd.SortComparer)
            {
                case SortZipEntry.Size:
                    return source.OrderBy(item => item.Size);
                case SortZipEntry.Date:
                    return source.OrderBy(item => item.LastModified);
                case SortZipEntry.Name:
                    return source.OrderBy(item => item.Name);
                default:
                    return source;
            }
        }

        public static IEnumerable<ZipSum> OrderEntryBy(
            this IEnumerable<ZipSum> source, ZipView cmd)
        {
            switch (cmd.SortComparer)
            {
                case SortZipEntry.Size:
                    return source.OrderBy(entry => entry.Size);
                case SortZipEntry.Date:
                    return source.OrderBy(entry => entry.Earliest);
                case SortZipEntry.DateLast:
                    return source.OrderBy(entry => entry.Lastest);
                case SortZipEntry.Count:
                    return source.OrderBy(entry => entry.Count);
                case SortZipEntry.Name:
                    return source.OrderBy(entry => entry.Name);
                default:
                    return source;
            }
        }
    }
}
