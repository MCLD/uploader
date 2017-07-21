using Serilog;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Uploader.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var log = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .Enrich.WithProperty("Application", Assembly.GetExecutingAssembly().GetName().Name)
                .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
                .Enrich.WithProperty("Identifier", Process.GetCurrentProcess().Id)
                .CreateLogger())
            {
                if (args.Count() == 0)
                {
                    log.Fatal("Must supply one or more job names to perform upload.");
                    throw new ArgumentNullException();
                }
                else
                {
                    var settings = ConfigurationManager.AppSettings;
                    foreach (string arg in args)
                    {
                        log.Verbose("Processing {arg}...", arg);
                        var uploader = new Uploader(log, settings);
                        try
                        {
                            uploader.Process(arg);
                        }
                        catch (Exception ex)
                        {
                            log.Fatal(ex, "Fatal exception");
                        }
                    }
                }
            }
        }
    }
}
