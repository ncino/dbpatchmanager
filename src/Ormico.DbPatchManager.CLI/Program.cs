using System;
using CommandLine;
using Newtonsoft.Json;
using Ormico.DbPatchManager.Logic;
using Ormico.DbPatchManager.CLI.CommandLineOptions;

namespace Ormico.DbPatchManager.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            int rc = 0;
            try
            {
                rc = CommandLine.Parser.Default.ParseArguments(args, typeof(InitCmdLineOptions),
                        typeof(AddPatchCmdLineOptions), typeof(BuildCmdLineOptions))
                    .MapResult(
                        (InitCmdLineOptions o) => InitBuildSettings(o),
                        (AddPatchCmdLineOptions o) => AddPatch(o),
                        (BuildCmdLineOptions o) => Build(o),
                        err => 1
                    );
            }
            catch (JsonException jsonException)
            {
                Console.WriteLine("{0}", jsonException.Message);
                Console.WriteLine("{0}", jsonException.StackTrace);
                if (jsonException.InnerException != null)
                {
                    Console.WriteLine("{0}", jsonException.InnerException.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"{ex.InnerException.Message}");
                }
                rc = -1;
            }

            return rc;
        }

        private const string _patchFileName = "patches.json";
        private const string _patchLocalFileName = "patches.local.json";

        public static bool StrEq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        static int InitBuildSettings(InitCmdLineOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            //todo: pass all settings
            manager.InitConfig(new InitOptions() { DbType = options.DbType });

            return rc;
        }

        static int AddPatch(AddPatchCmdLineOptions options)
        {
            int rc = 0;
            var startTime = DateTimeOffset.Now;
            Console.WriteLine("{0:O} - Database Add Patch Started", startTime);
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            //todo: pass all settings
            manager.AddPatch(options.Name, new PatchOptions()
            {
            });
            var endTime = DateTimeOffset.Now;
            Console.WriteLine("{0:O} - Database Add Patch Completed", endTime);
            Console.WriteLine("{0:g} - Add Patch Time", endTime.Subtract(startTime));
            return rc;
        }

        static int Build(BuildCmdLineOptions options)
        {
            int rc = 0;
            var startTime = DateTimeOffset.Now;
            Console.WriteLine("{0:O} - Database Build Started", startTime);
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            manager.Build();
            var endTime = DateTimeOffset.Now;
            Console.WriteLine("{0:O} - Database Build Completed", endTime);
            Console.WriteLine("{0:g} - Build Time", endTime.Subtract(startTime));
            return rc;
        }
    }
}
