using GraphQL.Server.Transports.AspNetCore;
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a GraphQL request.");

        return new GraphQLExecutionActionResult();
    }

    [FunctionName("Playground")]
    public static IActionResult RunPlayground(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request for the GraphQL Playground UI.");

        return new PlaygroundActionResult(opts => opts.GraphQLEndPoint = "/api/graphql"); // /api/graphql route will call RunGraphQL method
    }
}
