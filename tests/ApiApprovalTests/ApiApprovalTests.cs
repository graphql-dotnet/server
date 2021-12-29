using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using PublicApiGenerator;
using Shouldly;
using Xunit;

namespace GraphQL.Authorization.ApiTests
{
    /// <see href="https://github.com/JakeGinnivan/ApiApprover"/>
    public class ApiApprovalTests
    {
        [Theory]
        [InlineData(typeof(Server.Transports.AspNetCore.NewtonsoftJson.GraphQLRequestDeserializer))]
        [InlineData(typeof(Server.Transports.AspNetCore.SystemTextJson.GraphQLRequestDeserializer))]
        [InlineData(typeof(Server.Ui.Altair.AltairMiddleware))]
        [InlineData(typeof(Server.Ui.GraphiQL.GraphiQLMiddleware))]
        [InlineData(typeof(Server.Ui.Playground.PlaygroundMiddleware))]
        [InlineData(typeof(Server.Ui.Voyager.VoyagerMiddleware))]
        [InlineData(typeof(Server.Authorization.AspNetCore.AuthorizationValidationRule))]
        [InlineData(typeof(Server.GraphQLRequest))]
        [InlineData(typeof(Server.Transports.AspNetCore.GraphQLHttpMiddleware<>))]
        [InlineData(typeof(Server.Transports.Subscriptions.Abstractions.SubscriptionServer))]
        [InlineData(typeof(Server.Transports.WebSockets.WebSocketTransport))]
        public void public_api_should_not_change_unintentionally(Type type)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectName = type.Assembly.GetName().Name!;
            string projectFolderName = projectName["GraphQL.Server.".Length..];
            string testDir = Path.Combine(baseDir, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..");
            string projectDir = Path.Combine(testDir, $"..{Path.DirectorySeparatorChar}..", "src");
            string buildDir = Path.Combine(projectDir, projectFolderName, "bin", "Debug");
            Debug.Assert(Directory.Exists(buildDir), $"Directory '{buildDir}' doesn't exist");
            string csProject = Path.Combine(projectDir, projectFolderName, projectFolderName + ".csproj");
            var project = XDocument.Load(csProject);
            string[] tfms = project.Descendants("TargetFrameworks").Union(project.Descendants("TargetFramework")).First().Value.Split(";", StringSplitOptions.RemoveEmptyEntries);

            // There may be old stuff from earlier builds like net45, netcoreapp3.0, etc. so filter it out
            string[] actualTfmDirs = Directory.GetDirectories(buildDir).Where(dir => tfms.Any(tfm => dir.EndsWith(tfm))).ToArray();
            Debug.Assert(actualTfmDirs.Length > 0, $"Directory '{buildDir}' doesn't contain subdirectories matching {string.Join(";", tfms)}");

            (string tfm, string content)[] publicApi = actualTfmDirs.Select(tfmDir => (new DirectoryInfo(tfmDir).Name.Replace(".", ""), Assembly.LoadFile(Path.Combine(tfmDir, projectName + ".dll")).GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                WhitelistedNamespacePrefixes = new[] { "Microsoft." },
                ExcludeAttributes = new[] { "System.Diagnostics.DebuggerDisplayAttribute", "System.Diagnostics.CodeAnalysis.AllowNullAttribute" }
            }))).ToArray();

            if (publicApi.DistinctBy(item => item.content).Count() == 1)
            {
                AutoApproveOrFail(publicApi[0].content, "");
            }
            else
            {
                foreach (var item in publicApi.ToLookup(item => item.content))
                {
                    AutoApproveOrFail(item.Key, string.Join("+", item.Select(x => x.tfm).OrderBy(x => x)));
                }
            }

            // Approval test should (re)generate approved.txt files locally if needed.
            // Approval test should fail on CI.
            // https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables
            void AutoApproveOrFail(string publicApi, string folder)
            {
                string file = null!;

                try
                {
                    publicApi.ShouldMatchApproved(options => options.SubFolder(folder).NoDiff().WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => file = $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
                }
                catch (ShouldMatchApprovedException) when (Environment.GetEnvironmentVariable("CI") == null)
                {
                    string? received = Path.Combine(testDir, folder, file);
                    string? approved = received.Replace(".received.txt", ".approved.txt");
                    if (File.Exists(received) && File.Exists(approved))
                    {
                        File.Copy(received, approved, overwrite: true);
                        File.Delete(received);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
