# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.2.0]
### Added
- Serilog.Sinks.Seq so that logging can happen to Seq
- Configurable rolling file log

### Fixed
- Logging is now uniformly structured
- General code clean-up

## [1.1.0] - 2017-11-21
### Added
- Option to delete source file once uploaded
- Handle multiple files matching a wildcard path

### Changed
- If no email body is supplied, send the number of bytes uploaded in the email.

## [1.0.0] - 2017-07-21
### Added
- Windows console application for processing file upload jobs
- Upload jobs configured via plain-text [JSON](http://www.json.org/) file
- Ability to compress files before uploading ([ZIP](https://en.wikipedia.org/wiki/Zip_(file_format)) compression)
- Replacement of `{date}` in destination file name with date in `yyyy-MM-dd` format
- Optional ability to send an email notification when a job is complete

[1.2.0]: https://github.com/MCLD/uploader/releases/tag/v1.2.0
[1.1.0]: https://github.com/MCLD/uploader/releases/tag/v1.1.0
[1.0.0]: https://github.com/MCLD/uploader/releases/tag/v1.0.0
