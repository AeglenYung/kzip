using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Config
{
    public class MyConfigEnvir : IMyCommandEnvir
    {
        protected readonly IReadOnlyCollection<ISwitchSetup> SwitchSetups;
        protected readonly IReadOnlyCollection<ITypeSetup> TypeSetups;
        protected static IMyCommandConfig innerConfig = null;

        public readonly string HelpHint;

        public MyConfigEnvir(string helpHint,
            IReadOnlyCollection<ISwitchSetup> switchSetups,
            IReadOnlyCollection<ITypeSetup> typeSetups)
        {
            HelpHint = helpHint;
            SwitchSetups = switchSetups;
            TypeSetups = typeSetups;
        }

        public static void MakeCommand<T>(string helpHint)
            where T : IMyCommandConfig
        {
            if (innerConfig != null)
            {
                throw new ArgumentException(
                    "Command option must be single!");
            }
            innerConfig = (IMyCommandConfig)Activator
                .CreateInstance(typeof(T), helpHint); // , this);
        }

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
            return rtn.ToString();
        }

        public string[] ParseArgs(
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
                var help = (InnerConfig() == null)
                    ? Help() : InnerConfig().Help();
                Console.WriteLine(help);
                throw new OnlineHelpShown();
            }

            if (InnerConfig() != null)
            {
                return InnerConfig().ParseArgs(p1array).ToArray();
            }

            return p1array;
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

        public IMyCommandConfig InnerConfig()
        {
            return innerConfig;
        }
    }
}