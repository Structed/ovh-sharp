#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=OctopusTools"

const string TASK_ALL = "All";
const string TASK_DEFAULT = "Default";
const string TASK_CLEAN = "Clean";
const string TASK_GITVERSION = "GitVersion";
const string TASK_NUGET_RESTORE = "NugetRestore";
const string TASK_BUILD = "Build";
const string TASK_BUILD_LIBRARY = "BuildLibrary";
const string TASK_PACK_LIBARY = "PackLibrary";
const string TASK_PUBLISH_LIBRARY = "PublishLibrary";
const string TASK_BUILD_CLIENT = "BuildClient";
const string TASK_PUBLISH_CLIENT = "PublishClient";
const string TASK_PACK_CLIENT = "PackClient";

string[] TASKS = new [] {
    TASK_ALL,
    TASK_CLEAN,
    TASK_GITVERSION,
    TASK_NUGET_RESTORE,
    TASK_BUILD,
    TASK_BUILD_LIBRARY,
    TASK_PACK_LIBARY,
    TASK_PUBLISH_LIBRARY,
    TASK_BUILD_CLIENT,
    TASK_PUBLISH_CLIENT,
    TASK_PACK_CLIENT,
};

const string PROJECT_BASE_NAME = "ovh-sharp";
const string PROJECT_CLIENT_NAME = "ovh-cli";
const string PROJECT_PATH_LIBRARY = "./src/ovh-sharp";
const string PROJECT_PATH_CLIENT = "./src/ovh-cli";

const string ARTIFACTS_PATH = "./artifacts";
const string PUBLISH_PATH =  ARTIFACTS_PATH + "/publish/";


var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var buildDirLibrary = MakeAbsolute(Directory(PROJECT_PATH_LIBRARY + "/bin/"));
var buildDirClient = MakeAbsolute(Directory(PROJECT_PATH_CLIENT + "/bin/"));

var objDirLibrary = MakeAbsolute(Directory(PROJECT_PATH_LIBRARY + "/obj/"));
var objDirClient = MakeAbsolute(Directory(PROJECT_PATH_CLIENT + "/obj/"));

string octoPackFilePath = "";

GitVersion gitVersion;

// This is the default task which will get ultimately executed (including all the tasks it depends on)
// if no other task is specified on the command-line with the -Target option
Task(TASK_DEFAULT)
    .Does(() =>
{
    foreach (string taskName in TASKS) {
        Information(taskName);
    }
});

Task(TASK_CLEAN)
    .Does(() =>
{
    CleanDirectory(buildDirLibrary);
    CleanDirectory(buildDirClient);
    CleanDirectory(objDirLibrary);
    CleanDirectory(objDirClient);
    CleanDirectory(ARTIFACTS_PATH);
});

Task(TASK_GITVERSION)
    .Does(() =>
{
    gitVersion = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true
    });

    Information("AssemblySemVer: " + gitVersion.AssemblySemVer);
    Information("FullSemVer: " + gitVersion.FullSemVer);
    Information("NuGetVersion: " + gitVersion.NuGetVersion);
    Information("NuGetVersionV2: " + gitVersion.NuGetVersionV2);
});

Task(TASK_NUGET_RESTORE)
    .Does(() =>
{
    DotNetCoreRestore();
});

Task(TASK_BUILD)
    .IsDependentOn(TASK_CLEAN)
    .IsDependentOn(TASK_BUILD_LIBRARY)
    .IsDependentOn(TASK_BUILD_CLIENT);

Task(TASK_BUILD_LIBRARY)
    .IsDependentOn(TASK_CLEAN)
    .IsDependentOn(TASK_NUGET_RESTORE)
    .IsDependentOn(TASK_GITVERSION)
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
     {
        Configuration = configuration,
     };

     DotNetCoreBuild(PROJECT_PATH_LIBRARY, settings);
});

Task(TASK_PACK_LIBARY)
    .IsDependentOn(TASK_CLEAN)
    .IsDependentOn(TASK_BUILD_LIBRARY)
    .Does(() =>
{
    var msbuildSettings = new DotNetCoreMSBuildSettings().SetVersion(gitVersion.NuGetVersion);
    var settings = new DotNetCorePackSettings {
        Configuration = configuration,
        MSBuildSettings = msbuildSettings,
        OutputDirectory = ARTIFACTS_PATH
    };

    DotNetCorePack(PROJECT_PATH_LIBRARY, settings);
});

Task(TASK_PUBLISH_LIBRARY)
    .IsDependentOn(TASK_PACK_LIBARY)
    .Does(() =>
{
    var settings = new DotNetCoreNuGetPushSettings
    {
        Source = GetEnvironmentVariable("OVHSHARP_NUGET_SOURCE"),
        ApiKey = GetEnvironmentVariable("OVHSHARP_NUGET_API_KEY")
    };

    var absolutePath = MakeAbsolute(Directory(ARTIFACTS_PATH)).ToString();
    string filePath = absolutePath + "/" + PROJECT_BASE_NAME + "." + gitVersion.NuGetVersion + ".nupkg";
    DotNetCoreNuGetPush(filePath, settings);
});

Task(TASK_BUILD_CLIENT)
    .IsDependentOn(TASK_CLEAN)
    .IsDependentOn(TASK_NUGET_RESTORE)
    .IsDependentOn(TASK_GITVERSION)
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
     {
         Configuration = configuration,
     };

     DotNetCoreBuild(PROJECT_PATH_CLIENT, settings);
});

Task(TASK_PUBLISH_CLIENT)
    .IsDependentOn(TASK_CLEAN)
    .IsDependentOn(TASK_NUGET_RESTORE)
    .IsDependentOn(TASK_GITVERSION)
    .Does(() =>
{
    var settings = new DotNetCorePublishSettings
    {
        Framework = "netcoreapp1.1",
        Runtime = "win10-x64",
        Configuration = configuration,
        OutputDirectory = PUBLISH_PATH,
    };

    DotNetCorePublish(PROJECT_PATH_CLIENT, settings);
});

Task(TASK_PACK_CLIENT)
    .IsDependentOn(TASK_CLEAN)
    .IsDependentOn(TASK_PUBLISH_CLIENT)
    .Does(() =>
{
    var settings = new OctopusPackSettings {
        Format = OctopusPackFormat.NuPkg,
        Author = "Johannes Ebner <ovh-sharp@jebner.de>",
        BasePath = PUBLISH_PATH,
        Description = "Client to interface with api.ovh.com",
        OutFolder = ARTIFACTS_PATH,
        Overwrite = true,
        ReleaseNotesFile = File("./ReleaseNotes.md"),
        Title = "ovh-cli",
        Version = gitVersion.NuGetVersion,
    };

    octoPackFilePath = ARTIFACTS_PATH + "/" + PROJECT_CLIENT_NAME + "." + gitVersion.NuGetVersion + ".nupkg";

    OctoPack(PROJECT_CLIENT_NAME, settings);
});

Task(TASK_ALL)
    .IsDependentOn(TASK_CLEAN)
    .IsDependentOn(TASK_GITVERSION)
    .IsDependentOn(TASK_BUILD)
    .IsDependentOn(TASK_PUBLISH_CLIENT);

RunTarget(target);

private string GetEnvironmentVariable(string identifier)
{
    Information("trying to get env " + identifier);
    if (HasEnvironmentVariable(identifier) == false)
    {
        throw new ArgumentException("There is no environment variable " + identifier);
    }

    return EnvironmentVariable(identifier);
}