using GraphQL.Server.Transports.AspNetCore.AzureFunctions;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Samples.AzureFunctions;

public class GraphQL
{
    [FunctionName("GraphQL")]
    public static IActionResult RunGraphQL(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        return new AzureGraphQLActionResult(req);
    }

    [FunctionName("Playground")]
    public static IActionResult RunPlayground(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
        ILogger log)
    {
        _ = req;
        log.LogInformation("Getting Playground UI.");

        return new PlaygroundActionResult(opts => opts.GraphQLEndPoint = "/api/graphql");
    }
}
