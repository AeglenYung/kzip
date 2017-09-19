/*
kzip - A command line tool for Zip file
Copyright (C) 2017, Yung, Ck Aeglen. (https://github.com/AeglenYung/kzip)
BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above
      copyright notice, this list of conditions and the following
      disclaimer in the documentation and/or other materials provided
      with the distribution.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using My.Config;
using System.Linq;

namespace kzip
{
    class ZipProgram
    {
        static void Main(string[] args)
        {
            try
            {
                RunMain(args);
            }
            catch (OnlineHelpShown)
            {
                // do nothing ..
            }
            catch (Ionic.Zip.BadPasswordException)
            {
                Console.WriteLine("No passowrd is given or input password is incorrect.");
            }
            catch (Exception ee)
            {
                Console.WriteLine();
                Console.WriteLine(ZipEnvir.Debug
                    ? ee.ToString()
                    : ee.GetType().Name+ ": "+ ee.Message);
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("Press any key ");
                Console.ReadKey();
            }
        }

        private static void RunMain(string[] args)
        {
            var envir = new ZipEnvir(nameof(kzip));
            var parseRtn = envir.ParseArgs(args);
            var theArgs = parseRtn.Item2;
            var badArgs = theArgs.Where(arg => arg.StartsWith("-"));
            if (badArgs.Any())
            {
                Console.WriteLine("Help syntax:");
                Console.WriteLine("\t"+ nameof(kzip)+ " -?");
                Console.WriteLine();
                Console.WriteLine("Unknown options:");
                foreach (var arg in badArgs)
                    Console.WriteLine("\t"+arg);
                return;
            }

            if (String.IsNullOrEmpty(ZipEnvir.ZipFilename))
            {
                Console.WriteLine("Filename is required!");
                return;
            }
            var cmdThe = parseRtn.Item1;
            cmdThe.Apply(theArgs);
        }
    }
}
