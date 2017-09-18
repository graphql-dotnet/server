var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var artifactsDir = Directory(Argument("artifactsDir", "./artifacts"));
var publishDir = Directory(Argument("publishDir", "./publish"));
var framework = Argument("framework", "netstandard2.0");
var projectFile = "./src/WebSockets/WebSockets.csproj";
var runtime = Argument("runtime", "win-x64");
bool isAppVeyor = AppVeyor.IsRunningOnAppVeyor && AppVeyor.Environment.Repository.Name == "graphql-dotnet/subscription-transport-ws";

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
      var settings = new DotNetCorePackSettings
      {
          Configuration = configuration,
          OutputDirectory = artifactsDir,
          IncludeSymbols = true,
          VersionSuffix = "alpha"
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
    .WithCriteria(isAppVeyor)
    .IsDependentOn("Pack")
    .Does(() => 
    {
        Information("AppVeyor build done.");
        //AppVeyor.UploadArtifact("./dist/Cake.VisualStudio.vsix");
    });

Information($"AppVeyor: {isAppVeyor}");
RunTarget(target);
