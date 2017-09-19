using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Config
{
    public struct CommandSetup
    {
        public char ShortForm { get; private set; }
        public string Name { get; private set; }
        public Func<ICommandConfig> Getter { get; private set; }

        public CommandSetup(char shortFrom, string name,
            Func<ICommandConfig> getter)
        {
            ShortForm = shortFrom;
            Name = name;
            Getter = getter;
        }
    }

    public class ConfigEnvir : ICommandEnvir
    {
        protected readonly IReadOnlyCollection<CommandSetup> CommandSetups;
        protected readonly IReadOnlyCollection<SwitchSetup> SwitchSetups;
        protected readonly IReadOnlyCollection<ITypeSetup> TypeSetups;
        protected CommandSetup? _commandSetup = null;

        public readonly string HelpHint;

        public ConfigEnvir(string helpHint,
            IReadOnlyCollection<CommandSetup> commandSetups,
            IReadOnlyCollection<SwitchSetup> switchSetups,
            IReadOnlyCollection<ITypeSetup> typeSetups)
        {
            HelpHint = helpHint;
            CommandSetups = commandSetups;
            SwitchSetups = switchSetups;
            TypeSetups = typeSetups;
        }

        public string Help()
        {
            var rtn = new StringBuilder();
            rtn.AppendLine(HelpHint);
            rtn.AppendLine("Commands:");
            rtn.AppendLine(CommandSetups.Help());
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
            return rtn.ToString();
        }

        public Tuple<ICommandConfig, string[]> ParseArgs(
            IReadOnlyCollection<string> args)
        {
            bool isShowHelp = false;
            IEnumerable<string> ParseArgsFirstPhase(
                IReadOnlyCollection<string> firstPhaseArgs)
            {
                var enumThe = firstPhaseArgs.SplitOptions()
                    .GetEnumerator();
                while (enumThe.MoveNext())
                {
                    var curr = enumThe.Current;

                    if (new string[] { "-?", "-h", "--help" }
                        .Contains(curr))
                    {
                        isShowHelp = true;
                        continue;
                    }

                    if (curr=="--version")
                    {
                        Console.WriteLine(VersionText());
                        throw new OnlineHelpShown();
                    }

                    var setupFound = CommandSetups.Find(curr);
                    if (setupFound.HasValue)
                    {
                        var found = setupFound.Value;
                        if (_commandSetup.HasValue)
                        {
                            var setupThe = _commandSetup.Value;
                            Console.WriteLine(
                                "Command line should contain one command but"
                                + " command -"+ setupThe.ShortForm
                                + " and command -" + found.ShortForm
                                + " are found!");
                            throw new OnlineHelpShown();
                        }
                        _commandSetup = setupFound;
                        continue;
                    }

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

            var p1array = ParseArgsFirstPhase(args).ToArray();

            if (isShowHelp)
            {
                if (_commandSetup.HasValue)
                {
                    Console.WriteLine(_commandSetup.Value.Getter().Help());
                }
                else
                {
                    Console.WriteLine(Help());
                }
                throw new OnlineHelpShown();
            }

            if (!_commandSetup.HasValue)
            {
                Console.WriteLine("Command is required!");
                Console.WriteLine(HelpHint);
                throw new OnlineHelpShown();
            }
            var cmd = _commandSetup.Value.Getter();
            var p2array = cmd.ParseArgs(p1array).ToArray();
            return new Tuple<ICommandConfig, string[]>(cmd, p2array);
        }

        private string VersionText()
        {
            var rtn = new StringBuilder();
            var exeAssembly = System.Reflection.Assembly
                .GetEntryAssembly();
            var infoExe = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                exeAssembly.Location);
            var version = infoExe.FileVersion;
            rtn.AppendFormat("{0} (Version: {1})\n",
                System.IO.Path.GetFileNameWithoutExtension(
                    exeAssembly.Location), version);
            rtn.AppendLine(infoExe.CompanyName);
            rtn.AppendLine(infoExe.LegalCopyright);
            rtn.AppendLine(infoExe.Comments);
            return rtn.ToString();
        }
    }

    public static class EnvirHelper
    {
        public static string Help(
            this IReadOnlyCollection<CommandSetup> setups)
        {
            var rtn = new StringBuilder();
            foreach (var setup in setups)
            {
                rtn.Append($"  -{setup.ShortForm}, ");
                rtn.AppendLine($"--{setup.Name}");
            }
            return rtn.ToString();
        }

        public static CommandSetup? Find(
            this IReadOnlyCollection<CommandSetup> setups,
            string arg)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ISwitchSetup[].ValueAssigment='{arg}'");
            var cmdFound = setups
                .Where(setup => setup.ShortForm != Helper.CfgFileOnly)
                .Where(setup =>
                    arg.Equals("--" + setup.Name) ||
                    arg.Equals("-" + setup.ShortForm))
                .ToArray();
            if (cmdFound.Length!=1)
            {
                return null;
            }
            return cmdFound[0];
        }
    }
}