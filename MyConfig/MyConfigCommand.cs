using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace My.Config
{
    public abstract class CommandConfig : ICommandConfig
    {
        protected readonly string HelpHint;
        protected readonly string CfgPrefix;

        public abstract IReadOnlyCollection<SwitchCfgSetup> SwitchSetups { get; }

        public abstract IReadOnlyCollection<ITypeCfgSetup> TypeSetups { get; }

        private CommandConfig() { }

        public CommandConfig(string helpHint, string cfgPrefix)
        {
            CfgPrefix = cfgPrefix;
            HelpHint = helpHint;
        }

        public abstract bool Apply(IReadOnlyCollection<string> opts);

        public string Help()
        {
            var rtn = new StringBuilder();
            rtn.AppendLine(HelpHint);
            var help = SwitchSetups.Help();
            if (!String.IsNullOrEmpty(help))
            {
                rtn.AppendLine("Switch options:");
                rtn.AppendLine(help);
            }
            help = TypeSetups.Help();
            if (!String.IsNullOrEmpty(help))
            {
                rtn.AppendLine("Value options:");
                rtn.AppendLine(help);
            }
            rtn.AppendLine("Config file option:");
            // .............12-c,_
            rtn.AppendLine("      --cfg-off=[exe|private|all]");
            rtn.AppendLine("      --cfg-save=[exe|private]");
            rtn.AppendLine("      --cfg-show");
            return rtn.ToString();
        }

        public IEnumerable<string> ParseArgs(
            IReadOnlyCollection<string> theArgs)
        {
            bool _isLoadExeCfg = true;
            bool _isLoadPrivateCfg = true;
            ConfigFileType _saveCfgTo = ConfigFileType.Nothing;
            Action<string> _cfgSetterLog = _ => { };

            IEnumerable<string> ParseArgsFirstPhase(
                IReadOnlyCollection<string> firstPhaseArgs)
            {
                foreach (var arg in firstPhaseArgs)
                {
                    if (new string[] { "-?", "-h", "--help" }
                        .Contains(arg))
                    {
                        Console.WriteLine(Help());
                        throw new OnlineHelpShown();
                    }
                    else if (arg.StartsWith("--cfg-off="))
                    {
                        switch (arg)
                        {
                            case "--cfg-off=exe":
                                _isLoadExeCfg = false;
                                break;
                            case "--cfg-off=private":
                                _isLoadPrivateCfg = false;
                                break;
                            case "--cfg-off=all":
                                _isLoadExeCfg = false;
                                _isLoadPrivateCfg = false;
                                break;
                            default:
                                break;
                        }
                    }
                    else if (arg.StartsWith("--cfg-save="))
                    {
                        switch (arg)
                        {
                            case "--cfg-save=exe":
                                _saveCfgTo = ConfigFileType.ExeCfg;
                                break;
                            case "--cfg-save=private":
                                _saveCfgTo = ConfigFileType.PrivateCfg;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (arg)
                        {
                            case "--cfg-show":
                                _cfgSetterLog =
                                    msg => Console.WriteLine(msg);
                                _cfgSetterLog("Show config:");
                                break;
                            default:
                                yield return arg;
                                break;
                        }
                    }
                }
            }

            IEnumerable<string> ParseArgsSecondPhase(
                IReadOnlyCollection<string> secondPhaseArgs)
            {
                var enumThe = secondPhaseArgs.GetEnumerator();
                while (enumThe.MoveNext())
                {
                    var curr = enumThe.Current;
                    if (SwitchSetups.ValueAssignment(curr))
                    {
                        continue;
                    }
                    var anyFound = TypeSetups
                        .Where(setup => setup.ValueAssignment(
                            curr, enumThe))
                        .Take(1);
                    if (!anyFound.Any())
                    {
                        yield return curr;
                    }
                }
            }

            int LoadConfig(string cfgFilename,
                KeyValueConfigurationCollection setting)
            {
                var cntLoad = SwitchSetups.LoadCfg(
                    CfgPrefix, setting, _cfgSetterLog);
                cntLoad += TypeSetups.Aggregate(0,
                    (acc, setup) => acc + setup.LoadCfg(
                        CfgPrefix, setting, _cfgSetterLog));
                _cfgSetterLog(cfgFilename + " contains "
                    + cntLoad.ToString() + " values.");
                return cntLoad;
            }

            int SaveConfig(KeyValueConfigurationCollection setting)
            {
                var cntSave = SwitchSetups.SaveCfg(
                    CfgPrefix, setting);
                cntSave += TypeSetups.Aggregate(0,
                    (curr, setup) => curr + setup.SaveCfg(
                        CfgPrefix, setting));
                return cntSave;
            }

            var rsltFirstPhase = ParseArgsFirstPhase(theArgs).ToArray();

            var exeCfg = ConfigFileType.ExeCfg.GetConfiguration();
            var appExeSetting = exeCfg.AppSettings.Settings;
            if (_isLoadExeCfg)
            {
                LoadConfig($"ExeConfig '{exeCfg.FilePath}'",
                    appExeSetting);
            }

            var privateCfg = ConfigFileType.PrivateCfg
                .GetConfiguration();
            var appPrivateSetting = privateCfg.AppSettings.Settings;
            if (_isLoadPrivateCfg)
            {
                LoadConfig($"PrivateConfig '{privateCfg.FilePath}'",
                    appPrivateSetting);
            }

            var rtn = ParseArgsSecondPhase(rsltFirstPhase).ToList();

            if (_saveCfgTo == ConfigFileType.ExeCfg)
            {
                SaveConfig(appExeSetting);
                exeCfg.Save(ConfigurationSaveMode.Modified);
            }
            else if (_saveCfgTo == ConfigFileType.PrivateCfg)
            {
                SaveConfig(appPrivateSetting);
                privateCfg.Save(ConfigurationSaveMode.Modified);
            }

            return rtn;
        }
    }
}