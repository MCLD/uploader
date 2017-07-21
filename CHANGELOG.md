# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2017-07-21
### Added
- Windows console application for processing file upload jobs
- Upload jobs configured via plain-text [JSON](http://www.json.org/) file
- Ability to compress files before uploading ([ZIP](https://en.wikipedia.org/wiki/Zip_(file_format) compression)
- Replacement of `{date}` in destination file name with date in `yyyy-MM-dd` format
- Optional ability to send an email notification when a job is complete
