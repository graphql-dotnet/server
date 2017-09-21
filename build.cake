var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");
var artifactsDir = Directory(Argument<string>("artifactsDir", "./artifacts"));
var publishDir = Directory(Argument<string>("publishDir", "./publish"));
var framework = Argument<string>("framework", "netstandard2.0");
var runtime = Argument<string>("runtime", "win-x64");
var version = Argument<string>("packageVersion", "0.0.0-dev");
var projectFile = "./src/WebSockets/WebSockets.csproj";

Task("Default")
  .IsDependentOn("Build");

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
  .Does(()=>
  {
      Information($"Version: {version}");
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

Task("AppVeyor")
    .IsDependentOn("PrintAppVeyorInfo")
    .IsDependentOn("UseEnvironment")
    .IsDependentOn("Pack")
    .Does(() => 
    {
        Information("AppVeyor build done.");
    });

Task("PrintAppVeyorInfo")
  .Does(()=>
  {
    Information($"AppVeyor: {AppVeyor.IsRunningOnAppVeyor}, Repo: {AppVeyor?.Environment?.Repository?.Name}");
  });

Task("UseEnvironment")
    .Does(()=> 
    {
        version = AppVeyor.Environment.Build.Version;
        Information($"version: {version}");
    });

RunTarget(target);
