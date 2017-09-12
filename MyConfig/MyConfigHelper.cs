using System;
using System.Collections.Generic;
using System.Configuration; // Ref: System.Configuration.dll
using System.IO;

namespace My.Config
{
    class OnlineHelpShown : Exception
    {
    }

    public class Result<T>
    {
        public readonly Exception Exception;
        public readonly bool Succeeded;
        public readonly T Value;

        private Result(int _1, int _2) { Succeeded = false; }
        public static Result<T> False = new Result<T>(0, 0);

        public Result(T value)
        {
            Succeeded = true;
            Value = value;
        }

        public Result(Exception exception)
        {
            Succeeded = false;
            Exception = exception;
        }

        private Result(bool _1, bool _2)
        {
            Succeeded = false;
        }

        public static Result<T> Failure =
            new Result<T>(false, false);
    }

    public enum ConfigFileType
    {
        ExeCfg,
        PrivateCfg,
        Nothing
    }

    public static class ConfigFileTypeExtensions
    {
        public static Configuration GetConfiguration(
            this ConfigFileType arg)
        {
            switch (arg)
            {
                case ConfigFileType.ExeCfg:
                    return ConfigurationManager
                        .OpenExeConfiguration(
                        ConfigurationUserLevel.None);
                case ConfigFileType.PrivateCfg:
                    var usrAppCfgDir = Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData);
                    var exeFilename = Path.GetFileName(
                        System.Reflection.Assembly
                        .GetEntryAssembly().Location);
                    var usrCfgFilename = Path.Combine(usrAppCfgDir,
                        exeFilename + ".config");
                    var exeMap = new ExeConfigurationFileMap()
                    {
                        ExeConfigFilename = usrCfgFilename
                    };
                    return ConfigurationManager.OpenMappedExeConfiguration(
                        exeMap, ConfigurationUserLevel.None);
                default:
                    throw new ArgumentOutOfRangeException(
                        "ConfigFileType");
            }
        }
    }

    static class Helper
    {
        public static string Absent { get { return "*absent"; } }
        public static char CfgFileOnly = '-';
        public static char NonPrintable = ' ';
        public static bool IsPrintableShortForm(char arg)
        {
            return arg > NonPrintable;
        }

        public static IEnumerable<string> SplitOptions(
            this IReadOnlyCollection<string> args)
        {
            foreach (var arg in args)
            {
                if ((arg == "-") || (arg.StartsWith("--")))
                {
                    yield return arg;
                }
                else if (arg.StartsWith("-"))
                {
                    foreach (var argCh in arg.Substring(1))
                    {
                        yield return "-" + argCh;
                    }
                }
                else
                {
                    yield return arg;
                }
            }
        }
    }

    public interface IConfigable<T>
    {
        Func<T> Getter { get; }
        T GetValue(string cfgPrefix,
            KeyValueConfigurationCollection cfg);
    }

    public interface IMyCommandConfig
    {
        string Help();
        IReadOnlyCollection<SwitchCfgSetup> SwitchSetups { get; }
        IReadOnlyCollection<ITypeCfgSetup> TypeSetups { get; }
        IEnumerable<string> ParseArgs(IReadOnlyCollection<string> args);
        bool Apply(IReadOnlyCollection<string> args);
    }

    public interface IMyCommandEnvir
    {
        string Help();
        string[] ParseArgs(IReadOnlyCollection<string> args);
        IMyCommandConfig InnerConfig();
    }
}