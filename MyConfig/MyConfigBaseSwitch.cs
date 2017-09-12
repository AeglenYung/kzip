using System;
using System.Collections.Generic;
using System.Configuration; // Ref: System.Configuration.dll
using System.Linq;
using System.Text;

namespace My.Config
{
    public interface ISwitchSetup
    {
        char ShortForm { get; }
        string Name { get; }
        string Help { get; }
        Action Setter { get; }
    }

    public class SwitchSetup : ISwitchSetup
    {
        public char ShortForm { get; private set; }
        public string Name { get; private set; }
        public string Help { get; private set; }
        public Action Setter { get; private set; }

        public SwitchSetup(char shortFrom, string name,
            string help, Action setter)
        {
            ShortForm = shortFrom;
            Name = name;
            Help = help;
            Setter = setter;
        }

        public SwitchSetup(string name,
            string help, Action setter)
        {
            ShortForm = Helper.NonPrintable;
            Name = name;
            Help = help;
            Setter = setter;
        }
    }

    public class SwitchCfgSetup : SwitchSetup, IConfigable<bool>
    {
        public Func<bool> Getter { get; }

        public SwitchCfgSetup(char shortFrom, string name,
            string help, Func<bool> getter, Action setter)
            : base(shortFrom,name,help,setter)
        {
            Getter = getter;
        }

        public SwitchCfgSetup(string name,
            string help, Func<bool> getter, Action setter)
            : base(Helper.NonPrintable, name, help, setter)
        {
            Getter = getter;
        }

        public bool GetValue(string cfgPrefix,
            KeyValueConfigurationCollection cfg)
        {
            var elm = cfg[cfgPrefix+Name];
            if (elm == null)
            {
                return false;
            }
            var valueThe = elm.Value.ToLower();
            return (valueThe == "yes") || (valueThe == "true");
        }
    }

    public static class SwitchHelper
    {
        public static string Help(
            this IReadOnlyCollection<ISwitchSetup> setups)
        {
            var rtn = new StringBuilder();
            foreach (var setup in setups
                .Where(setup => Helper.CfgFileOnly!=setup.ShortForm)
                )
            {
                //if (Helper.IsPrintableShortForm(setup.ShortForm))
                var a1 = Helper.IsPrintableShortForm(setup.ShortForm);
                if (a1)
                {
                    rtn.Append($"\t -{setup.ShortForm}, ");
                }
                else
                {
                    // ........ \t -c,_
                    rtn.Append("\t     ");
                }
                rtn.AppendLine($"--{setup.Name}\t {setup.Help}");
            }
            return rtn.ToString();
        }

        public static bool ValueAssignment(
            this IReadOnlyCollection<ISwitchSetup> setups,
            string arg)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ISwitchSetup[].ValueAssigment='{arg}'");
            var anySetupFound = setups
                .Where(setup => setup.ShortForm != Helper.CfgFileOnly)
                .Where(setup =>
                    arg.Equals("--" + setup.Name) ||
                    arg.Equals("-" + setup.ShortForm));
            if (anySetupFound.Any())
            {
                anySetupFound.First().Setter();
                return true;
            }
            return false;
        }

        public static int LoadCfg(
            this IReadOnlyCollection<SwitchCfgSetup> setups,
            string cfgPrefix,
            KeyValueConfigurationCollection cfg,
            Action<string> logger)
        {
            int rtn = 0;
            foreach (var setup in setups)
            {
                if (setup.GetValue(cfgPrefix,cfg))
                {
                    logger("\t" + setup.Name + " is YES");
                    setup.Setter();
                    rtn += 1;
                }
            }
            return rtn;
        }

        public static int SaveCfg(
            this IReadOnlyCollection<SwitchCfgSetup> setups,
            string cfgPrefix,
            KeyValueConfigurationCollection cfg)
        {
            int rtn = 0;
            foreach (var setup in setups)
            {
                if (!setup.Getter()) continue;
                // TODO: add assembly.Name as prefix
                cfg.Remove(cfgPrefix+setup.Name);
                cfg.Add(cfgPrefix+setup.Name, "yes");
                rtn += 1;
            }
            return rtn;
        }
    }
}