#tool "nuget:?package=GitVersion.CommandLine&version=5.1.3"

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");
var artifactsDir = Directory(Argument<string>("artifactsDir", "./artifacts"));
var publishDir = Directory(Argument<string>("publishDir", "./publish"));
var runtime = Argument<string>("runtime", "win-x64");
var projectFiles = new [] {
  "./src/Core/Core.csproj",
  "./src/Transports.AspNetCore/Transports.AspNetCore.csproj",
  "./src/Transports.AspNetCore.NewtonsoftJson/Transports.AspNetCore.NewtonsoftJson.csproj",
  "./src/Transports.AspNetCore.SystemTextJson/Transports.AspNetCore.SystemTextJson.csproj",
  "./src/Transports.Subscriptions.Abstractions/Transports.Subscriptions.Abstractions.csproj",
  "./src/Transports.Subscriptions.WebSockets/Transports.Subscriptions.WebSockets.csproj",
  "./src/Ui.Playground/Ui.Playground.csproj",
  "./src/Ui.GraphiQL/Ui.GraphiQL.csproj",
  "./src/Ui.Altair/Ui.Altair.csproj",
  "./src/Ui.Voyager/Ui.Voyager.csproj",
  "./src/Authorization.AspNetCore/Authorization.AspNetCore.csproj"
  };

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
          Configuration = configuration,
          OutputDirectory = publishDir,
          Runtime = runtime
      };

      foreach(var projectFile in projectFiles)
      {
          DotNetCorePublish(projectFile, settings);
      }
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

      foreach (var projectFile in projectFiles)
      {
          DotNetCorePack(projectFile, settings);
      }
  });

Task("Build")
  .IsDependentOn("Clean")
  .IsDependentOn("Restore")
  .Does(() =>
  {
      var settings = new DotNetCoreBuildSettings
      {
          Configuration = configuration
      };

      foreach (var projectFile in projectFiles)
      {
          DotNetCoreBuild(projectFile, settings);
      }
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
      foreach (var projectFile in projectFiles)
      {
          DotNetCoreRestore(projectFile);
      }
  });

Task("SetVersion")
  .Does(()=> 
  {
      var versionInfo = GitVersion(new GitVersionSettings {
          RepositoryPath = "."
      });
      version = versionInfo.NuGetVersion;
      
      Information("MajorMinorPatch: {0}", versionInfo.MajorMinorPatch);
      Information("FullSemVer: {0}", versionInfo.FullSemVer);
      Information("InformationalVersion: {0}", versionInfo.InformationalVersion);
      Information("LegacySemVer: {0}", versionInfo.LegacySemVer);
      Information("Nuget v1 version: {0}", versionInfo.NuGetVersion);
      Information("Nuget v2 version: {0}", versionInfo.NuGetVersionV2);

      if (AppVeyor.IsRunningOnAppVeyor) {
          Information($"AppVeyor.UpdateBuildVersion with version: {version}");
          AppVeyor.UpdateBuildVersion(version);
      }
  });

Task("Test")
  .Does(()=> 
  {
      var testProjectFiles = GetFiles("./tests/**/*.csproj");
      foreach (var file in testProjectFiles)
      {
          DotNetCoreTest(file.FullPath);
      }
  });

RunTarget(target);
