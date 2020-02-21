using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace Uploader.Console
{
    public class Uploader
    {
        private readonly ILogger _log;

        public Uploader(ILogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Process(string jobName)
        {
            long start = Stopwatch.GetTimestamp();
            try
            {
                if (string.IsNullOrWhiteSpace(jobName))
                {
                    _log.Error("No detail name provided, nothing to do.");
                    throw new ArgumentNullException(nameof(jobName));
                }

                UploadJob job = null;
                using (var configurationJsonFile = File.OpenText("jobs.json"))
                {
                    var serializer = new JsonSerializer();
                    var jobsFile = (JobsFile)serializer.Deserialize(configurationJsonFile,
                        typeof(JobsFile));

                    if (jobsFile?.Jobs.Any() != true)
                    {
                        _log.Error("Unable to read configuration file: configuration.json");
                        throw new Exception("Unable to read configuration file configuration.json");
                    }

                    job = jobsFile.Jobs.SingleOrDefault(_ => _.Name == jobName);
                }

                if (job == null)
                {
                    _log.Error("Details not found for job: {JobName}",
                        jobName);
                    throw new Exception($"Unable to find upload details for job ${jobName}");
                }

                if (string.IsNullOrEmpty(job.Site))
                {
                    _log.Error("Site is not specified for job: {JobName}", job.Name);
                    return;
                }

                if (!job.Path.Contains('*'))
                {
                    if (!File.Exists(job.Path))
                    {
                        _log.Error("Could not find file to upload at: {FilePath}", job.Path);
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

                    switch (files.Length)
                    {
                        case 0:
                            _log.Error("Could not find a file matching: {FilePath}", job.Path);
                            return;
                        case 1:
                            _log.Information("Found match for: {FilePath}: {FileMatch}",
                                job.Path,
                                files[0]);
                            job.Path = files[0];
                            break;
                        default:
                            _log.Error("Found multiple files matching: {FilePath}", job.Path);
                            return;
                    }
                }
                string originalFile = job.Path;

                string tempFile = null;

                string responseText = null;
                long filesize = new FileInfo(job.Path).Length;

                try
                {
                    if (job.ZipBeforeUpload)
                    {
                        tempFile = Path.GetTempFileName();
                        _log.Debug("Zipping to temporary file: {TemporaryFile}", tempFile);
                        using (var fs = new FileStream(tempFile, FileMode.Create))
                        {
                            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
                            {
                                archive.CreateEntryFromFile(job.Path,
                                    Path.GetFileName(job.Path));
                            }
                        }

                        long zipped = new FileInfo(tempFile).Length;

                        _log.Information("Zipped {FilePath} from {OriginalSize} b to {CompressedSize} b into {TemporaryFile}",
                            Path.GetFileName(job.Path),
                            filesize.ToString("n0"),
                            zipped.ToString("n0"),
                            tempFile);
                        job.Path = tempFile;

                        filesize = zipped;
                    }

                    try
                    {
                        using (var client = new WebClient())
                        {
                            if (!string.IsNullOrEmpty(job.Username)
                                && !string.IsNullOrEmpty(job.Password))
                            {
                                _log.Verbose("Using username: {Username}", job.Username);
                                client.Credentials
                                    = new NetworkCredential(job.Username, job.Password);
                            }

                            if (job.Site.Contains("{date}"))
                            {
                                job.Site = job.Site.Replace("{date}",
                                    DateTime.Now.ToString("yyyy-MM-dd"));
                            }


                            _log.Information("Uploading file...");
                            var response = client.UploadFile(job.Site, "STOR", job.Path);

                            responseText = Encoding.UTF8.GetString(response);
                            if (responseText.Length > 0)
                            {
                                _log.Information("Server response: {ServerResponse}",
                                    responseText);
                            }
                            else
                            {
                                _log.Information("Upload complete.");
                            }

                            if (job.DeleteSourceAfterUpload)
                            {
                                _log.Information("Deleting original file post-upload: {FilePath}",
                                    originalFile);
                                try
                                {
                                    if (File.Exists(originalFile))
                                    {
                                        File.Delete(originalFile);
                                    }
                                    else
                                    {
                                        _log.Error("Could not find original file to delete: {FilePath}",
                                            originalFile);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _log.Error("An error occurred deleting original file {FilePath}: {ErrorMessage}",
                                        originalFile,
                                        ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Problem with upload: {ErrorMessage}", ex.Message);
                    }
                }
                finally
                {
                    if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
                    {
                        _log.Debug("Deleting temporary file: {TemporaryFile}", tempFile);
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
                        if (!string.IsNullOrEmpty(job.EmailBody))
                        {
                            message.Body = job.EmailBody;
                        }
                        else
                        {
                            message.Body = $"Uploaded {filesize:n0} bytes."
                                + Environment.NewLine
                                + responseText;
                        }

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
                                _log.Error(ex,
                                    "Sending mail for upload {JobName} failed: {ErrorMessage}",
                                    job.Name,
                                    ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Fatal(ex, "Fatal exception: {ErrorMessage}", ex.Message);
            }
            finally
            {
                _log.Information("Finished processing {JobName} in {Elapsed} ms",
                    jobName,
                    (Stopwatch.GetTimestamp() - start) * 1000 / (double)Stopwatch.Frequency);
            }
        }
    }
}
