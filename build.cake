#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0012&prerelease"

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");
var artifactsDir = Directory(Argument<string>("artifactsDir", "./artifacts"));
var publishDir = Directory(Argument<string>("publishDir", "./publish"));
var framework = Argument<string>("framework", "netstandard2.0");
var runtime = Argument<string>("runtime", "win-x64");
var projectFile = "./src/WebSockets/WebSockets.csproj";

var version = "0.0.0-dev";

Task("Default")
  .IsDependentOn("SetVersion")
  .IsDependentOn("Pack");

Task("Publish")
  .IsDependentOn("Build")
  .Does(()=>
  {
      var settings = new DotNetCorePublishSettings
      {
          Framework = framework,
          Configuration = configuration,
          OutputDirectory = publishDir,
          Runtime = runtime
      };

      DotNetCorePublish(projectFile, settings);
  });

Task("Pack")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .Does(()=>
  {
      var buildSettings = new DotNetCoreMSBuildSettings();
      buildSettings.SetVersion(version);
      var settings = new DotNetCorePackSettings
      {
          Configuration = configuration,
          OutputDirectory = artifactsDir,
          IncludeSymbols = true,
          MSBuildSettings = buildSettings
      };

      DotNetCorePack(projectFile, settings);
  });

Task("Build")
  .IsDependentOn("Clean")
  .IsDependentOn("Restore")
  .Does(() =>
  {
      var settings = new DotNetCoreBuildSettings
      {
          Framework = framework,
          Configuration = configuration
      };
 
      DotNetCoreBuild(projectFile, settings);
  });

Task("Clean")
  .Does(()=>
  {
      Information($"Cleaning: {artifactsDir}");
      CleanDirectory(artifactsDir);
      Information($"Cleaning: {publishDir}");
      CleanDirectory(publishDir);
  });

Task("Restore")
  .Does(()=>
  {
      DotNetCoreRestore(projectFile);
  });

Task("SetVersion")
    .Does(()=> {
        var versionInfo = GitVersion(new GitVersionSettings {
            RepositoryPath = "."
        });
        version = versionInfo.SemVer;
        Information($"Version: {version}, FullSemVer: {versionInfo.SemVer}");

        if(AppVeyor.IsRunningOnAppVeyor) {
            AppVeyor.UpdateBuildVersion(versionInfo.FullSemVer);
        }
    });

Task("Test")
  .Does(()=> {
      var projectFiles = GetFiles("./tests/**/*.csproj");
      foreach(var file in projectFiles)
      {
          DotNetCoreTest(file.FullPath);
      }
    });

RunTarget(target);
