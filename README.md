# Uploader
Windows console application (.NET 4.5) for handling periodic FTP uploads and optionally sending a completion email.

- Windows console application for processing file upload jobs
- Upload jobs configured via plain-text [JSON](http://www.json.org/) file
- Ability to compress files before uploading ([ZIP](https://en.wikipedia.org/wiki/Zip_(file_format)) compression)
- Replacement of `{date}` in destination file name with date in `yyyy-MM-dd` format
- Optional ability to send an email notification when a job is complete
- Does not handle scheduling

# Requirements
- .NET 4.5 or later on the system where the console application will run
- Software to schedule running tasks periodically such as [Task Scheduler](https://msdn.microsoft.com/en-us/library/windows/desktop/aa383614.aspx) (optional)
- Mail server accepting SMTP connections (optional)

# Initial setup
- Build the application or [download a release](https://github.com/MCLD/uploader/releases)
- Configure the mail portion in the `Uploader.Console.exe.config` (optional)
- Configure the desired job(s) in the `jobs.json` file (see below)
- Copy the application to the place where you want it to run
- Set up scheduling to automatically run the program (if desired)

# Configuration
Configure jobs by adding objects to `jobs.json` describing the upload jobs. The format of the `jobs.json` file is as follows:

```javascript
{
  "Jobs": [
    {
      "Name": "JobName",
      "Path": "c:\\awesomefile.txt",
      "Site": "ftp://localhost/awesomefile.zip",
      "Username": "ftp",
      "Password": "ftp",
      "ZipBeforeUpload": "true",
      "DeleteSourceAfterUpload": "false",
      "EmailTo": "person@whatever",
      "EmailBcc": "manager@whatever"
      "EmailSubject": "The file was uploaded!",
      "EmailBody": "Upload successful!"
    },
    {
      ...
    }
  ]
}
```

The following fields can be present in a Job description object:
- `Name` - the name of the job (**case-sensitive**) - you run the job by calling `Uploader.Console.exe <name>` (with the above configuration, you'd run `Uploader.Console.exe JobName`)
- `Path` - the full path to the file to upload, e.g. `c:\\awesomefile.txt` (backslashes must be escaped: type them twice). An asterisk (`*`) can be used to match partial file names - the upload will only be successful if there is a single file match.
- `Site` - URL to upload the file to, e.g. `ftp://localhost/awesomefile.zip` - if the configuration line contains `{date}` it will be replaced with the current date in `yyyy-MM-dd` format
- `Username` - optional - login to the FTP site
- `Password` - optional - password to the ftp site
- `ZipBeforeUpload` - optional - whether or not to zip the file before uploading it
- `DeleteSourceAfterUpload` - optional - if true, delete the source file once it has s been uploaded
- `EmailTo` - optional - who to send an email to once the file is uploaded
- `EmailBcc` - optional - a person to blind CC on the email that the software sends
- `EmailSubject` - optional - the subject of the email to send
- `EmailBody` - optional - the body of the email to send, if `EmailTo` and `EmailSubject` are supplied but `EmailBody` is not then just put the number of bytes uploaded in the body of the email.

# Configuring Email
Jobs can optionally be set to send email once they are complete. Email configuration is in the `Uploader.Console.exe.config` file:

```xml
<system.net>
  <mailSettings>
    <smtp from="from-email-address@whatever">
      <network host="smtp.server.hostname" port="25" />
    </smtp>
  </mailSettings>
</system.net>
```

# Dependencies
- Newtonsoft.Json
- Serilog
- Serilog.Settings.AppSettings
- Serilog.Sinks.Console
- Serilog.Sinks.Literate
- Serilog.Sinks.PeriodicBatching

# License
Code released under the [MIT License](https://github.com/MCLD/uploader/blob/master/LICENSE).
