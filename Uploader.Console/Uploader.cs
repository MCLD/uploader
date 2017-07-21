using Serilog;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.IO.Compression;
using Newtonsoft.Json;

namespace Uploader.Console
{
    public class Uploader
    {
        private readonly ILogger _log;
        private readonly NameValueCollection _config;

        public Uploader(ILogger log, NameValueCollection config)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _config = config ?? throw new ArgumentException(nameof(config));
        }

        public void Process(string jobName)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (string.IsNullOrWhiteSpace(jobName))
            {
                _log.Error("No detail name provided, nothing to do.");
                throw new ArgumentNullException(nameof(jobName));
            }

            UploadJob job = null;
            using (var configurationJsonFile = File.OpenText(@"jobs.json"))
            {
                var serializer = new JsonSerializer();
                var jobsFile = (JobsFile)serializer.Deserialize(configurationJsonFile,
                    typeof(JobsFile));

                if (jobsFile == null || jobsFile.Jobs.Count() == 0)
                {
                    _log.Error("Unable to read configuration file configuration.json");
                    throw new Exception("Unable to read configuration file configuration.json");
                }

                job = jobsFile.Jobs.Where(_ => _.Name == jobName).SingleOrDefault();
            }

            if (job == null)
            {
                _log.Error("Details for {jobName} could not be found.",
                    jobName);
                throw new Exception($"Unable to find upload details for job ${jobName}");
            }

            if (string.IsNullOrEmpty(job.Site))
            {
                _log.Error("Site is not specified for job: {Name}", job.Name);
                return;
            }

            if (!job.Path.Contains('*'))
            {
                if (!File.Exists(job.Path))
                {
                    _log.Error("Could not find file to upload at: {Path}", job.Path);
                    return;
                }
            }
            else
            {
                var directory = job.Path
                    .Substring(0, job.Path.LastIndexOf(Path.DirectorySeparatorChar));
                var file = job.Path
                    .Substring(job.Path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                var files = Directory.GetFiles(directory, file);

                switch(files.Count())
                {
                    case 0:
                        _log.Error("Could not find a file matching {detail.Path}", job.Path);
                        return;
                    case 1:
                        _log.Information("Found match for {0}: {1}", job.Path, files[0]);
                        job.Path = files[0];
                        break;
                    default:
                        _log.Error("Found multiple files matching {detail.Path}", job.Path);
                        return;
                }
            }

            string tempFile = null;
            try
            {
                if (job.ZipBeforeUpload)
                {
                    tempFile = Path.GetTempFileName();
                    _log.Debug("Zipping to temporary file {tempFile}", tempFile);
                    using (var fs = new FileStream(tempFile, FileMode.Create))
                    {
                        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
                        {
                            archive.CreateEntryFromFile(job.Path,
                                Path.GetFileName(job.Path));
                        }
                    }

                    _log.Information("Zipped {0} from {1}b to {2}b",
                        Path.GetFileName(job.Path),
                        new FileInfo(job.Path).Length,
                        new FileInfo(tempFile).Length);
                    job.Path = tempFile;
                }

                using (var client = new WebClient())
                {
                    if (!string.IsNullOrEmpty(job.Username)
                        && !string.IsNullOrEmpty(job.Password))
                    {
                        _log.Verbose("Using username {Username}", job.Username);
                        client.Credentials = new NetworkCredential(job.Username, job.Password);
                    }

                    if (job.Site.Contains("{date}"))
                    {
                        job.Site = job.Site.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
                    }

                    var response = client.UploadFile(job.Site, "STOR", job.Path);

                    var responseText = Encoding.UTF8.GetString(response);
                    if (responseText.Length > 0)
                    {
                        _log.Information("Server response: {responseText}", responseText);
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
                {
                    _log.Debug("Deleting temporary file {tempFile}", tempFile);
                    File.Delete(tempFile);
                }
            }

            if (!string.IsNullOrEmpty(job.EmailTo)
                && !string.IsNullOrEmpty(job.EmailSubject))
            {
                using (var message = new MailMessage())
                {
                    message.To.Add(job.EmailTo);
                    message.Subject = job.EmailSubject;
                    message.Body = job.EmailBody;

                    if (!string.IsNullOrEmpty(job.EmailBcc))
                    {
                        message.Bcc.Add(job.EmailBcc);
                    }

                    using (var mailClient = new SmtpClient())
                    {
                        try
                        {
                            mailClient.Send(message);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Sending mail for upload {Name} failed: {Message}",
                                job.Name,
                                ex.Message);
                        }
                    }
                }
            }

            sw.Stop();
            _log.Information("Finished processing {jobName} in {TotalSeconds:N2} seconds",
                jobName,
                sw.Elapsed.TotalSeconds);
        }
    }
}
