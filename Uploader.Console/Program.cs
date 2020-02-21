using System;
using System.Diagnostics;
using System.Reflection;
using Serilog;

namespace Uploader.Console
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using (var log = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .Enrich.WithProperty("Application", Assembly.GetExecutingAssembly().GetName().Name)
                .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
                .Enrich.WithProperty("Identifier", Process.GetCurrentProcess().Id)
                .CreateLogger())
            {
                if (args.Length == 0)
                {
                    log.Fatal("Must supply one or more job names to perform upload, see jobs.json.");
                    throw new ArgumentNullException();
                }
                else
                {
                    foreach (string arg in args)
                    {
                        log.Verbose("Processing {arg}...", arg);
                        var uploader = new Uploader(log);
                        try
                        {
                            uploader.Process(arg);
                        }
                        catch (Exception ex)
                        {
                            log.Fatal(ex, "Fatal exception: {ErrorMessage}", ex.Message);
                        }
                    }
                }
            }
        }
    }
}
