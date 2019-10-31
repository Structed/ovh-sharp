using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using Serilog;

namespace OvhSharp.Cli
{
    class Program
    {
        private static CommandLineApplication app;
        private static OvhApi ovhApi;

        private static string ovhApplicationKey = Environment.GetEnvironmentVariable("OVH_API_APPLICATION_KEY");
        private static string ovhConsumerKey = Environment.GetEnvironmentVariable("OVH_API_CONSUMER_KEY");
        private static string ovhApplicationSecret = Environment.GetEnvironmentVariable("OVH_API_APPLICATION_SECRET");
        private static readonly int defaultTTL = 60;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            app = new CommandLineApplication();
            app.Name = "ovh-cli";
            app.HelpOption("-?|-h|--help");

            app.OnExecute(() => {
                return 0;
            });

            app.Command("create-cname", ConfigureCreateCnameCommand());
            app.Command("refresh-zone", ConfigureRefreshZoneCommand());
            app.Command("show-records", ConfigureShowRecordsCommand());
            app.Command("delete-record", ConfigureDeleteRecordCommand());
            app.Command("clean-cnames", ConfigureCleanCnamesCommand());

            app.Execute(args);

            Log.CloseAndFlush();

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
            }
        }

        private static Action<CommandLineApplication> ConfigureCleanCnamesCommand()
        {
            return command =>
            {
                command.Description = "Deletes CNAME records matching the subdomain pattern";
                command.HelpOption("-?|-h|--help");

                var zoneOption = command.Option(
                    "-z|--zone",
                    "The zone for which the CNAME records should be removed for, i.e. example.com",
                    CommandOptionType.SingleValue);

                var subdomainOption = command.Option(
                    "-s|--subdomain",
                    "Subdomain (LIKE pattern)",
                    CommandOptionType.SingleValue);

                var targetStartsWithOption = command.Option(
                    "--targetStartsWith",
                    "Target starts with this",
                    CommandOptionType.SingleValue);

                var forceOption = command.Option(
                    "-f|--force",
                    "Force the operation (do not ask for confirmation)",
                    CommandOptionType.NoValue);

                var ovhOptions = PrepareOvhOptions(command);

                command.OnExecute(() =>
                {
                    var zone = zoneOption.HasValue() ? zoneOption.Value() : throw new ArgumentNullException();

                    string likePattern = "";
                    string targetStartsWith = "";

                    if (subdomainOption.HasValue())
                    {
                        likePattern = subdomainOption.Value();
                    }

                    if (targetStartsWithOption.HasValue())
                    {
                        targetStartsWith = targetStartsWithOption.Value();
                    }

                    if (string.IsNullOrEmpty(likePattern) && string.IsNullOrEmpty(targetStartsWith))
                    {
                        Log.Error("You have to either specify {subdomainOption} or {targetStartsWithOption} to clean CNAMES", subdomainOption.LongName, targetStartsWithOption.LongName);
                        return -1;
                    }

                    if ((string.IsNullOrEmpty(likePattern) || likePattern.Equals("*")) && string.IsNullOrEmpty(targetStartsWith))
                    {
                        Log.Error("{likePattern} can only be not set, empty or a wildcard if {targetStartsWithOption} is set", subdomainOption.LongName, targetStartsWithOption.LongName);
                        return -1;
                    }

                    ParseOvhOptions(ovhOptions);
                    ovhApi = new OvhApi(ovhApplicationSecret, ovhApplicationKey, ovhConsumerKey);

                    IEnumerable<int> domainZoneRecords = ovhApi.GetDomainZoneRecords(zone, "CNAME", likePattern).ToList();
                    Console.Write($"Found {domainZoneRecords.Count()} records. Please stand by while details are being fetched");

                    var recordDetails = new List<JToken>(domainZoneRecords.Count());
                    foreach (var domainZoneRecord in domainZoneRecords)
                    {
                        JToken details = ovhApi.GetDomainZoneRecordDetails(zone, domainZoneRecord);
                        recordDetails.Add(details);
                        Console.Write(".");
                    }

                    Console.WriteLine("DONE.");


                    if (!string.IsNullOrEmpty(targetStartsWith))
                    {
                        var filteredRecordDetails = FilterRecordsByField(recordDetails, "target", targetStartsWith);
                        recordDetails = filteredRecordDetails.ToList();
                    }

                    if (recordDetails.Any())
                    {
                        Console.WriteLine("Found/filtered records:");
                        recordDetails.ForEach(token => Console.WriteLine(token.ToString()));

                        ConsoleKeyInfo key = new ConsoleKeyInfo();

                        if (false == forceOption.HasValue())
                        {
                            Console.Write("Press Y to DELETE the records found. Any other key to cancel: ");

                            key = Console.ReadKey();
                            Console.WriteLine("");
                        }

                        if (forceOption.HasValue() || key.Key == ConsoleKey.Y)
                        {
                            Console.WriteLine($"Now deleting records:");
                            foreach (var record in recordDetails)
                            {
                                Console.WriteLine(record.ToString());
                            }
                            ovhApi.DeleteDomainZoneRecord(zone, recordDetails);
                            Log.Information("Deletion complete.");
                        }
                    }
                    else
                    {
                        Log.Information($"No records have been found. Exiting.");
                    }

                    return 0;
                });
            };
        }

        private static Action<CommandLineApplication> ConfigureDeleteRecordCommand()
        {
            return command =>
            {
                command.Description = "Deletes a domain zone record";
                command.HelpOption("-?|-h|--help");

                var zoneArgument = command.Argument("zone", "the zone name", false);
                var recordIdArgument = command.Argument("record", "The record's ID", false);

                var ovhOptions = PrepareOvhOptions(command);

                command.OnExecute(() =>
                {
                    if (string.IsNullOrEmpty(zoneArgument.Value))
                    {
                        Console.WriteLine("zone cannot be empty");
                        command.ShowHelp("delete-record");
                        return -1;
                    }

                    if (string.IsNullOrEmpty(recordIdArgument.Value))
                    {
                        Console.WriteLine("record argument cannot be empty");
                        command.ShowHelp("delete-record");
                        return -1;
                    }

                    ParseOvhOptions(ovhOptions);
                    ovhApi = new OvhApi(ovhApplicationSecret, ovhApplicationKey, ovhConsumerKey);

                    try
                    {
                        ovhApi.DeleteDomainZoneRecord(zoneArgument.Value, int.Parse(recordIdArgument.Value));
                        Console.WriteLine($"Deleted record {recordIdArgument.Value}");

                        return 0;
                    }
                    catch (System.Net.Http.HttpRequestException httpRequestException)
                    {
                        Console.WriteLine(httpRequestException.Message);
                        return -1;
                    }
                });
            };
        }

        private static Action<CommandLineApplication> ConfigureShowRecordsCommand()
        {
            return command =>
            {
                command.Description = "Shows records of a domain zone";
                command.HelpOption("-?|-h|--help");

                var zoneArgument = command.Argument("zone", "the zone name", false);
                var recordTypeArgument = command.Argument("[record-type]", "record type to filter for (LIKE filter), e.g. CNAME", false);
                var subdomainArgument = command.Argument("[subdomain]", "subdomain to filter for (LIKE filter)", false);

                var ovhOptions =  PrepareOvhOptions(command);

                command.OnExecute(() =>
                {
                    if (string.IsNullOrEmpty(zoneArgument.Value))
                    {
                        Console.WriteLine("zone cannot be empty");
                        command.ShowHelp("show-records");
                        return -1;
                    }

                    ParseOvhOptions(ovhOptions);
                    ovhApi = new OvhApi(ovhApplicationSecret, ovhApplicationKey, ovhConsumerKey);

                    var records = ovhApi.GetDomainZoneRecords(zoneArgument.Value, recordTypeArgument.Value, subdomainArgument.Value);

                    foreach (int record in records)
                    {
                        var recordDetails = ovhApi.GetDomainZoneRecordDetails(zoneArgument.Value, record);
                        Log.Information(recordDetails.ToString());
                    }
                    return 0;
                });
            };
        }

        private static IEnumerable<JToken> FilterRecordsByField(List<JToken> details, string fieldName, string filterValue)
        {
            return details.Where((token) => token[fieldName].Value<string>().StartsWith(filterValue));
        }

        private static Action<CommandLineApplication> ConfigureRefreshZoneCommand()
        {
            return command =>
            {
                command.Description = "Refreshes a domain zone";
                command.HelpOption("-?|-h|--help");

                var zoneArgument = command.Argument("zone", "the zone name", false);

                var ovhOptions = PrepareOvhOptions(command);

                command.OnExecute(() =>
                {
                    if (string.IsNullOrEmpty(zoneArgument.Value))
                    {
                        Console.WriteLine("zone cannot be empty");
                        command.ShowHelp("refresh-zone");
                        return -1;
                    }

                    ParseOvhOptions(ovhOptions);
                    ovhApi = new OvhApi(ovhApplicationSecret, ovhApplicationKey, ovhConsumerKey);

                    ovhApi.RefreshDomainZone(zoneArgument.Value);
                    Console.WriteLine($"The Zone {zoneArgument.Value} has been refreshed.");
                    return 0;
                });
            };
        }

        private static Action<CommandLineApplication> ConfigureCreateCnameCommand()
        {
            return (command) =>
            {
                command.Description = "Creates a CNAME record, e.g. subdomain.example.com --> whatever.acme.com";
                command.HelpOption("-?|-h|--help");

                var zoneOption = command.Option(
                    "-z|--zone",
                    "The zone for which the CNAME record should be created for, i.e. example.com",
                    CommandOptionType.SingleValue);

                var targetOption = command.Option(
                    "-t|--target",
                    "The target this CNAME record points to (needs to end with a dot!), e.g. app-api-development.acme.com.",
                    CommandOptionType.SingleValue);

                var subdomainOption = command.Option(
                    "-s|--subdomain",
                    "The zone's subdomain the CNAME should be created at",
                    CommandOptionType.SingleValue);

                var ttlOption = command.Option(
                    "-ttl|--ttl",
                    $"The record's TTL (time to live), default: {defaultTTL}",
                    CommandOptionType.SingleValue);

                var ovhOptions = PrepareOvhOptions(command);

                command.OnExecute(() =>
                {
                    var zone = zoneOption.HasValue() ? zoneOption.Value() : throw new ArgumentNullException();
                    var target = targetOption.HasValue() ? targetOption.Value() : throw new ArgumentNullException();
                    var subdomain = subdomainOption.HasValue() ? subdomainOption.Value() : throw new ArgumentNullException();
                    var ttl = ttlOption.HasValue() ? long.Parse(ttlOption.Value()) : defaultTTL;

                    ParseOvhOptions(ovhOptions);
                    ovhApi = new OvhApi(ovhApplicationSecret, ovhApplicationKey, ovhConsumerKey);

                    CreateCnameRecord(zone, subdomain, target, ttl);
                    Log.Information($"Created CNAME record for zone {zone} to {target}");

                    return 0;
                });
            };
        }

        private static void CreateCnameRecord(string zone, string subdomain, string target, long ttl)
        {
            if (target.EndsWith(".") == false)
            {
                throw new ArgumentException($"The argument {nameof(target)} needs to end with a .");
            }

            ovhApi.PostDomainZoneRecord(zone, "CNAME", target, subdomain, ttl);
        }

        static (CommandOption ovhApplicationKeyOption, CommandOption ovhApplicationSecretOption, CommandOption ovhConsumerKeyOption) PrepareOvhOptions(CommandLineApplication command)
        {
            var ovhApplicationKeyOption = command.Option(
                "--ovh-application-key",
                "The OVH Application Key",
                CommandOptionType.SingleValue);

            var ovhApplicationSecretOption = command.Option(
                "--ovh-application-secret",
                "The OVH Application Secret",
                CommandOptionType.SingleValue);

            var ovhConsumerKeyOption = command.Option(
                "--ovh-consumer-key",
                "The OVH Consumer Key",
                CommandOptionType.SingleValue);

            return (ovhApplicationKeyOption, ovhApplicationSecretOption, ovhConsumerKeyOption);
        }

        static void ParseOvhOptions(ValueTuple<CommandOption, CommandOption, CommandOption> ovhOptions)
        {
            if (ovhOptions.Item1.HasValue())
            {
                ovhApplicationKey = ovhOptions.Item1.Value();
            }

            if (ovhOptions.Item2.HasValue())
            {
                ovhApplicationSecret = ovhOptions.Item2.Value();
            }

            if (ovhOptions.Item3.HasValue())
            {
                ovhConsumerKey = ovhOptions.Item3.Value();
            }
        }
    }
}