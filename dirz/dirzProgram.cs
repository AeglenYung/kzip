using System;
using System.Linq;

namespace dirz
{
    class DirzProgram
    {
        static void Main(string[] args)
        {
            try
            {
                RunMain(args);
            }
            catch (My.Config.OnlineHelpShown)
            {

            }
            catch (Exception ee)
            {
                Console.WriteLine();
                Console.WriteLine(ee.ToString());
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("Press any key ");
                Console.ReadKey();
            }
        }

        static void RunMain(string[] cmdArgs)
        {
            const string syntax = "dirz [opt ..] zipFile [..]";
            var envr = new DirzConfig(syntax);
            var args = envr.ParseArgs(cmdArgs);
            if (args.Count()==0)
            {
                Console.WriteLine(syntax+"\ndirz -?");
                return;
            }
            envr.Apply(args.ToArray());
        }
    }
}
