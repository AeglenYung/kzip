using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration; // Ref: System.Configuration.dll
using System.Text;

namespace My.Config
{
    public class ValueSetup<T> where T : IComparable
    {
        readonly public char ShortForm;
        readonly public string Name;
        readonly public string Help;
        readonly public Action<T> Setter;

        public ValueSetup(char shortForm, string name,
            string help, Action<T> setter)
        {
            ShortForm = shortForm;
            Name = name;
            Help = help;
            Setter = setter;
        }

        public ValueSetup(string name,
            string help, Action<T> setter)
        {
            ShortForm = Helper.NonPrintable;
            Name = name;
            Help = help;
            Setter = setter;
        }
    }

    public interface ITypeSetup
    {
        string Help();
        bool ValueAssignment(string arg,
            IEnumerator<string> args);
    }

    public class ValueFactory<T> : ITypeSetup
        where T : IComparable
    {
        public readonly Func<string, Result<T>> TryParse;
        public readonly IReadOnlyCollection<ValueSetup<T>> Setups;

        public ValueFactory(
            Func<string, Result<T>> tryParse,
            IReadOnlyCollection<ValueSetup<T>> setups)
        {
            TryParse = tryParse;
            Setups = setups;
        }

        public ValueFactory(
            Func<string, Result<T>> tryParse, 
            ValueSetup<T> setup)
        {
            TryParse = tryParse;
            Setups = new ValueSetup<T>[1] { setup };
        }

        public string Help()
        {
            var rtn = new StringBuilder();
            foreach (var setup in Setups
                .Where(setup => setup.ShortForm!=Helper.CfgFileOnly))
            {
                if (Helper.IsPrintableShortForm(setup.ShortForm))
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

        public bool ValueAssignment(string arg, 
            IEnumerator<string> args)
        {
            if (arg.StartsWith("--"))
            {
                var anyFound2 = Setups
                    .Where(setup => 
                        setup.ShortForm != Helper.CfgFileOnly &&
                        arg.StartsWith("--" + setup.Name + "="));
                if (!anyFound2.Any()) return false;
                var setupThe = anyFound2.First();
                var result = TryParse(arg.Substring(
                    3 + setupThe.Name.Length));
                if (result.Succeeded)
                {
                    try
                    {
                        setupThe.Setter(result.Value);
                    }
                    catch (ArgumentOutOfRangeException ee)
                    {
                        Console.Error.WriteLine(ee.Message);
                    }
                }
                return true;
            }

            if (arg.StartsWith("-") && (arg.Length == 2))
            {
                var anyFound1 = Setups
                    .Where(setup => arg[1] == setup.ShortForm);
                if (!anyFound1.Any()) return false;
                var setupThe = anyFound1.First();
                if (!args.MoveNext())
                {
                    throw new ArgumentException(String.Format(
                        "Value is NOT found for '" + setupThe.Name + "'"));
                }
                var result = TryParse(args.Current);
                if (result.Succeeded)
                {
                    try
                    {
                        setupThe.Setter(result.Value);
                    }
                    catch (ArgumentOutOfRangeException ee)
                    {
                        Console.Error.WriteLine(ee.Message);
                    }
                }
                return true;
            }

            return false;
        }
    }

    public class ValueCfgSetup<T> : ValueSetup<T>
        where T : IComparable
    {
        readonly public Func<T> Getter;

        public ValueCfgSetup(char shortForm, string name,
            string help, Func<T> getter, Action<T> setter)
            : base(shortForm, name, help, setter)
        {
            Getter = getter;
        }

        public ValueCfgSetup(string name,
            string help, Func<T> getter, Action<T> setter)
            : base(name, help, setter)
        {
            Getter = getter;
        }
    }

    public interface ITypeCfgSetup: ITypeSetup
    {
        int LoadCfg(string cfgPrefix,
            KeyValueConfigurationCollection cfg,
            Action<string> log);
        int SaveCfg(string cfgPrefix,
            KeyValueConfigurationCollection cfg);
    }

    public class ValueCfgFactory<T> : ValueFactory<T>, ITypeCfgSetup
        where T: IComparable
    {
        public readonly Func<T, string> ToText;

        public ValueCfgFactory(
            Func<string, Result<T>> tryParse,
            Func<T,string> toText,
            IReadOnlyCollection<ValueCfgSetup<T>> setups)
            : base(tryParse,setups)
        {
            ToText = toText;
        }

        public ValueCfgFactory(
            Func<string, Result<T>> tryParse,
            Func<T, string> toText, 
            ValueCfgSetup<T> setup)
            : base(tryParse,setup)
        {
            ToText = toText;
        }

        public int LoadCfg( string cfgPrefix,
            KeyValueConfigurationCollection cfg, Action<string> log)
        {
            int rtn = 0;
            foreach (var setup in Setups)
            {
                var elm = cfg[cfgPrefix+ setup.Name];
                if (elm == null) continue;
                try
                {
                    var result = TryParse(
                        System.Net.WebUtility.HtmlDecode(
                        elm.Value));
                    if (result.Succeeded)
                    {
                        setup.Setter(result.Value);
                        log(String.Format("\t {0} is '{1}'",
                            setup.Name, ToText(result.Value)));
                        rtn += 1;
                    }
                }
                catch (Exception ee)
                {
                    Console.Error.WriteLine(
                        String.Format(ee.Message));
                }
            }
            return rtn;
        }

        public int SaveCfg(string cfgPrefix,
            KeyValueConfigurationCollection cfg)
        {
            int rtn = 0;
            foreach (ValueCfgSetup<T> setup in Setups)
            {
                var currText = ToText(setup.Getter());
                cfg.Remove(cfgPrefix+ setup.Name);
                if ((String.IsNullOrEmpty(currText)) || 
                    (Helper.Absent == currText))
                {
                    continue;
                }
                cfg.Add(cfgPrefix+ setup.Name,
                    System.Net.WebUtility.HtmlEncode( currText));
                rtn += 1;
            }
            return rtn;
        }
    }

    public static class FactoryHelper
    {
        public static string Help(
            this IReadOnlyCollection<ITypeSetup> setups)
        {
            var rtn = new StringBuilder();
            foreach (var setup in setups)
            {
                rtn.Append(setup.Help());
            }
            return rtn.ToString();
        }
    }
}
