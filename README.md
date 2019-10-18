# Overview
This repository contains a Solution with two projects:

* ovh-sharp (`netstandard1.6`)
* ovh-cli (`netcoreapp1.1`)

## ovh-sharp ("The Library")
This is a .NET Standard library project written in C# to wrap the OVH API for .NET Standard.
This was originally developed in 2017, as there was no .NET library available to interface with the OVH API.

## ovh-cli
This is a .NET Core Console Application to actually work with the OVH API via CLI by using the ovh-sharp Library.

## Building
To build the library and the Client, use the build script with the appropriate task/target, which is written using [Cake](https://cakebuild.net), using the build script bootstrapper for PowerShell. You can get a list of targets by executing the bootstrapper without arguments (first time launch will take a while because it needs to download build tool dependencies):

`<REPO-ROOT>: .\build.ps1`

## How to use ovh-cli

### Prerequisites

* Account with OVH
* Set up OVH API access (see [OVH API documentation](https://docs.ovh.com/gb/en/customer/first-steps-with-ovh-api)), which yields these three keys:
  * Application Secret
  * Application Key
  * Consumer Key
* .NET Core 1.1 (with SDK). You should use [this LTS release](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.1.2-download.md).

### Preparation

Once you have the keys/secrets from OVH, you need to decide how you want to use them. You have two options:

* Set environment variables (preferred). The application will look for the following environment variables:
    * `OVH_API_APPLICATION_KEY`
    * `OVH_API_APPLICATION_SECRET`
    * `OVH_API_CONSUMER_KEY`
* Set them as command line parameters on each use (discouraged because it will be logged to the console history):
    * `--ovh-application-key`
    * `--ovh-application-secret`
    * `--ovh-consumer-key`

### Getting help

In the client's directory (`<REPO-ROOT>/src/ovh-cli`) run the program with the `--help` option:

`dotnet run -- --help`

You can run all sub-commands with the `--help` option to get further help on that command, e.g. `dotnet run -- create-cname --help`

### Command examples

#### To create CNAMES
* `dotnet run -- create-cname -z example.com -s sub-domain-name -t actual-host.example.net.`

#### To search CNAME entries that have 'search_string' in their CNAME record:
`dotnet run -- show-records example.com CNAME %search_string%`

#### To remove found entry with ID '1337':
`dotnet run -- delete-record example.com 1337`

#### To Clean CNAME records whose `target` properties start with `foo`:
`dotnet run -- clean-cnames --zone example.com --targetStartsWith "foo"`

# Contributing
Please read [CONTRIBUTING.md](CONTRIBUTING.md)

# Acknowledgement
This project was originally developed at [RE'FLEKT](https://re-flekt.com).

# License
All code and other resources in this repository are licensed under the [MIT License](https://spdx.org/licenses/MIT.html)