# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
### Added
* README.md
* Default build task for build.cake to list build targets
* clean-cnames now supports the `--targetStartsWith` option to allow filtering for a specific target

### Changed
* Updated cake to 0.20.1
* Implemented proper packing with versioning
* Added feature to publish library as a nuget package
* Added ReleaseNotes.md
* Added Octopus Packaging for client
* Added output for Nuget V2 version number
* Added validation for environment variables
* Added ttl parameter to create-cname

### Known Issues
